using CommunityToolkit.Mvvm.ComponentModel;
using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.ViewModels;

public partial class AlerteHistoriqueApercu : ObservableObject
{
    [ObservableProperty] private bool selectionnee;

    public Guid Id { get; }
    public string CodeEquipement { get; }
    public string BaliseIdentifiant { get; }
    public string ZoneNom { get; }
    public bool ZoneInterdite { get; }
    public bool ZonePerimetre { get; }
    public bool EstEntree { get; }
    public DateTimeOffset Horodatage { get; }

    public string TypeLibelle => (ZoneInterdite, ZonePerimetre) switch
    {
        (true, _) => EstEntree ? "Entree en zone interdite" : "Sortie de zone interdite",
        (_, true) => EstEntree ? "Sortie du perimetre" : "Retour dans le perimetre",
        _ => EstEntree ? "Entree" : "Sortie"
    };

    public string HorodatageFormatte => Horodatage.ToLocalTime().ToString("dd/MM HH:mm:ss");

    public bool EstAlerteRouge => (ZoneInterdite || ZonePerimetre) && EstEntree;

    public AlerteHistoriqueApercu(AlerteHistoriqueDto source)
    {
        Id = source.Id;
        CodeEquipement = source.CodeEquipement;
        BaliseIdentifiant = source.BaliseIdentifiant;
        ZoneNom = source.ZoneNom;
        ZoneInterdite = source.ZoneInterdite;
        ZonePerimetre = source.ZonePerimetre;
        EstEntree = source.EstEntree;
        Horodatage = source.Horodatage;
    }
}
