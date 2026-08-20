using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace FactoryTrack.Ingestion.Services;

public class PublicateurSignalR : IPublicateurPositions, IAsyncDisposable
{
    private const string METHODE_DIFFUSION = "DiffuserPosition";

    private readonly HubConnection _connexion;
    private readonly SemaphoreSlim _verrouDemarrage = new(1, 1);
    private readonly ILogger<PublicateurSignalR> _logger;

    public PublicateurSignalR(IConfiguration configuration, ILogger<PublicateurSignalR> logger)
    {
        _logger = logger;

        var url = configuration["Api:UrlHub"]
            ?? throw new InvalidOperationException("Configuration manquante : Api:UrlHub");

        _connexion = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connexion.Reconnected += id =>
        {
            _logger.LogInformation("Reconnexion au hub etablie ({Id}).", id);
            return Task.CompletedTask;
        };
    }

    public async Task PublierAsync(Position position, CancellationToken jeton = default)
    {
        if (_connexion.State == HubConnectionState.Disconnected)
            await DemarrerAsync(jeton);

        if (_connexion.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Hub indisponible : position de {Balise} non diffusee.", position.BaliseIdentifiant);
            return;
        }

        var dto = new PositionDto(
            position.BaliseIdentifiant,
            position.X,
            position.Y,
            position.Etage,
            position.Precision,
            position.Technologie.ToString(),
            position.NombreAncres,
            position.Horodatage);

        await _connexion.InvokeAsync(METHODE_DIFFUSION, dto, jeton);
    }

    private async Task DemarrerAsync(CancellationToken jeton)
    {
        await _verrouDemarrage.WaitAsync(jeton);

        try
        {

            if (_connexion.State != HubConnectionState.Disconnected)
                return;

            await _connexion.StartAsync(jeton);
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Connexion au hub impossible.");
        }
        finally
        {
            _verrouDemarrage.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connexion.DisposeAsync();
        _verrouDemarrage.Dispose();
        GC.SuppressFinalize(this);
    }
}
