using FactoryTrack.Mobile.ViewModels;

namespace FactoryTrack.Mobile.Pages;

public partial class PageHistoriqueAlertes : ContentPage
{
    private readonly HistoriqueAlertesViewModel _modele;

    public PageHistoriqueAlertes(HistoriqueAlertesViewModel modele)
    {
        InitializeComponent();
        BindingContext = _modele = modele;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _modele.ChargerAsync();
    }
}
