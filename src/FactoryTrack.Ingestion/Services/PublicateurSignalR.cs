using FactoryTrack.Contracts;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace FactoryTrack.Ingestion.Services;

/// <summary>
/// V1 : l'ingestion se connecte au hub de l'API comme un client et lui transmet
/// les positions a diffuser. Voir ADR 0002 pour le remplacement par un courtier.
/// </summary>
public class PublicateurSignalR : IPublicateurPositions, IAsyncDisposable
{
    private const string METHODE_DIFFUSION = "DiffuserPosition";

    private readonly HubConnection _connexion;
    private readonly ILogger<PublicateurSignalR> _journal;

    public PublicateurSignalR(IConfiguration configuration, ILogger<PublicateurSignalR> journal)
    {
        _journal = journal;

        var url = configuration["Api:UrlHub"]
            ?? throw new InvalidOperationException("Configuration manquante : Api:UrlHub");

        _connexion = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connexion.Reconnected += id =>
        {
            _journal.LogInformation("Reconnexion au hub etablie ({Id}).", id);
            return Task.CompletedTask;
        };
    }

    public async Task PublierAsync(Position position, CancellationToken jeton = default)
    {
        if (_connexion.State == HubConnectionState.Disconnected)
            await DemarrerAsync(jeton);

        if (_connexion.State != HubConnectionState.Connected)
        {
            _journal.LogWarning("Hub indisponible : position de {Balise} non diffusee.", position.BaliseIdentifiant);
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

    public async Task DemarrerAsync(CancellationToken jeton = default)
    {
        try
        {
            await _connexion.StartAsync(jeton);
        }
        catch (Exception ex)
        {
            // La diffusion est secondaire : l'ingestion et le stockage doivent continuer.
            _journal.LogError(ex, "Connexion au hub impossible.");
        }
    }

    public async ValueTask DisposeAsync() => await _connexion.DisposeAsync();
}
