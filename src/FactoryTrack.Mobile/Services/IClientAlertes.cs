using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

public interface IClientAlertes
{
    Task<IReadOnlyList<AlerteHistoriqueDto>> ObtenirAsync(
        DateTimeOffset? debut = null,
        DateTimeOffset? fin = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        int limite = 200,
        CancellationToken jeton = default);

    Task SupprimerAsync(Guid id, CancellationToken jeton = default);

    Task SupprimerLotAsync(
    DateTimeOffset? avant = null,
    Guid? zoneId = null,
    string? baliseIdentifiant = null,
    CancellationToken jeton = default);
}
