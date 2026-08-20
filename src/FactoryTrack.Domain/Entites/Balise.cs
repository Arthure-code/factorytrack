using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

public class Balise
{
    public Guid Id { get; set; }
    public string Identifiant { get; set; } = string.Empty;
    public TypeTechnologie Technologie { get; set; }

    public double PuissanceReference { get; set; } = -59;

    public DateTimeOffset DateModification { get; set; }
}
