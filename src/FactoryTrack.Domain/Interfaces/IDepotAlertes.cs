using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Domain.Interfaces;

/// <summary>
/// Journal d'alertes. Ecriture des transitions par la surveillance, lecture
/// et suppression par l'ecran d'historique cote client.
/// </summary>
public interface IDepotAlertes
{
    Task EnregistrerAsync(AlerteHistorique alerte, CancellationToken jeton = default);

    /// <summary>
    /// Historique filtre. Tous les criteres sont optionnels et se combinent
    /// (ET logique). Resultats les plus recents en premier.
    /// </summary>
    Task<IReadOnlyList<AlerteHistorique>> ObtenirAsync(
        DateTimeOffset? debut = null,
        DateTimeOffset? fin = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        int limite = 200,
        CancellationToken jeton = default);

    Task<int> SupprimerAsync(Guid id, CancellationToken jeton = default);

    /// <summary>Suppression par lot. Au moins un critere doit etre fourni.</summary>
    Task<int> SupprimerLotAsync(
        DateTimeOffset? avant = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        CancellationToken jeton = default);
}
