using FactoryTrack.Api.Hubs;
using FactoryTrack.Contracts;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace FactoryTrack.Api.Services;

/// <summary>
/// Detecte les equipements dont plus aucune mesure n'arrive et previent les clients.
/// Afficher une position perimee comme actuelle serait un mensonge fonctionnel.
/// </summary>
public class ServiceSurveillanceSilence : BackgroundService
{
    private static readonly TimeSpan PERIODE = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly IHubContext<PositionHub> _hub;
    private readonly OptionsPositionnement _options;
    private readonly ILogger<ServiceSurveillanceSilence> _journal;

    public ServiceSurveillanceSilence(
        IServiceScopeFactory fabriquePortee,
        IHubContext<PositionHub> hub,
        IOptions<OptionsPositionnement> options,
        ILogger<ServiceSurveillanceSilence> journal)
    {
        _fabriquePortee = fabriquePortee;
        _hub = hub;
        _options = options.Value;
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
                // Une erreur ponctuelle ne doit pas tuer la boucle de surveillance.
                _journal.LogError(ex, "Echec du cycle de surveillance.");
            }
        }
    }

    private async Task VerifierAsync(CancellationToken jeton)
    {
        using var portee = _fabriquePortee.CreateScope();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();

        var limite = DateTimeOffset.UtcNow.AddSeconds(-_options.DelaiSilenceSecondes);
        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: 0, jeton);

        foreach (var position in dernieres.Where(p => p.Horodatage < limite))
        {
            await _hub.Clients
                .Group(NomsHub.Groupes.Etage(position.Etage))
                .SendAsync(NomsHub.Methodes.EquipementSilencieux, position.BaliseIdentifiant, jeton);
        }
    }
}
