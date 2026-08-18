using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

public class Equipement
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Categorie { get; set; }
    public Guid? BaliseId { get; set; }
    public Balise? Balise { get; set; }
    public EtatEquipement Etat { get; set; }
    public DateTimeOffset DateModification { get; set; }

    public void MettreAJour(Equipement source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Code = source.Code;
        Nom = source.Nom;
        Categorie = source.Categorie;
        BaliseId = source.BaliseId;
        Etat = source.Etat;
        DateModification = source.DateModification;
    }
}
