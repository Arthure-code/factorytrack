using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryTrack.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FactoryTrack.Mobile.ViewModels;

public partial class HistoriqueAlertesViewModel : ObservableObject
{
    private readonly IClientAlertes _client;
    private readonly ILogger<HistoriqueAlertesViewModel> _logger;

    [ObservableProperty] private ObservableCollection<AlerteHistoriqueApercu> alertes = new();
    [ObservableProperty] private bool chargement;
    [ObservableProperty] private string? messageErreur;
    [ObservableProperty] private string? messageInfo;
    [ObservableProperty] private int nombreSelectionnees;

    public HistoriqueAlertesViewModel(
        IClientAlertes client,
        ILogger<HistoriqueAlertesViewModel> logger)
    {
        _client = client;
        _logger = logger;
    }

    [RelayCommand]
    public async Task ChargerAsync()
    {
        Chargement = true;
        MessageErreur = null;
        MessageInfo = null;

        try
        {
            var recues = await _client.ObtenirAsync(limite: 500);

            foreach (var ancienne in Alertes)
                ancienne.PropertyChanged -= AuChangementSelection;

            Alertes = new ObservableCollection<AlerteHistoriqueApercu>(
                recues.Select(a =>
                {
                    var apercu = new AlerteHistoriqueApercu(a);
                    apercu.PropertyChanged += AuChangementSelection;
                    return apercu;
                }));

            NombreSelectionnees = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chargement de l'historique impossible.");
            MessageErreur = "Impossible de charger l'historique. Verifier la connexion.";
        }
        finally
        {
            Chargement = false;
        }
    }

    private void AuChangementSelection(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AlerteHistoriqueApercu.Selectionnee))
            NombreSelectionnees = Alertes.Count(a => a.Selectionnee);
    }

    [RelayCommand]
    public async Task SupprimerSelectionAsync()
    {
        var aSupprimer = Alertes.Where(a => a.Selectionnee).ToList();
        if (aSupprimer.Count == 0) return;

        try
        {
            foreach (var alerte in aSupprimer)
                await _client.SupprimerAsync(alerte.Id);

            foreach (var alerte in aSupprimer)
            {
                alerte.PropertyChanged -= AuChangementSelection;
                Alertes.Remove(alerte);
            }

            NombreSelectionnees = 0;
            MessageInfo = $"{aSupprimer.Count} alerte(s) supprimee(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suppression de la selection impossible.");
            MessageErreur = "Suppression partiellement echouee. Rafraichir.";
        }
    }

    [RelayCommand]
    public async Task SupprimerAvantAujourdhuiAsync()
    {
        var minuit = DateTimeOffset.Now.Date;
        await SupprimerLotAsync(avant: minuit, message: "avant aujourd'hui");
    }

    [RelayCommand]
    public async Task SupprimerAvantUneHeureAsync()
    {
        var limite = DateTimeOffset.UtcNow.AddHours(-1);
        await SupprimerLotAsync(avant: limite, message: "de plus d'une heure");
    }

    private async Task SupprimerLotAsync(DateTimeOffset avant, string message)
    {
        try
        {
            await _client.SupprimerLotAsync(avant: avant);
            await ChargerAsync();
            MessageInfo = $"Alertes {message} supprimees.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suppression par lot impossible.");
            MessageErreur = "Suppression par lot echouee.";
        }
    }

    [RelayCommand]
    public void ToutSelectionner()
    {
        foreach (var alerte in Alertes)
            alerte.Selectionnee = true;
    }

    [RelayCommand]
    public void ToutDeselectionner()
    {
        foreach (var alerte in Alertes)
            alerte.Selectionnee = false;
    }
}
