using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

public interface IDepotPositions
{
    Task EnregistrerAsync(Position position, CancellationToken jeton = default);

    Task EnregistrerLotAsync(IReadOnlyCollection<Position> positions, CancellationToken jeton = default);

    Task<IReadOnlyList<Position>> ObtenirDernieresAsync(int? etage, CancellationToken jeton = default);

    Task<IReadOnlyList<Position>> ObtenirHistoriqueAsync(
        Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default);
}
