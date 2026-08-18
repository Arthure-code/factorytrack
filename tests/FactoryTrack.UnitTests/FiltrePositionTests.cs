using FactoryTrack.Positioning;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class FiltrePositionTests
{
    [Fact]
    public void Lisser_PremiereMesure_RetourneLaValeurBrute()
    {
        var filtre = new FiltrePosition(alpha: 0.35, sautMaximalMetres: 8);

        var (x, y) = filtre.Lisser("TAG-001", 10, 20);

        x.Should().Be(10);
        y.Should().Be(20);
    }

    [Fact]
    public void Lisser_MesureSuivante_TendVersLaNouvelleValeurSansLAtteindre()
    {
        var filtre = new FiltrePosition(alpha: 0.5, sautMaximalMetres: 100);
        filtre.Lisser("TAG-001", 0, 0);

        var (x, _) = filtre.Lisser("TAG-001", 10, 0);

        x.Should().BeApproximately(5, 0.001);
    }

    [Fact]
    public void Lisser_SautAberrant_EstFortementAmorti()
    {
        var filtre = new FiltrePosition(alpha: 0.5, sautMaximalMetres: 5);
        filtre.Lisser("TAG-001", 0, 0);

        // Un bond de 50 m est physiquement impossible pour un chariot.
        var (x, _) = filtre.Lisser("TAG-001", 50, 0);

        x.Should().BeLessThan(10, "un saut aberrant ne doit pas etre suivi");
    }

    [Fact]
    public void Lisser_BalisesDifferentes_NInterferentPas()
    {
        var filtre = new FiltrePosition(alpha: 0.5, sautMaximalMetres: 100);
        filtre.Lisser("TAG-001", 0, 0);

        var (x, y) = filtre.Lisser("TAG-002", 30, 40);

        x.Should().Be(30);
        y.Should().Be(40);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    [InlineData(-0.2)]
    public void Constructeur_AlphaHorsIntervalle_LeveUneException(double alpha)
    {
        var action = () => new FiltrePosition(alpha, sautMaximalMetres: 8);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
