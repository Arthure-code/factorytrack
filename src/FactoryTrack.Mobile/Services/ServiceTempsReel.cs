using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FactoryTrack.Mobile.Services;

public sealed class ServiceTempsReel : IServiceTempsReel
{
    private readonly OptionsApi _options;
    private readonly ILogger<ServiceTempsReel> _logger;
    private readonly SemaphoreSlim _verrou = new(1, 1);

    private HubConnection? _connexion;
    private int _etageCourant;

    public event Action<PositionDto>? PositionRecue;
    public event Action<string>? EquipementSilencieux;
    public event Action<string>? EquipementActif;
    public event Action<AlerteZoneDto>? AlerteZoneEntree;
    public event Action<AlerteZoneDto>? AlerteZoneSortie;
    public event Func<Task>? Resynchronisation;

    public ServiceTempsReel(OptionsApi options, ILogger<ServiceTempsReel> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool EstConnecte => _connexion?.State == HubConnectionState.Connected;

    public async Task DemarrerAsync(int etage, CancellationToken jeton = default)
    {
        await _verrou.WaitAsync(jeton);

        try
        {
            _etageCourant = etage;

            if (_connexion is null)
            {
                _connexion = new HubConnectionBuilder()
                    .WithUrl(_options.CheminHub)
                    .WithAutomaticReconnect()
                    .Build();

                _connexion.On<PositionDto>(NomsHub.Methodes.PositionMiseAJour,
                    p => PositionRecue?.Invoke(p));

                _connexion.On<string>(NomsHub.Methodes.EquipementSilencieux,
                    id => EquipementSilencieux?.Invoke(id));

                _connexion.On<string>(NomsHub.Methodes.EquipementActif,
                    id => EquipementActif?.Invoke(id));

                _connexion.On<AlerteZoneDto>(NomsHub.Methodes.AlerteZoneEntree,
                    a => AlerteZoneEntree?.Invoke(a));

                _connexion.On<AlerteZoneDto>(NomsHub.Methodes.AlerteZoneSortie,
                    a => AlerteZoneSortie?.Invoke(a));

                _connexion.Reconnected += async _ =>
                {
                    _logger.LogInformation("Reconnexion SignalR : reprise sur l'etage {Etage}.", _etageCourant);
                    await _connexion.InvokeAsync("RejoindreEtage", _etageCourant);

                    if (Resynchronisation is not null)
                        await Resynchronisation.Invoke();
                };
            }

            if (_connexion.State == HubConnectionState.Disconnected)
                await _connexion.StartAsync(jeton);

            await _connexion.InvokeAsync("RejoindreEtage", _etageCourant, jeton);
        }
        finally
        {
            _verrou.Release();
        }
    }

    public async Task ChangerEtageAsync(int nouvelEtage, CancellationToken jeton = default)
    {
        if (_connexion is null || !EstConnecte)
        {
            _etageCourant = nouvelEtage;
            return;
        }

        await _connexion.InvokeAsync("QuitterEtage", _etageCourant, jeton);
        _etageCourant = nouvelEtage;
        await _connexion.InvokeAsync("RejoindreEtage", _etageCourant, jeton);
    }

    public async Task ArreterAsync()
    {
        if (_connexion is null)
            return;

        try
        {
            await _connexion.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arret SignalR : erreur non bloquante.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connexion is not null)
            await _connexion.DisposeAsync();

        _verrou.Dispose();
    }
}
