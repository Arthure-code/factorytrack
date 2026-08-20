namespace FactoryTrack.Domain.Options;

public class OptionsPositionnement
{
    public const string Section = "Positionnement";

    public double ExposantPropagation { get; set; } = 2.8;

    public int AncresMinimales { get; set; } = 3;

    public int FenetreRegroupementMs { get; set; } = 1000;

    public double AlphaLissage { get; set; } = 0.35;

    public double SautMaximalMetres { get; set; } = 8.0;

    public int DelaiSilenceSecondes { get; set; } = 30;

    public int RetentionIdempotenceSecondes { get; set; } = 120;
}
