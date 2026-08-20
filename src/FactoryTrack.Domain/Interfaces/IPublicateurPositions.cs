using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

public interface IPublicateurPositions
{
    Task PublierAsync(Position position, CancellationToken jeton = default);
}
