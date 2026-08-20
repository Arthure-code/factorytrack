using FactoryTrack.Domain.Entites;

namespace FactoryTrack.Simulator;

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

    private double BruitGaussien()
    {
        var u1 = 1.0 - _alea.NextDouble();
        var u2 = 1.0 - _alea.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
