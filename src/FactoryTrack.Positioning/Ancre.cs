namespace FactoryTrack.Positioning;

/// <summary>Une passerelle et la distance estimee la separant de la balise.</summary>
public sealed record Ancre(double X, double Y, int Etage, double Distance);
