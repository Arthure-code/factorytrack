using FactoryTrack.Mobile.Pages;

namespace FactoryTrack.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Routes non declarees dans le XAML : navigation programmatique
		// depuis le plan vers les pages secondaires.
		Routing.RegisterRoute("detail", typeof(PageDetailEquipement));
		Routing.RegisterRoute("historique", typeof(PageHistoriqueAlertes));
	}
}
