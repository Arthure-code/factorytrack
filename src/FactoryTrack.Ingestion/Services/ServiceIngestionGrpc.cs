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
///
/// Un groupe est ferme et trilatere quand :
///   - il contient une mesure de toutes les passerelles actives (cas nominal), OU
///   - une nouvelle mesure arrive alors que la plus ancienne a plus de
///     FenetreRegroupementMs (le groupe est ferme avant d'accueillir la nouvelle).
/// Cette double condition evite deux defauts opposes : trilaterer trop tot en
/// gaspillant les ancres qui suivent, ou attendre indefiniment un lot complet
/// quand une passerelle est muette.
/// </summary>
public class ServiceIngestionGrpc : ServiceIngestion.ServiceIngestionBase
{
    private readonly ServicePositionnement _positionnement;
    private readonly IGardeIdempotence _idempotence;
    private readonly GardeHorsOrdre _horsOrdre;
    private readonly IPublicateurPositions _publicateur;
    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly CacheReferentiel _cache;
    private readonly OptionsPositionnement _options;
    private readonly ILogger<ServiceIngestionGrpc> _journal;

    public ServiceIngestionGrpc(
        ServicePositionnement positionnement,
        IGardeIdempotence idempotence,
        GardeHorsOrdre horsOrdre,
        IPublicateurPositions publicateur,
        IServiceScopeFactory fabriquePortee,
        CacheReferentiel cache,
        IOptions<OptionsPositionnement> options,
        ILogger<ServiceIngestionGrpc> journal)
    {
        _positionnement = positionnement;
        _idempotence = idempotence;
        _horsOrdre = horsOrdre;
        _publicateur = publicateur;
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
        var fenetre = TimeSpan.FromMilliseconds(_options.FenetreRegroupementMs);

        // Mesures en attente, groupees par balise, jusqu'a fermeture de la fenetre.
        var enAttente = new Dictionary<string, List<MesureRssi>>();

        using var portee = _fabriquePortee.CreateScope();
        var depotReferentiel = portee.ServiceProvider.GetRequiredService<IDepotReferentiel>();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();

        var (passerelles, balises) = await _cache.ObtenirAsync(depotReferentiel, jeton);
        var nombrePasserellesActives = passerelles.Values.Count(p => p.Active);

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

            if (!balises.TryGetValue(mesure.BaliseId, out var balise))
            {
                _journal.LogWarning("Balise inconnue : {Balise}.", mesure.BaliseId);
                continue;
            }

            if (!enAttente.TryGetValue(mesure.BaliseId, out var groupe))
            {
                groupe = new List<MesureRssi>();
                enAttente[mesure.BaliseId] = groupe;
            }
            else if (groupe.Count > 0)
            {
                // Groupe deja ouvert : est-il perime ? Si oui on le ferme avant d'ajouter
                // la nouvelle mesure (elle appartient au lot suivant).
                var age = mesure.Horodatage - groupe.Min(m => m.Horodatage);
                if (age > fenetre)
                {
                    await FermerGroupeAsync(groupe, passerelles, balise, depotPositions, compteurs, jeton);
                    groupe.Clear();
                }
            }

            // Une seule mesure par passerelle : la plus recente remplace la precedente.
            groupe.RemoveAll(m => m.PasserelleId == mesure.PasserelleId);
            groupe.Add(mesure);

            // Cas nominal : toutes les passerelles ont parle, on peut trilaterer.
            if (groupe.Count >= nombrePasserellesActives && groupe.Count >= _options.AncresMinimales)
            {
                await FermerGroupeAsync(groupe, passerelles, balise, depotPositions, compteurs, jeton);
                enAttente.Remove(mesure.BaliseId);
            }
        }

        // Fin du flux : on tente de trilaterer les groupes qui ont atteint le quorum minimal.
        foreach (var (baliseId, groupe) in enAttente)
        {
            if (groupe.Count < _options.AncresMinimales || !balises.TryGetValue(baliseId, out var balise))
                continue;

            await FermerGroupeAsync(groupe, passerelles, balise, depotPositions, compteurs, jeton);
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

    private async Task FermerGroupeAsync(
        List<MesureRssi> groupe,
        IReadOnlyDictionary<string, Passerelle> passerelles,
        Balise balise,
        IDepotPositions depotPositions,
        Compteurs compteurs,
        CancellationToken jeton)
    {
        if (groupe.Count < _options.AncresMinimales)
            return;

        var horodatageGroupe = groupe.Max(m => m.Horodatage);

        if (!_horsOrdre.Accepter(balise.Identifiant, horodatageGroupe))
        {
            compteurs.RejeteesHorsOrdre++;
            return;
        }

        var resultat = _positionnement.Calculer(groupe, passerelles, balise);

        if (!resultat.Reussi || resultat.Position is null)
        {
            _journal.LogDebug("Positionnement ecarte pour {Balise} : {Motif}", balise.Identifiant, resultat.Motif);
            return;
        }

        await depotPositions.EnregistrerAsync(resultat.Position, jeton);
        await _publicateur.PublierAsync(resultat.Position, jeton);

        compteurs.PositionsCalculees++;
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
