using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FactoryTrack.Mobile.ViewModels;

[QueryProperty(nameof(BaliseId), "baliseId")]
[QueryProperty(nameof(Code), "code")]
[QueryProperty(nameof(Nom), "nom")]
[QueryProperty(nameof(Categorie), "categorie")]
public partial class DetailEquipementViewModel : ObservableObject
{
    private static readonly TimeSpan FENETRE = TimeSpan.FromMinutes(30);

    private readonly IClientReferentiel _referentiel;
    private readonly ILogger<DetailEquipementViewModel> _logger;

    [ObservableProperty] private string? baliseId;
    [ObservableProperty] private string? code;
    [ObservableProperty] private string? nom;
    [ObservableProperty] private string? categorie;
    [ObservableProperty] private ObservableCollection<PositionDto> positions = new();
    [ObservableProperty] private ObservableCollection<PasserelleDto> passerelles = new();
    [ObservableProperty] private bool chargement;
    [ObservableProperty] private string? messageErreur;
    [ObservableProperty] private double largeurUsine = 60;
    [ObservableProperty] private double hauteurUsine = 40;
    [ObservableProperty] private int nombrePositions;
    [ObservableProperty] private double distanceParcourueMetres;
    [ObservableProperty] private double precisionMoyenne;
    [ObservableProperty] private DateTimeOffset? debutFenetre;
    [ObservableProperty] private DateTimeOffset? finFenetre;

    public DetailEquipementViewModel(
        IClientReferentiel referentiel,
        ILogger<DetailEquipementViewModel> logger)
    {
        _referentiel = referentiel;
        _logger = logger;
    }

    partial void OnBaliseIdChanged(string? value)
    {

        if (!string.IsNullOrWhiteSpace(value))
            _ = ChargerAsync();
    }

    [RelayCommand]
    public async Task ChargerAsync()
    {
        if (!Guid.TryParse(BaliseId, out var id))
            return;

        Chargement = true;
        MessageErreur = null;

        try
        {
            var fin = DateTimeOffset.UtcNow;
            var debut = fin - FENETRE;

            var trace = await _referentiel.ObtenirHistoriqueAsync(id, debut, fin);
            var passerelles = await _referentiel.ObtenirPasserellesAsync();

            Positions = new ObservableCollection<PositionDto>(trace);
            Passerelles = new ObservableCollection<PasserelleDto>(passerelles);

            if (passerelles.Count > 0)
            {
                LargeurUsine = passerelles.Max(p => p.X) + 5;
                HauteurUsine = passerelles.Max(p => p.Y) + 5;
            }

            CalculerStatistiques(trace, debut, fin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chargement de l'historique impossible.");
            MessageErreur = "Historique indisponible. Verifier la connexion au serveur.";
        }
        finally
        {
            Chargement = false;
        }
    }

    private void CalculerStatistiques(IReadOnlyList<PositionDto> trace, DateTimeOffset debut, DateTimeOffset fin)
    {
        DebutFenetre = debut;
        FinFenetre = fin;
        NombrePositions = trace.Count;

        if (trace.Count == 0)
        {
            DistanceParcourueMetres = 0;
            PrecisionMoyenne = 0;
            return;
        }

        double distance = 0;
        for (var i = 1; i < trace.Count; i++)
        {
            var dx = trace[i].X - trace[i - 1].X;
            var dy = trace[i].Y - trace[i - 1].Y;
            distance += Math.Sqrt(dx * dx + dy * dy);
        }

        DistanceParcourueMetres = Math.Round(distance, 1);
        PrecisionMoyenne = Math.Round(trace.Average(p => p.Precision), 2);
    }
}
