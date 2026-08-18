namespace FactoryTrack.Positioning;

public sealed record ResultatTrilateration(bool Reussi, double X, double Y, double ResiduMoyen, string? Motif = null)
{
    public static ResultatTrilateration Echec(string motif) => new(false, 0, 0, 0, motif);
}
