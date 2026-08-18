using FactoryTrack.Contracts;

namespace FactoryTrack.Mobile.Services;

/// <summary>
/// URL du serveur FactoryTrack. Volontairement mutable : l'utilisateur pourra
/// la modifier depuis une page de reglages en V1.1.
/// L'URL par defaut differe par plateforme : sur l'emulateur Android,
/// l'hote est vu comme 10.0.2.2, pas comme localhost.
/// </summary>
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
