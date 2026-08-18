using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

public interface IDepotReferentiel
{
    Task<IReadOnlyList<Passerelle>> ObtenirPasserellesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<Balise>> ObtenirBalisesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<Equipement>> ObtenirEquipementsAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<Zone>> ObtenirZonesAsync(CancellationToken jeton = default);
    Task<IReadOnlyList<MachineFixe>> ObtenirMachinesAsync(CancellationToken jeton = default);
}
