using System.Collections.Concurrent;
using FactoryTrack.Api.Hubs;
using FactoryTrack.Contracts;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace FactoryTrack.Api.Services;

public class ServiceSurveillanceSilence : BackgroundService
{
    private static readonly TimeSpan PERIODE = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _fabriquePortee;
    private readonly IHubContext<PositionHub> _hub;
    private readonly OptionsPositionnement _options;
    private readonly ILogger<ServiceSurveillanceSilence> _logger;

    private readonly ConcurrentDictionary<string, bool> _etatSilencieux = new();

    public ServiceSurveillanceSilence(
        IServiceScopeFactory fabriquePortee,
        IHubContext<PositionHub> hub,
        IOptions<OptionsPositionnement> options,
        ILogger<ServiceSurveillanceSilence> logger)
    {
        _fabriquePortee = fabriquePortee;
        _hub = hub;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuterie = new PeriodicTimer(PERIODE);

        while (await minuterie.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await VerifierAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {

                _logger.LogError(ex, "Echec du cycle de surveillance.");
            }
        }
    }

    private async Task VerifierAsync(CancellationToken jeton)
    {
        using var portee = _fabriquePortee.CreateScope();
        var depotPositions = portee.ServiceProvider.GetRequiredService<IDepotPositions>();

        var limite = DateTimeOffset.UtcNow.AddSeconds(-_options.DelaiSilenceSecondes);
        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: null, jeton);

        foreach (var position in dernieres)
        {
            var estSilencieuse = position.Horodatage < limite;
            var etaitSilencieuse = _etatSilencieux.GetValueOrDefault(position.BaliseIdentifiant, false);

            if (estSilencieuse == etaitSilencieuse)
                continue;

            _etatSilencieux[position.BaliseIdentifiant] = estSilencieuse;

            var methode = estSilencieuse
                ? NomsHub.Methodes.EquipementSilencieux
                : NomsHub.Methodes.EquipementActif;

            await _hub.Clients
                .Group(NomsHub.Groupes.Etage(position.Etage))
                .SendAsync(methode, position.BaliseIdentifiant, jeton);
        }
    }
}
