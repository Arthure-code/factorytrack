using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

public interface IClientReferentiel
{
    Task<IReadOnlyList<EquipementDto>> ObtenirEquipementsAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<PasserelleDto>> ObtenirPasserellesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<ZoneDto>> ObtenirZonesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<MachineFixeDto>> ObtenirMachinesAsync(CancellationToken jeton = default);

    Task<IReadOnlyList<PositionDto>> ObtenirHistoriqueAsync(
    Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default);
}
