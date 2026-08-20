namespace FactoryTrack.Positioning;

public static class CalculateurDistance
{
    private const double DISTANCE_MINIMALE = 0.1;
    private const double DISTANCE_MAXIMALE = 60.0;

    public static double Convertir(int rssi, double puissanceReference, double exposantPropagation)
    {
        if (exposantPropagation <= 0)
            throw new ArgumentOutOfRangeException(nameof(exposantPropagation),
                "L'exposant de propagation doit etre strictement positif.");

        var distance = Math.Pow(10, (puissanceReference - rssi) / (10 * exposantPropagation));

        return Math.Clamp(distance, DISTANCE_MINIMALE, DISTANCE_MAXIMALE);
    }
}
