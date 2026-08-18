using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

/// <summary>Emetteur fixe sur un equipement. N'emet que son identifiant.</summary>
public class Balise
{
    public Guid Id { get; set; }
    public string Identifiant { get; set; } = string.Empty;
    public TypeTechnologie Technologie { get; set; }

    /// <summary>Puissance mesuree a un metre, en dBm. Sert au calcul de distance.</summary>
    public double PuissanceReference { get; set; } = -59;

    public DateTimeOffset DateModification { get; set; }
}
