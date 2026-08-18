using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

/// <summary>Position calculee, en metres dans le repere local de l'usine.</summary>
public class Position
{
    public Guid BaliseId { get; set; }
    public string BaliseIdentifiant { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int Etage { get; set; }

    /// <summary>Rayon d'incertitude en metres. Plus grand en Bluetooth qu'en UWB.</summary>
    public double Precision { get; set; }

    public TypeTechnologie Technologie { get; set; }
    public int NombreAncres { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
