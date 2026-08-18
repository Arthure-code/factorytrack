using System.Collections.Concurrent;
using FactoryTrack.Api.Hubs;
using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FactoryTrack.Api.Services;

/// <summary>
/// Balaie regulierement les dernieres positions et emet une alerte a l'entree
/// ou la sortie d'une zone. Comme la surveillance du silence, on n'emet qu'aux
/// transitions : signaler qu'un equipement "est toujours dans la zone" a chaque
/// cycle inonderait l'UI et masquerait les vraies entrees.
///
/// Les zones sont rechargees a chaque cycle : le referentiel change rarement et
/// une politique manquee de quelques secondes n'a pas de consequence.
/// </summary>
public class ServiceSurveillanceZones : BackgroundService
{
    private static readonly TimeSpan PERIODE = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly IHubContext<PositionHub> _hub;
    private readonly ILogger<ServiceSurveillanceZones> _journal;

    // Zones dans lesquelles chaque balise se trouvait au dernier cycle. Sert a detecter
    // les transitions. Cle : identifiant de balise ; valeur : ids des zones occupees.
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _zonesParBalise = new();

    public ServiceSurveillanceZones(
        IServiceScopeFactory fabriquePortee,
        IHubContext<PositionHub> hub,
        ILogger<ServiceSurveillanceZones> journal)
    {
        _fabriquePortee = fabriquePortee;
        _hub = hub;
        _journal = journal;
    }

    protected override async Task ExecuteAsync(CancellationToken jeton)
    {
        using var minuterie = new PeriodicTimer(PERIODE);

        while (await minuterie.WaitForNextTickAsync(jeton))
        {
            try
            {
                await VerifierAsync(jeton);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _journal.LogError(ex, "Echec du cycle de surveillance des zones.");
            }
        }
    }

    private async Task VerifierAsync(CancellationToken jeton)
    {
        using var portee = _fabriquePortee.CreateScope();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();
        var depotReferentiel = portee.ServiceProvider.GetRequiredService<IDepotReferentiel>();

        var zones = await depotReferentiel.ObtenirZonesAsync(jeton);
        if (zones.Count == 0)
            return;

        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: null, jeton);

        foreach (var position in dernieres)
        {
            var zonesCourantes = zones
                .Where(z => z.Contient(position.X, position.Y, position.Etage))
                .Select(z => z.Id)
                .ToHashSet();

            var zonesPrecedentes = _zonesParBalise.GetValueOrDefault(position.BaliseIdentifiant, new HashSet<Guid>());

            var entrees = zonesCourantes.Except(zonesPrecedentes);
            var sorties = zonesPrecedentes.Except(zonesCourantes);

            foreach (var zoneId in entrees)
                await EmettreAsync(NomsHub.Methodes.AlerteZoneEntree, position, zones.First(z => z.Id == zoneId), jeton);

            foreach (var zoneId in sorties)
                await EmettreAsync(NomsHub.Methodes.AlerteZoneSortie, position, zones.First(z => z.Id == zoneId), jeton);

            _zonesParBalise[position.BaliseIdentifiant] = zonesCourantes;
        }
    }

    private Task EmettreAsync(string methode, Domain.Entites.Position position, Zone zone, CancellationToken jeton)
    {
        var dto = new AlerteZoneDto(
            position.BaliseIdentifiant,
            zone.Id,
            zone.Nom,
            zone.Interdite,
            zone.Etage,
            position.Horodatage);

        return _hub.Clients
            .Group(NomsHub.Groupes.Etage(zone.Etage))
            .SendAsync(methode, dto, jeton);
    }
}
