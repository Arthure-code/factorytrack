namespace FactoryTrack.Domain.Entites;

/// <summary>
/// Rectangle du plan. Trois roles possibles selon les flags :
///   - Interdite = true : entree dans la zone declenche l'alerte
///   - Perimetre = true : sortie de la zone declenche l'alerte
///     (la zone definit la surface OU l'equipement DOIT rester)
///   - Ni l'un ni l'autre : zone d'information (production, quai...)
/// </summary>
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

    /// <summary>Vrai si la zone est un perimetre de securite : l'alerte est
    /// declenchee quand l'equipement en sort, pas quand il y entre.</summary>
    public bool Perimetre { get; set; }

    public bool Contient(double x, double y, int etage) =>
        etage == Etage && x >= XMin && x <= XMax && y >= YMin && y <= YMax;
}
