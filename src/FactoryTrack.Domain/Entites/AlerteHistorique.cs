namespace FactoryTrack.Domain.Entites;

/// <summary>
/// Consignation permanente d'une transition d'alerte (entree ou sortie).
/// Sert l'ecran d'historique cote client : audit, tri, filtrage, suppression
/// par lot. La source de verite reste SignalR pour le temps reel ; cette
/// table est le journal, pas le pipeline.
/// </summary>
public class AlerteHistorique
{
    public Guid Id { get; set; }
    public string BaliseIdentifiant { get; set; } = string.Empty;
    public string CodeEquipement { get; set; } = string.Empty;
    public Guid ZoneId { get; set; }
    public string ZoneNom { get; set; } = string.Empty;
    public bool ZoneInterdite { get; set; }
    public bool ZonePerimetre { get; set; }

    /// <summary>Vrai si l'evenement est une entree en alerte, faux si c'est une sortie.</summary>
    public bool EstEntree { get; set; }

    public int Etage { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
