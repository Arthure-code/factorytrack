namespace FactoryTrack.Positioning;

/// <summary>
/// Resolution par moindres carres. En soustrayant l'equation de la premiere ancre
/// aux suivantes, le systeme quadratique devient lineaire :
///     2(xi - x0) X + 2(yi - y0) Y = d0^2 - di^2 + xi^2 - x0^2 + yi^2 - y0^2
/// resolu ensuite par les equations normales (A^T A) v = A^T b.
/// </summary>
public static class Trilateration
{
    private const double EPSILON = 1e-9;

    public static ResultatTrilateration Resoudre(IReadOnlyList<Ancre> ancres, int ancresMinimales)
    {
        ArgumentNullException.ThrowIfNull(ancres);

        if (ancres.Count < ancresMinimales)
            return ResultatTrilateration.Echec($"Ancres insuffisantes : {ancres.Count} recues, {ancresMinimales} requises.");

        var reference = ancres[0];
        var lignes = ancres.Count - 1;

        var a = new double[lignes, 2];
        var b = new double[lignes];

        for (var i = 1; i <= lignes; i++)
        {
            var courante = ancres[i];

            a[i - 1, 0] = 2 * (courante.X - reference.X);
            a[i - 1, 1] = 2 * (courante.Y - reference.Y);

            b[i - 1] = Carre(reference.Distance) - Carre(courante.Distance)
                     + Carre(courante.X) - Carre(reference.X)
                     + Carre(courante.Y) - Carre(reference.Y);
        }

        // Equations normales : matrice 2x2 symetrique, inversion directe.
        double a11 = 0, a12 = 0, a22 = 0, b1 = 0, b2 = 0;

        for (var i = 0; i < lignes; i++)
        {
            a11 += a[i, 0] * a[i, 0];
            a12 += a[i, 0] * a[i, 1];
            a22 += a[i, 1] * a[i, 1];
            b1 += a[i, 0] * b[i];
            b2 += a[i, 1] * b[i];
        }

        var determinant = a11 * a22 - a12 * a12;

        if (Math.Abs(determinant) < EPSILON)
            return ResultatTrilateration.Echec("Ancres colineaires ou confondues : systeme insoluble.");

        var x = (b1 * a22 - b2 * a12) / determinant;
        var y = (a11 * b2 - a12 * b1) / determinant;

        return new ResultatTrilateration(true, x, y, CalculerResidu(ancres, x, y));
    }

    /// <summary>Ecart moyen entre distances mesurees et distances a la solution. Sert d'indice de precision.</summary>
    private static double CalculerResidu(IReadOnlyList<Ancre> ancres, double x, double y)
    {
        var somme = 0.0;

        foreach (var ancre in ancres)
        {
            var distanceCalculee = Math.Sqrt(Carre(ancre.X - x) + Carre(ancre.Y - y));
            somme += Math.Abs(distanceCalculee - ancre.Distance);
        }

        return somme / ancres.Count;
    }

    private static double Carre(double valeur) => valeur * valeur;
}
