namespace FactoryTrack.Api.Services;

public abstract class ServiceSurveillancePeriodique : BackgroundService
{
    private readonly TimeSpan _periode;
    private readonly string _nomCycle;
    private readonly ILogger _logger;

    protected ServiceSurveillancePeriodique(TimeSpan periode, string nomCycle, ILogger logger)
    {
        _periode = periode;
        _nomCycle = nomCycle;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuterie = new PeriodicTimer(_periode);

        while (await minuterie.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await VerifierAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Echec du cycle de surveillance {Cycle}.", _nomCycle);
            }
        }
    }

    protected abstract Task VerifierAsync(CancellationToken jeton);
}
