namespace FactoryTrack.Domain.Entites;

public class MachineFixe
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public int Etage { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Largeur { get; set; }
    public double Hauteur { get; set; }
}
