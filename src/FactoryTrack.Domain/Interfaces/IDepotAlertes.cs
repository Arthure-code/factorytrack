using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

public interface IDepotAlertes
{
    Task EnregistrerAsync(AlerteHistorique alerte, CancellationToken jeton = default);

    Task<IReadOnlyList<AlerteHistorique>> ObtenirAsync(
    DateTimeOffset? debut = null,
    DateTimeOffset? fin = null,
    Guid? zoneId = null,
    string? baliseIdentifiant = null,
    int limite = 200,
    CancellationToken jeton = default);

    Task<int> SupprimerAsync(Guid id, CancellationToken jeton = default);

    Task<int> SupprimerLotAsync(
    DateTimeOffset? avant = null,
    Guid? zoneId = null,
    string? baliseIdentifiant = null,
    CancellationToken jeton = default);
}
