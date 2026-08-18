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
/// en zone interdite ou a la sortie d'un perimetre de securite. Comme la
/// surveillance du silence, on n'emet qu'aux transitions.
///
/// Un equipement est en "etat d'alerte" pour une zone donnee :
///   - zone interdite : quand il EST dedans
///   - perimetre     : quand il n'est PAS dedans
/// La transition (etait en alerte -> ne l'est plus, ou l'inverse) declenche
/// l'evenement. Le meme AlerteZoneEntree/Sortie sert dans les deux cas ; le
/// client distingue via les flags ZoneInterdite / ZonePerimetre du DTO.
/// </summary>
public class ServiceSurveillanceZones : BackgroundService
{
    private static readonly TimeSpan PERIODE = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly IHubContext<PositionHub> _hub;
    private readonly ILogger<ServiceSurveillanceZones> _journal;

    // Zones en etat d'alerte pour chaque balise au dernier cycle. Sert a detecter
    // les transitions. Cle : identifiant de balise ; valeur : ids des zones en alerte.
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _alertesParBalise = new();

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
        var zonesSurveillees = zones.Where(z => z.Interdite || z.Perimetre).ToList();
        if (zonesSurveillees.Count == 0)
            return;

        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: null, jeton);

        foreach (var position in dernieres)
        {
            var enAlerteMaintenant = zonesSurveillees
                .Where(z => EstEnAlerte(z, position))
                .Select(z => z.Id)
                .ToHashSet();

            var enAlerteAvant = _alertesParBalise.GetValueOrDefault(
                position.BaliseIdentifiant, new HashSet<Guid>());

            var entrees = enAlerteMaintenant.Except(enAlerteAvant);
            var sorties = enAlerteAvant.Except(enAlerteMaintenant);

            foreach (var zoneId in entrees)
                await EmettreAsync(NomsHub.Methodes.AlerteZoneEntree, position,
                    zonesSurveillees.First(z => z.Id == zoneId), jeton);

            foreach (var zoneId in sorties)
                await EmettreAsync(NomsHub.Methodes.AlerteZoneSortie, position,
                    zonesSurveillees.First(z => z.Id == zoneId), jeton);

            _alertesParBalise[position.BaliseIdentifiant] = enAlerteMaintenant;
        }
    }

    private static bool EstEnAlerte(Zone zone, Domain.Entites.Position position)
    {
        var dedans = zone.Contient(position.X, position.Y, position.Etage);
        // Zone interdite : alerte quand on est dedans.
        // Perimetre : alerte quand on est dehors (mais seulement si la zone
        // concerne l'etage de l'equipement, sinon on ignore).
        if (zone.Interdite) return dedans;
        if (zone.Perimetre) return zone.Etage == position.Etage && !dedans;
        return false;
    }

    private Task EmettreAsync(string methode, Domain.Entites.Position position, Zone zone, CancellationToken jeton)
    {
        var dto = new AlerteZoneDto(
            position.BaliseIdentifiant,
            zone.Id,
            zone.Nom,
            zone.Interdite,
            zone.Perimetre,
            zone.Etage,
            position.Horodatage);

        return _hub.Clients
            .Group(NomsHub.Groupes.Etage(zone.Etage))
            .SendAsync(methode, dto, jeton);
    }
}
