using FactoryTrack.Contracts.Grpc;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using FactoryTrack.Domain.Positionnement;
using FactoryTrack.Infrastructure.Depots;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using TechnologieDomaine = FactoryTrack.Domain.Enums.TypeTechnologie;

namespace FactoryTrack.Ingestion.Services;

/// <summary>
/// Point d'entree du flux montant. Les mesures d'une meme balise sont regroupees
/// dans une fenetre temporelle avant trilateration : une seule mesure ne suffit pas
/// a positionner quoi que ce soit.
/// </summary>
public class ServiceIngestionGrpc : ServiceIngestion.ServiceIngestionBase
{
    private readonly ServicePositionnement _positionnement;
    private readonly IGardeIdempotence _idempotence;
    private readonly GardeHorsOrdre _horsOrdre;
    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly CacheReferentiel _cache;
    private readonly OptionsPositionnement _options;
    private readonly ILogger<ServiceIngestionGrpc> _journal;

    public ServiceIngestionGrpc(
        ServicePositionnement positionnement,
        IGardeIdempotence idempotence,
        GardeHorsOrdre horsOrdre,
        IServiceScopeFactory fabriquePortee,
        CacheReferentiel cache,
        IOptions<OptionsPositionnement> options,
        ILogger<ServiceIngestionGrpc> journal)
    {
        _positionnement = positionnement;
        _idempotence = idempotence;
        _horsOrdre = horsOrdre;
        _fabriquePortee = fabriquePortee;
        _cache = cache;
        _options = options.Value;
        _journal = journal;
    }

    public override async Task<AccuseReception> EnvoyerMesures(
        IAsyncStreamReader<MesureRssiMessage> flux, ServerCallContext contexte)
    {
        var jeton = contexte.CancellationToken;
        var compteurs = new Compteurs();

        // Mesures en attente, groupees par balise, en attendant d'avoir assez d'ancres.
        var enAttente = new Dictionary<string, List<MesureRssi>>();

        using var portee = _fabriquePortee.CreateScope();
        var depotReferentiel = portee.ServiceProvider.GetRequiredService<IDepotReferentiel>();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();
        var publicateur = portee.ServiceProvider.GetRequiredService<IPublicateurPositions>();

        var (passerelles, balises) = await _cache.ObtenirAsync(depotReferentiel, jeton);

        await foreach (var message in flux.ReadAllAsync(jeton))
        {
            compteurs.Recues++;

            var mesure = Convertir(message);

            if (!_idempotence.Accepter(mesure.CleIdempotence))
            {
                compteurs.RejeteesDoublon++;
                continue;
            }

            compteurs.Acceptees++;

            if (!enAttente.TryGetValue(mesure.BaliseId, out var groupe))
            {
                groupe = new List<MesureRssi>();
                enAttente[mesure.BaliseId] = groupe;
            }

            // Une seule mesure par passerelle : la plus recente remplace la precedente.
            groupe.RemoveAll(m => m.PasserelleId == mesure.PasserelleId);
            groupe.Add(mesure);

            if (groupe.Count < _options.AncresMinimales)
                continue;

            if (!balises.TryGetValue(mesure.BaliseId, out var balise))
            {
                _journal.LogWarning("Balise inconnue : {Balise}.", mesure.BaliseId);
                enAttente.Remove(mesure.BaliseId);
                continue;
            }

            var horodatageGroupe = groupe.Max(m => m.Horodatage);

            if (!_horsOrdre.Accepter(mesure.BaliseId, horodatageGroupe))
            {
                compteurs.RejeteesHorsOrdre++;
                enAttente.Remove(mesure.BaliseId);
                continue;
            }

            var resultat = _positionnement.Calculer(groupe, passerelles, balise);
            enAttente.Remove(mesure.BaliseId);

            if (!resultat.Reussi || resultat.Position is null)
            {
                _journal.LogDebug("Positionnement ecarte pour {Balise} : {Motif}", mesure.BaliseId, resultat.Motif);
                continue;
            }

            await depotPositions.EnregistrerAsync(resultat.Position, jeton);
            await publicateur.PublierAsync(resultat.Position, jeton);

            compteurs.PositionsCalculees++;
        }

        _journal.LogInformation(
            "Flux termine : {Recues} recues, {Acceptees} acceptees, {Positions} positions.",
            compteurs.Recues, compteurs.Acceptees, compteurs.PositionsCalculees);

        return new AccuseReception
        {
            Recues = compteurs.Recues,
            Acceptees = compteurs.Acceptees,
            RejeteesDoublon = compteurs.RejeteesDoublon,
            RejeteesHorsOrdre = compteurs.RejeteesHorsOrdre,
            PositionsCalculees = compteurs.PositionsCalculees
        };
    }

    public override Task<ReponsePing> Ping(RequetePing requete, ServerCallContext contexte) =>
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

    private sealed class Compteurs
    {
        public int Recues;
        public int Acceptees;
        public int RejeteesDoublon;
        public int RejeteesHorsOrdre;
        public int PositionsCalculees;
    }
}
