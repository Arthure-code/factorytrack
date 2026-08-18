using FactoryTrack.Domain.Entites;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class ZoneTests
{
    private static Zone CreerZone() => new()
    {
        Nom = "Local electrique",
        Etage = 0,
        XMin = 52, YMin = 32, XMax = 60, YMax = 40,
        Interdite = true
    };

    [Theory]
    [InlineData(55, 35, true)]
    [InlineData(52, 32, true)]   // coin inferieur, inclusif
    [InlineData(60, 40, true)]   // coin superieur, inclusif
    [InlineData(51.9, 35, false)]
    [InlineData(30, 20, false)]
    public void Contient_SelonLaPosition_RetourneLeBonResultat(double x, double y, bool attendu)
    {
        CreerZone().Contient(x, y, etage: 0).Should().Be(attendu);
    }

    [Fact]
    public void Contient_MemeCoordonneesMaisAutreEtage_RetourneFaux()
    {
        CreerZone().Contient(55, 35, etage: 1).Should().BeFalse();
    }
}
