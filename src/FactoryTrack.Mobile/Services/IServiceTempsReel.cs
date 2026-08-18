using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

/// <summary>
/// Abonnement au flux temps reel des positions. Une seule instance pour toute
/// l'application : la HubConnection sous-jacente est coûteuse a etablir et
/// sait se reconnecter seule.
/// </summary>
public interface IServiceTempsReel : IAsyncDisposable
{
    event Action<PositionDto>? PositionRecue;
    event Action<string>? EquipementSilencieux;
    event Action<string>? EquipementActif;
    event Action<AlerteZoneDto>? AlerteZoneEntree;
    event Action<AlerteZoneDto>? AlerteZoneSortie;

    /// <summary>Emis quand la connexion revient : le ViewModel doit resynchroniser.</summary>
    event Func<Task>? Resynchronisation;

    bool EstConnecte { get; }

    Task DemarrerAsync(int etage, CancellationToken jeton = default);
    Task ChangerEtageAsync(int nouvelEtage, CancellationToken jeton = default);
    Task ArreterAsync();
}
