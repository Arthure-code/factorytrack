using FactoryTrack.Contracts.Grpc;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using FactoryTrack.Positioning;
using FactoryTrack.Infrastructure.Depots;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using TechnologieDomaine = FactoryTrack.Domain.Enums.TypeTechnologie;

namespace FactoryTrack.Ingestion.Services;

public class ServiceIngestionGrpc : ServiceIngestion.ServiceIngestionBase
{
    private readonly ServicePositionnement _positionnement;
    private readonly GardesIngestion _gardes;
    private readonly IPublicateurPositions _publicateur;
    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly CacheReferentiel _cache;
    private readonly OptionsPositionnement _options;
    private readonly ILogger<ServiceIngestionGrpc> _logger;

    public ServiceIngestionGrpc(
        ServicePositionnement positionnement,
        GardesIngestion gardes,
        IPublicateurPositions publicateur,
        IServiceScopeFactory fabriquePortee,
        CacheReferentiel cache,
        IOptions<OptionsPositionnement> options,
        ILogger<ServiceIngestionGrpc> logger)
    {
        _positionnement = positionnement;
        _gardes = gardes;
        _publicateur = publicateur;
        _fabriquePortee = fabriquePortee;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<AccuseReception> EnvoyerMesures(
        IAsyncStreamReader<MesureRssiMessage> requestStream, ServerCallContext context)
    {
        var jeton = context.CancellationToken;

        using var portee = _fabriquePortee.CreateScope();
        var depotReferentiel = portee.ServiceProvider.GetRequiredService<IDepotReferentiel>();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();

        var (passerelles, balises) = await _cache.ObtenirAsync(depotReferentiel, jeton);

        var etat = new EtatFlux(
            passerelles,
            balises,
            passerelles.Values.Count(p => p.Active),
            depotPositions);

        await foreach (var message in requestStream.ReadAllAsync(jeton))
            await TraiterMessageAsync(message, etat, jeton);

        await ViderGroupesAsync(etat, jeton);

        _logger.LogInformation(
            "Flux termine : {Recues} recues, {Acceptees} acceptees, {Positions} positions.",
            etat.Compteurs.Recues, etat.Compteurs.Acceptees, etat.Compteurs.PositionsCalculees);

        return new AccuseReception
        {
            Recues = etat.Compteurs.Recues,
            Acceptees = etat.Compteurs.Acceptees,
            RejeteesDoublon = etat.Compteurs.RejeteesDoublon,
            RejeteesHorsOrdre = etat.Compteurs.RejeteesHorsOrdre,
            PositionsCalculees = etat.Compteurs.PositionsCalculees
        };
    }

    private async Task TraiterMessageAsync(MesureRssiMessage message, EtatFlux etat, CancellationToken jeton)
    {
        etat.Compteurs.Recues++;

        var mesure = Convertir(message);

        if (!_gardes.Idempotence.Accepter(mesure.CleIdempotence))
        {
            etat.Compteurs.RejeteesDoublon++;
            return;
        }

        etat.Compteurs.Acceptees++;

        if (!etat.Balises.TryGetValue(mesure.BaliseId, out var balise))
        {
            _logger.LogWarning("Balise inconnue : {Balise}.", mesure.BaliseId);
            return;
        }

        var groupe = await ObtenirGroupeAsync(mesure, balise, etat, jeton);

        groupe.RemoveAll(m => m.PasserelleId == mesure.PasserelleId);
        groupe.Add(mesure);

        if (groupe.Count >= etat.NombrePasserellesActives && groupe.Count >= _options.AncresMinimales)
        {
            await FermerGroupeAsync(groupe, etat, balise, jeton);
            etat.EnAttente.Remove(mesure.BaliseId);
        }
    }

    private async Task<List<MesureRssi>> ObtenirGroupeAsync(
        MesureRssi mesure, Balise balise, EtatFlux etat, CancellationToken jeton)
    {
        if (!etat.EnAttente.TryGetValue(mesure.BaliseId, out var groupe))
        {
            groupe = new List<MesureRssi>();
            etat.EnAttente[mesure.BaliseId] = groupe;
            return groupe;
        }

        var fenetre = TimeSpan.FromMilliseconds(_options.FenetreRegroupementMs);

        if (groupe.Count > 0 && mesure.Horodatage - groupe.Min(m => m.Horodatage) > fenetre)
        {
            await FermerGroupeAsync(groupe, etat, balise, jeton);
            groupe.Clear();
        }

        return groupe;
    }

    private async Task ViderGroupesAsync(EtatFlux etat, CancellationToken jeton)
    {
        foreach (var (baliseId, groupe) in etat.EnAttente)
        {
            if (groupe.Count < _options.AncresMinimales || !etat.Balises.TryGetValue(baliseId, out var balise))
                continue;

            await FermerGroupeAsync(groupe, etat, balise, jeton);
        }
    }

    private async Task FermerGroupeAsync(
        List<MesureRssi> groupe, EtatFlux etat, Balise balise, CancellationToken jeton)
    {
        if (groupe.Count < _options.AncresMinimales)
            return;

        var horodatageGroupe = groupe.Max(m => m.Horodatage);

        if (!_gardes.HorsOrdre.Accepter(balise.Identifiant, horodatageGroupe))
        {
            etat.Compteurs.RejeteesHorsOrdre++;
            return;
        }

        var resultat = _positionnement.Calculer(groupe, etat.Passerelles, balise);

        if (!resultat.Reussi || resultat.Position is null)
        {
            _logger.LogDebug("Positionnement ecarte pour {Balise} : {Motif}", balise.Identifiant, resultat.Motif);
            return;
        }

        await etat.DepotPositions.EnregistrerAsync(resultat.Position, jeton);
        await _publicateur.PublierAsync(resultat.Position, jeton);

        etat.Compteurs.PositionsCalculees++;
    }

    public override Task<ReponsePing> Ping(RequetePing request, ServerCallContext context) =>
        Task.FromResult(new ReponsePing
        {
            Version = "1.0.0",
            HorodatageServeur = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
        });

    private static MesureRssi Convertir(MesureRssiMessage message) => new(
        message.BaliseId,
        message.PasserelleId,
        message.Rssi,
        message.Technologie == TypeTechnologie.Uwb ? TechnologieDomaine.Uwb : TechnologieDomaine.Bluetooth,
        message.Horodatage.ToDateTimeOffset());

    private sealed class EtatFlux
    {
        public EtatFlux(
            IReadOnlyDictionary<string, Passerelle> passerelles,
            IReadOnlyDictionary<string, Balise> balises,
            int nombrePasserellesActives,
            IDepotPositions depotPositions)
        {
            Passerelles = passerelles;
            Balises = balises;
            NombrePasserellesActives = nombrePasserellesActives;
            DepotPositions = depotPositions;
        }

        public IReadOnlyDictionary<string, Passerelle> Passerelles { get; }
        public IReadOnlyDictionary<string, Balise> Balises { get; }
        public int NombrePasserellesActives { get; }
        public IDepotPositions DepotPositions { get; }
        public Compteurs Compteurs { get; } = new();
        public Dictionary<string, List<MesureRssi>> EnAttente { get; } = new();
    }

    private sealed class Compteurs
    {
        public int Recues;
        public int Acceptees;
        public int RejeteesDoublon;
        public int RejeteesHorsOrdre;
        public int PositionsCalculees;
    }
}
