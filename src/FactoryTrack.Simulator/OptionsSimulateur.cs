namespace FactoryTrack.Simulator;

public class OptionsSimulateur
{
    public const string Section = "Simulateur";

    public string UrlIngestion { get; set; } = "http://ingestion:8080";
    public int NombreEquipements { get; set; } = 20;
    public int PeriodeMs { get; set; } = 1000;

    /// <summary>Dimensions du plan simule, en metres.</summary>
    public double LargeurUsine { get; set; } = 60;
    public double HauteurUsine { get; set; } = 40;

    /// <summary>Vitesse de deplacement des equipements, en metres par seconde.</summary>
    public double VitesseMax { get; set; } = 0.8;

    /// <summary>Ecart-type du bruit ajoute au RSSI, en dB. Le vrai monde est bruite.</summary>
    public double BruitRssi { get; set; } = 3.0;

    /// <summary>Proportion de mesures volontairement perdues, pour tester la robustesse.</summary>
    public double TauxPerte { get; set; } = 0.05;

    /// <summary>Proportion de mesures volontairement dupliquees, pour tester l'idempotence.</summary>
    public double TauxDoublon { get; set; } = 0.03;
}
