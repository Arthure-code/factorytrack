using CommunityToolkit.Mvvm.ComponentModel;
using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.ViewModels;

/// <summary>
/// Vue courte d'un equipement pour le plan et la liste. Les DTO REST sont
/// immuables (records), on les enveloppe pour que la mise a jour temps reel
/// declenche l'affichage sans recreer la collection.
/// </summary>
public partial class EquipementApercu : ObservableObject
{
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private int etage;
    [ObservableProperty] private double precision;
    [ObservableProperty] private DateTimeOffset? horodatage;
    [ObservableProperty] private bool silencieux;
    [ObservableProperty] private bool aUnePosition;
    [ObservableProperty] private bool dansZoneInterdite;

    public string Id { get; }
    public string Code { get; }
    public string Nom { get; }
    public string? Categorie { get; }
    public Guid? BaliseId { get; }
    public string? BaliseIdentifiant { get; }

    public EquipementApercu(EquipementDto source)
    {
        Id = source.Id.ToString();
        Code = source.Code;
        Nom = source.Nom;
        Categorie = source.Categorie;
        BaliseId = source.BaliseId;
        BaliseIdentifiant = source.BaliseIdentifiant;
        Silencieux = source.Silencieux;

        if (source.DernierePosition is { } p)
        {
            AUnePosition = true;
            X = p.X;
            Y = p.Y;
            Etage = p.Etage;
            Precision = p.Precision;
            Horodatage = p.Horodatage;
        }
    }

    public void AppliquerPosition(PositionDto position)
    {
        AUnePosition = true;
        X = position.X;
        Y = position.Y;
        Etage = position.Etage;
        Precision = position.Precision;
        Horodatage = position.Horodatage;
        Silencieux = false;
    }
}
