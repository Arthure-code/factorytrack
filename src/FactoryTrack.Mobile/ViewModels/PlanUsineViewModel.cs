using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FactoryTrack.Mobile.ViewModels;

/// <summary>Nombre maximal d'alertes gardees dans la banniere.</summary>
file static class LimitesAlertes
{
    public const int Maximum = 5;
    public static readonly TimeSpan Duree = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Etat du plan de l'usine : referentiel statique et positions temps reel.
/// Les evenements SignalR arrivent sur un thread de fond ; toute modification
/// des collections observables passe par le dispatcher UI.
/// </summary>
public partial class PlanUsineViewModel : ObservableObject, IAsyncDisposable
{
    private const double MARGE_PLAN_METRES = 5;

    private readonly IClientReferentiel _referentiel;
    private readonly IServiceTempsReel _tempsReel;
    private readonly ILogger<PlanUsineViewModel> _journal;

    private readonly Dictionary<string, EquipementApercu> _parBalise = new();

    [ObservableProperty] private ObservableCollection<EquipementApercu> equipements = new();
    [ObservableProperty] private ObservableCollection<PasserelleDto> passerelles = new();
    [ObservableProperty] private ObservableCollection<ZoneDto> zones = new();
    [ObservableProperty] private ObservableCollection<MachineFixeDto> machines = new();
    [ObservableProperty] private int etage;
    [ObservableProperty] private bool chargement;
    [ObservableProperty] private string? messageErreur;
    [ObservableProperty] private double largeurUsine = 60;
    [ObservableProperty] private double hauteurUsine = 40;
    [ObservableProperty] private bool connecte;
    [ObservableProperty] private bool afficherPrecision;
    [ObservableProperty] private ObservableCollection<AlerteApercu> alertes = new();
    [ObservableProperty] private bool panneauAlertesOuvert;
    [ObservableProperty] private bool panneauMenuOuvert;

    public PlanUsineViewModel(
        IClientReferentiel referentiel,
        IServiceTempsReel tempsReel,
        ILogger<PlanUsineViewModel> journal)
    {
        _referentiel = referentiel;
        _tempsReel = tempsReel;
        _journal = journal;

        _tempsReel.PositionRecue += TraiterPosition;
        _tempsReel.EquipementSilencieux += id => BasculerSilence(id, true);
        _tempsReel.EquipementActif += id => BasculerSilence(id, false);
        _tempsReel.AlerteZoneEntree += TraiterAlerteEntree;
        _tempsReel.AlerteZoneSortie += TraiterAlerteSortie;
        _tempsReel.Resynchronisation += ChargerAsync;
    }

    [RelayCommand]
    public async Task ChargerAsync()
    {
        Chargement = true;
        MessageErreur = null;

        try
        {
            var equipements = await _referentiel.ObtenirEquipementsAsync();
            var passerelles = await _referentiel.ObtenirPasserellesAsync();
            var zones = await _referentiel.ObtenirZonesAsync();
            var machines = await _referentiel.ObtenirMachinesAsync();

            await SurUiAsync(() =>
            {
                Passerelles = new ObservableCollection<PasserelleDto>(passerelles);
                Zones = new ObservableCollection<ZoneDto>(zones);
                Machines = new ObservableCollection<MachineFixeDto>(machines);

                _parBalise.Clear();
                var apercus = new List<EquipementApercu>();

                foreach (var dto in equipements)
                {
                    var apercu = new EquipementApercu(dto);
                    apercus.Add(apercu);

                    if (dto.BaliseIdentifiant is not null)
                        _parBalise[dto.BaliseIdentifiant] = apercu;
                }

                Equipements = new ObservableCollection<EquipementApercu>(apercus);

                if (passerelles.Count > 0)
                {
                    LargeurUsine = passerelles.Max(p => p.X) + MARGE_PLAN_METRES;
                    HauteurUsine = passerelles.Max(p => p.Y) + MARGE_PLAN_METRES;
                }
            });

            await _tempsReel.DemarrerAsync(Etage);
            Connecte = _tempsReel.EstConnecte;
        }
        catch (Exception ex)
        {
            _journal.LogError(ex, "Chargement du plan impossible.");
            MessageErreur = "Serveur injoignable. Verifier l'URL et reessayer.";
        }
        finally
        {
            Chargement = false;
        }
    }

    [RelayCommand]
    public Task RafraichirAsync()
    {
        PanneauMenuOuvert = false;
        return ChargerAsync();
    }

    [RelayCommand]
    public void BasculerPanneauAlertes()
    {
        PanneauMenuOuvert = false;
        PanneauAlertesOuvert = !PanneauAlertesOuvert;
    }

    [RelayCommand]
    public void BasculerMenu()
    {
        PanneauAlertesOuvert = false;
        PanneauMenuOuvert = !PanneauMenuOuvert;
    }

    [RelayCommand]
    public void FermerPanneaux()
    {
        PanneauAlertesOuvert = false;
        PanneauMenuOuvert = false;
    }

    [RelayCommand]
    public void EffacerAlertes()
    {
        Alertes.Clear();
        PanneauAlertesOuvert = false;
    }

    [RelayCommand]
    public void BasculerPrecision()
    {
        AfficherPrecision = !AfficherPrecision;
        PanneauMenuOuvert = false;
    }

    [RelayCommand]
    public async Task OuvrirDetailAsync(EquipementApercu? apercu)
    {
        if (apercu is null || apercu.BaliseId is null)
            return;

        // On passe les infos statiques dans l'URL pour eviter au detail d'attendre
        // un round-trip REST juste pour afficher l'en-tete.
        var parametres = new Dictionary<string, object>
        {
            ["baliseId"] = apercu.BaliseId.Value.ToString(),
            ["code"] = apercu.Code,
            ["nom"] = apercu.Nom,
            ["categorie"] = apercu.Categorie ?? string.Empty
        };

        await Shell.Current.GoToAsync("detail", parametres);
    }

    private void TraiterPosition(PositionDto position)
    {
        SurUi(() =>
        {
            if (_parBalise.TryGetValue(position.BaliseIdentifiant, out var apercu))
                apercu.AppliquerPosition(position);
        });
    }

    private void BasculerSilence(string baliseId, bool silencieux)
    {
        SurUi(() =>
        {
            if (_parBalise.TryGetValue(baliseId, out var apercu))
                apercu.Silencieux = silencieux;
        });
    }

    private void TraiterAlerteEntree(AlerteZoneDto alerte)
    {
        // Deux cas d'alerte a notifier : entree en zone interdite, ou sortie
        // du perimetre de securite. Les zones neutres (production, quai) sont
        // ignorees ici : entrer dedans est un evenement metier normal.
        if (!alerte.ZoneInterdite && !alerte.ZonePerimetre)
            return;

        SurUi(() =>
        {
            var code = alerte.BaliseIdentifiant;
            if (_parBalise.TryGetValue(alerte.BaliseIdentifiant, out var apercu))
            {
                code = apercu.Code;
                if (alerte.ZoneInterdite) apercu.DansZoneInterdite = true;
                if (alerte.ZonePerimetre) apercu.HorsPerimetre = true;
            }

            Alertes.Insert(0, new AlerteApercu(alerte, code));

            while (Alertes.Count > LimitesAlertes.Maximum)
                Alertes.RemoveAt(Alertes.Count - 1);

            // Auto-expiration de la banniere apres LimitesAlertes.Duree.
            var horodatage = alerte.Horodatage;
            _ = Task.Delay(LimitesAlertes.Duree).ContinueWith(_ =>
                SurUi(() =>
                {
                    for (var i = Alertes.Count - 1; i >= 0; i--)
                    {
                        if (Alertes[i].Horodatage == horodatage)
                            Alertes.RemoveAt(i);
                    }
                }));
        });
    }

    private void TraiterAlerteSortie(AlerteZoneDto alerte)
    {
        if (!alerte.ZoneInterdite && !alerte.ZonePerimetre)
            return;

        SurUi(() =>
        {
            if (_parBalise.TryGetValue(alerte.BaliseIdentifiant, out var apercu))
            {
                if (alerte.ZoneInterdite) apercu.DansZoneInterdite = false;
                if (alerte.ZonePerimetre) apercu.HorsPerimetre = false;
            }
        });
    }

    private static void SurUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.IsDispatchRequired == false)
            action();
        else
            dispatcher.Dispatch(action);
    }

    private static Task SurUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.IsDispatchRequired == false)
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.DispatchAsync(action);
    }

    public async ValueTask DisposeAsync()
    {
        _tempsReel.PositionRecue -= TraiterPosition;
        _tempsReel.Resynchronisation -= ChargerAsync;
        await _tempsReel.ArreterAsync();
    }
}
