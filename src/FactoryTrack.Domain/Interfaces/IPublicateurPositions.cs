using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

/// <summary>
/// Diffusion des positions calculees vers les clients.
/// V1 : client SignalR vers l'API. V2 : courtier de messages (voir ADR 0002).
/// </summary>
public interface IPublicateurPositions
{
    Task PublierAsync(Position position, CancellationToken jeton = default);
}
