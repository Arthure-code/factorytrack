using FactoryTrack.Mobile.Pages;

namespace FactoryTrack.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Route non declaree dans le XAML : navigation programmatique
		// depuis le plan vers la page detail.
		Routing.RegisterRoute("detail", typeof(PageDetailEquipement));
	}
}
