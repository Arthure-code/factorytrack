using FactoryTrack.Mobile.ViewModels;

namespace FactoryTrack.Mobile.Pages;

public partial class PagePlanUsine : ContentPage
{
    private readonly PlanUsineViewModel _modele;

    public PagePlanUsine(PlanUsineViewModel modele)
    {
        InitializeComponent();
        BindingContext = _modele = modele;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_modele.Equipements.Count == 0)
            await _modele.ChargerAsync();
    }
}
