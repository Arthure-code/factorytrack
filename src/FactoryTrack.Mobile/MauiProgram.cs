using FactoryTrack.Mobile.Pages;
using FactoryTrack.Mobile.Services;
using FactoryTrack.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FactoryTrack.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// URL du serveur : une seule instance modifiable, injectee partout.
		builder.Services.AddSingleton<OptionsApi>();

		// Client HTTP par service : le typed client est trop rigide ici puisque
		// l'URL peut changer a chaud. On garde une seule fabrique.
		builder.Services.AddHttpClient();
		builder.Services.AddScoped<IClientReferentiel>(sp =>
			new ClientReferentiel(
				sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
				sp.GetRequiredService<OptionsApi>()));

		// SignalR : instance unique de bout en bout, sinon on paye un handshake par page.
		builder.Services.AddSingleton<IServiceTempsReel, ServiceTempsReel>();

		builder.Services.AddTransient<PlanUsineViewModel>();
		builder.Services.AddTransient<PagePlanUsine>();
		builder.Services.AddTransient<DetailEquipementViewModel>();
		builder.Services.AddTransient<PageDetailEquipement>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
