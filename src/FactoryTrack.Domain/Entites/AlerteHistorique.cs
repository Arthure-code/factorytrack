namespace FactoryTrack.Domain.Entites;

public class AlerteHistorique
{
    public Guid Id { get; set; }
    public string BaliseIdentifiant { get; set; } = string.Empty;
    public string CodeEquipement { get; set; } = string.Empty;
    public Guid ZoneId { get; set; }
    public string ZoneNom { get; set; } = string.Empty;
    public bool ZoneInterdite { get; set; }
    public bool ZonePerimetre { get; set; }

    public bool EstEntree { get; set; }

    public int Etage { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
