namespace FactoryTrack.Domain.Options;

public class OptionsPositionnement
{
    public const string Section = "Positionnement";

    /// <summary>Exposant de perte de propagation. 2.0 en espace libre, 2.7 a 4.0 en interieur encombre.</summary>
    public double ExposantPropagation { get; set; } = 2.8;

    /// <summary>Nombre minimal d'ancres pour trilaterer. En dessous, la mesure est ecartee.</summary>
    public int AncresMinimales { get; set; } = 3;

    /// <summary>Fenetre de regroupement des mesures d'une meme balise, en millisecondes.</summary>
    public int FenetreRegroupementMs { get; set; } = 1000;

    /// <summary>Coefficient du lissage exponentiel. 0 = fige, 1 = aucun lissage.</summary>
    public double AlphaLissage { get; set; } = 0.35;

    /// <summary>Deplacement au-dela duquel on considere un saut aberrant, en metres.</summary>
    public double SautMaximalMetres { get; set; } = 8.0;

    /// <summary>Delai sans mesure au-dela duquel un equipement est declare silencieux.</summary>
    public int DelaiSilenceSecondes { get; set; } = 30;

    /// <summary>Duree de conservation des cles d'idempotence.</summary>
    public int RetentionIdempotenceSecondes { get; set; } = 120;
}
