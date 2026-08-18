using FactoryTrack.Positioning;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class CalculateurDistanceTests
{
    private const double PUISSANCE_REFERENCE = -59;

    [Fact]
    public void Convertir_RssiEgalPuissanceReference_RetourneUnMetre()
    {
        var distance = CalculateurDistance.Convertir(-59, PUISSANCE_REFERENCE, 2.8);

        distance.Should().BeApproximately(1.0, 0.01);
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(-59)]
    [InlineData(-70)]
    [InlineData(-85)]
    public void Convertir_SignalPlusFaible_DonneUneDistancePlusGrande(int rssi)
    {
        var distance = CalculateurDistance.Convertir(rssi, PUISSANCE_REFERENCE, 2.8);
        var distancePlusFaible = CalculateurDistance.Convertir(rssi - 5, PUISSANCE_REFERENCE, 2.8);

        distancePlusFaible.Should().BeGreaterThan(distance);
    }

    [Fact]
    public void Convertir_RssiAberrant_RestBorne()
    {
        var distance = CalculateurDistance.Convertir(-120, PUISSANCE_REFERENCE, 2.8);

        distance.Should().BeLessThanOrEqualTo(60);
    }

    [Fact]
    public void Convertir_ExposantNul_LeveUneException()
    {
        var action = () => CalculateurDistance.Convertir(-70, PUISSANCE_REFERENCE, 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
