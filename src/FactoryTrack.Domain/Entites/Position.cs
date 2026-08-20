using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

public class Position
{
    public Guid BaliseId { get; set; }
    public string BaliseIdentifiant { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int Etage { get; set; }

    public double Precision { get; set; }

    public TypeTechnologie Technologie { get; set; }
    public int NombreAncres { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
