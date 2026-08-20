namespace FactoryTrack.Simulator;

public class OptionsSimulateur
{
    public const string Section = "Simulateur";

    public string UrlIngestion { get; set; } = "https://localhost:8081";
    public int NombreEquipements { get; set; } = 20;
    public int PeriodeMs { get; set; } = 1000;

    public double LargeurUsine { get; set; } = 60;
    public double HauteurUsine { get; set; } = 40;

    public double VitesseMax { get; set; } = 0.8;

    public double BruitRssi { get; set; } = 3.0;

    public double TauxPerte { get; set; } = 0.05;

    public double TauxDoublon { get; set; } = 0.03;
}
