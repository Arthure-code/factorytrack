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

        builder.Services.AddSingleton<OptionsApi>();

        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IClientReferentiel>(sp =>
            new ClientReferentiel(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                sp.GetRequiredService<OptionsApi>()));
        builder.Services.AddScoped<IClientAlertes>(sp =>
            new ClientAlertes(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                sp.GetRequiredService<OptionsApi>()));

        builder.Services.AddSingleton<IServiceTempsReel, ServiceTempsReel>();

        builder.Services.AddTransient<PlanUsineViewModel>();
        builder.Services.AddTransient<PagePlanUsine>();
        builder.Services.AddTransient<DetailEquipementViewModel>();
        builder.Services.AddTransient<PageDetailEquipement>();
        builder.Services.AddTransient<HistoriqueAlertesViewModel>();
        builder.Services.AddTransient<PageHistoriqueAlertes>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
