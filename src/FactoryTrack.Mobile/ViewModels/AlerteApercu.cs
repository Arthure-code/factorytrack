using CommunityToolkit.Mvvm.ComponentModel;
using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.ViewModels;

public partial class AlerteApercu : ObservableObject
{
    public string BaliseIdentifiant { get; }
    public string ZoneNom { get; }
    public DateTimeOffset Horodatage { get; }

    [ObservableProperty] private string codeEquipement;

    public string HeureFormattee => Horodatage.ToLocalTime().ToString("HH:mm:ss");

    public AlerteApercu(AlerteZoneDto source, string codeEquipement)
    {
        BaliseIdentifiant = source.BaliseIdentifiant;
        ZoneNom = source.ZoneNom;
        Horodatage = source.Horodatage;
        this.codeEquipement = codeEquipement;
    }
}
