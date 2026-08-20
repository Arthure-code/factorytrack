namespace FactoryTrack.Contracts;

public static class NomsHub
{
    public const string Chemin = "/hubs/positions";

    public static class Methodes
    {
        public const string PositionMiseAJour = "PositionMiseAJour";
        public const string EquipementSilencieux = "EquipementSilencieux";
        public const string EquipementActif = "EquipementActif";
        public const string AlerteZoneEntree = "AlerteZoneEntree";
        public const string AlerteZoneSortie = "AlerteZoneSortie";
    }

    public static class Groupes
    {
        public static string Etage(int etage) => $"etage-{etage}";
    }
}
