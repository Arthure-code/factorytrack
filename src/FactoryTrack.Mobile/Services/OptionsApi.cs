using FactoryTrack.Contracts;

namespace FactoryTrack.Mobile.Services;

public class OptionsApi
{
    public string UrlBase { get; set; } = ValeurParDefaut();

    public string CheminHub => UrlBase.TrimEnd('/') + NomsHub.Chemin;

    private static string ValeurParDefaut()
    {
#if ANDROID
        return "http://10.0.2.2:8080";
#else
        return "http://localhost:8080";
#endif
    }
}
