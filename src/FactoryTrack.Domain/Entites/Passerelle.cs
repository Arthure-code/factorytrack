namespace FactoryTrack.Domain.Entites;

public class Passerelle
{
    public Guid Id { get; set; }
    public string Identifiant { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public int Etage { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset DateModification { get; set; }
}
