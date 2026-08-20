namespace FactoryTrack.Domain.Entites;

public class Zone
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Etage { get; set; }
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public bool Interdite { get; set; }

    public bool Perimetre { get; set; }

    public bool Contient(double x, double y, int etage) =>
        etage == Etage && x >= XMin && x <= XMax && y >= YMin && y <= YMax;
}
