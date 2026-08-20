using FactoryTrack.Positioning;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class TrilaterationTests
{
    [Fact]
    public void Resoudre_TroisAncresExactes_RetrouveLaPosition()
    {

        var ancres = new List<Ancre>
        {
            new(0, 0, 0, Distance(0, 0, 10, 10)),
            new(20, 0, 0, Distance(20, 0, 10, 10)),
            new(0, 20, 0, Distance(0, 20, 10, 10))
        };

        var resultat = Trilateration.Resoudre(ancres, ancresMinimales: 3);

        resultat.Reussi.Should().BeTrue();
        resultat.X.Should().BeApproximately(10, 0.001);
        resultat.Y.Should().BeApproximately(10, 0.001);
        resultat.ResiduMoyen.Should().BeLessThan(0.01);
    }

    [Fact]
    public void Resoudre_MoinsDAncresQueLeMinimum_Echoue()
    {
        var ancres = new List<Ancre> { new(0, 0, 0, 5), new(10, 0, 0, 5) };

        var resultat = Trilateration.Resoudre(ancres, ancresMinimales: 3);

        resultat.Reussi.Should().BeFalse();
        resultat.Motif.Should().Contain("insuffisantes");
    }

    [Fact]
    public void Resoudre_AncresColineaires_Echoue()
    {

        var ancres = new List<Ancre>
        {
            new(0, 0, 0, 10),
            new(10, 0, 0, 10),
            new(20, 0, 0, 10)
        };

        var resultat = Trilateration.Resoudre(ancres, ancresMinimales: 3);

        resultat.Reussi.Should().BeFalse();
        resultat.Motif.Should().Contain("colineaires");
    }

    [Fact]
    public void Resoudre_DistancesBruitees_RetourneUnResiduNonNul()
    {
        var ancres = new List<Ancre>
        {
            new(0, 0, 0, Distance(0, 0, 10, 10) + 1.5),
            new(20, 0, 0, Distance(20, 0, 10, 10) - 1.2),
            new(0, 20, 0, Distance(0, 20, 10, 10) + 0.8),
            new(20, 20, 0, Distance(20, 20, 10, 10) - 0.6)
        };

        var resultat = Trilateration.Resoudre(ancres, ancresMinimales: 3);

        resultat.Reussi.Should().BeTrue();
        resultat.ResiduMoyen.Should().BeGreaterThan(0);
        resultat.X.Should().BeApproximately(10, 3);
        resultat.Y.Should().BeApproximately(10, 3);
    }

    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
}
