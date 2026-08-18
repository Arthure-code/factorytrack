namespace FactoryTrack.Domain.Entites;

/// <summary>
/// Poste stationnaire du plan : presse, tour, robot, ligne d'assemblage.
/// Une machine occupe une emprise physique rectangulaire, contrairement a
/// un equipement mobile qui est un point suivi par sa balise.
/// </summary>
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
