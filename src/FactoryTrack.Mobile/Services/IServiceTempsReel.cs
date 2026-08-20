using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

public interface IServiceTempsReel : IAsyncDisposable
{
    event Action<PositionDto>? PositionRecue;
    event Action<string>? EquipementSilencieux;
    event Action<string>? EquipementActif;
    event Action<AlerteZoneDto>? AlerteZoneEntree;
    event Action<AlerteZoneDto>? AlerteZoneSortie;

    event Func<Task>? Resynchronisation;

    bool EstConnecte { get; }

    Task DemarrerAsync(int etage, CancellationToken jeton = default);
    Task ChangerEtageAsync(int nouvelEtage, CancellationToken jeton = default);
    Task ArreterAsync();
}
