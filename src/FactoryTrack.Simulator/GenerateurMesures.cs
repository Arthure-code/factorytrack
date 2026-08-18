using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Simulator;

/// <summary>
/// Chemin inverse du positionnement : a partir d'une position connue, produit
/// les RSSI qu'auraient mesures les passerelles. Permet de comparer la position
/// calculee par le back-end a la verite terrain.
/// </summary>
public class GenerateurMesures
{
    private readonly Random _alea;
    private readonly double _exposantPropagation;

    public GenerateurMesures(Random alea, double exposantPropagation = 2.8)
    {
        _alea = alea;
        _exposantPropagation = exposantPropagation;
    }

    public int CalculerRssi(double x, double y, Passerelle passerelle, double puissanceReference, double bruit)
    {
        var distance = Math.Max(0.1, Math.Sqrt(
            Math.Pow(x - passerelle.X, 2) + Math.Pow(y - passerelle.Y, 2)));

        var rssiTheorique = puissanceReference - 10 * _exposantPropagation * Math.Log10(distance);

        return (int)Math.Round(rssiTheorique + BruitGaussien() * bruit);
    }

    /// <summary>Box-Muller : le bruit radio suit une loi normale, pas uniforme.</summary>
    private double BruitGaussien()
    {
        var u1 = 1.0 - _alea.NextDouble();
        var u2 = 1.0 - _alea.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
