using FactoryTrack.Mobile.Pages;

namespace FactoryTrack.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("detail", typeof(PageDetailEquipement));
        Routing.RegisterRoute("historique", typeof(PageHistoriqueAlertes));
    }
}
