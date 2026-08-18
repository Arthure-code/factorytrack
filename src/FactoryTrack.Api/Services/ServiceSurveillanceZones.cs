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
/// en zone interdite ou a la sortie d'un perimetre de securite. Chaque
/// transition est aussi consignee en base (IDepotAlertes) : la source de
/// verite temps reel reste SignalR, le journal sert l'ecran d'historique.
/// </summary>
public class ServiceSurveillanceZones : BackgroundService
{
    private static readonly TimeSpan PERIODE = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly IHubContext<PositionHub> _hub;
    private readonly ILogger<ServiceSurveillanceZones> _journal;

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
        var depotAlertes = portee.ServiceProvider.GetRequiredService<IDepotAlertes>();

        var zones = await depotReferentiel.ObtenirZonesAsync(jeton);
        var zonesSurveillees = zones.Where(z => z.Interdite || z.Perimetre).ToList();
        if (zonesSurveillees.Count == 0)
            return;

        var equipements = await depotReferentiel.ObtenirEquipementsAsync(jeton);
        var codesParBalise = equipements
            .Where(e => e.Balise is not null)
            .ToDictionary(e => e.Balise!.Identifiant, e => e.Code);

        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: null, jeton);

        foreach (var position in dernieres)
        {
            var enAlerteMaintenant = zonesSurveillees
                .Where(z => EstEnAlerte(z, position))
                .Select(z => z.Id)
                .ToHashSet();

            var enAlerteAvant = _alertesParBalise.GetValueOrDefault(
                position.BaliseIdentifiant, new HashSet<Guid>());

            var entrees = enAlerteMaintenant.Except(enAlerteAvant).ToList();
            var sorties = enAlerteAvant.Except(enAlerteMaintenant).ToList();

            var code = codesParBalise.GetValueOrDefault(position.BaliseIdentifiant, position.BaliseIdentifiant);

            foreach (var zoneId in entrees)
            {
                var zone = zonesSurveillees.First(z => z.Id == zoneId);
                await ConsignerAsync(depotAlertes, position, zone, code, estEntree: true, jeton);
                await EmettreAsync(NomsHub.Methodes.AlerteZoneEntree, position, zone, jeton);
            }

            foreach (var zoneId in sorties)
            {
                var zone = zonesSurveillees.First(z => z.Id == zoneId);
                await ConsignerAsync(depotAlertes, position, zone, code, estEntree: false, jeton);
                await EmettreAsync(NomsHub.Methodes.AlerteZoneSortie, position, zone, jeton);
            }

            _alertesParBalise[position.BaliseIdentifiant] = enAlerteMaintenant;
        }
    }

    private static bool EstEnAlerte(Zone zone, Domain.Entites.Position position)
    {
        var dedans = zone.Contient(position.X, position.Y, position.Etage);
        if (zone.Interdite) return dedans;
        if (zone.Perimetre) return zone.Etage == position.Etage && !dedans;
        return false;
    }

    private static Task ConsignerAsync(
        IDepotAlertes depot, Domain.Entites.Position position, Zone zone,
        string codeEquipement, bool estEntree, CancellationToken jeton)
    {
        return depot.EnregistrerAsync(new AlerteHistorique
        {
            Id = Guid.NewGuid(),
            BaliseIdentifiant = position.BaliseIdentifiant,
            CodeEquipement = codeEquipement,
            ZoneId = zone.Id,
            ZoneNom = zone.Nom,
            ZoneInterdite = zone.Interdite,
            ZonePerimetre = zone.Perimetre,
            EstEntree = estEntree,
            Etage = zone.Etage,
            Horodatage = position.Horodatage
        }, jeton);
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
