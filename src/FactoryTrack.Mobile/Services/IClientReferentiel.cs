using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

/// <summary>
/// Acces REST au referentiel et aux dernieres positions connues. Utilise a
/// deux moments : chargement initial de la page, et resynchronisation apres
/// une reconnexion SignalR (les messages perdus ne reviennent pas seuls).
/// </summary>
public interface IClientReferentiel
{
    Task<IReadOnlyList<EquipementDto>> ObtenirEquipementsAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<PasserelleDto>> ObtenirPasserellesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<ZoneDto>> ObtenirZonesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<MachineFixeDto>> ObtenirMachinesAsync(CancellationToken jeton = default);

    /// <summary>Trace de positions d'une balise sur un intervalle. Sert a la page detail.</summary>
    Task<IReadOnlyList<PositionDto>> ObtenirHistoriqueAsync(
        Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default);
}
