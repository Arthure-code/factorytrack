namespace FactoryTrack.Contracts;

/// <summary>
/// Noms partages entre serveur et clients. Une faute de frappe dans une chaine
/// SignalR ne se voit qu'a l'execution : les centraliser evite la classe entiere de bogue.
/// </summary>
public static class NomsHub
{
    public const string Chemin = "/hubs/positions";

    public static class Methodes
    {
        public const string PositionMiseAJour = "PositionMiseAJour";
        public const string EquipementSilencieux = "EquipementSilencieux";
    }

    public static class Groupes
    {
        public static string Etage(int etage) => $"etage-{etage}";
    }
}
