using FactoryTrack.Mobile.ViewModels;

namespace FactoryTrack.Mobile.Pages;

public partial class PageDetailEquipement : ContentPage
{
    public PageDetailEquipement(DetailEquipementViewModel modele)
    {
        InitializeComponent();
        BindingContext = modele;
    }
}
