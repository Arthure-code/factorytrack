using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class IdempotenceTests
{
    [Fact]
    public void CleIdempotence_MesuresIdentiques_ProduitLaMemeCle()
    {
        var horodatage = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        var premiere = new MesureRssi("TAG-001", "GW-01", -70, TypeTechnologie.Bluetooth, horodatage);
        var doublon = new MesureRssi("TAG-001", "GW-01", -70, TypeTechnologie.Bluetooth, horodatage);

        doublon.CleIdempotence.Should().Be(premiere.CleIdempotence);
    }

    [Fact]
    public void CleIdempotence_PasserellesDifferentes_ProduitDesClesDistinctes()
    {
        var horodatage = DateTimeOffset.UtcNow;

        var depuisGw01 = new MesureRssi("TAG-001", "GW-01", -70, TypeTechnologie.Bluetooth, horodatage);
        var depuisGw02 = new MesureRssi("TAG-001", "GW-02", -70, TypeTechnologie.Bluetooth, horodatage);

        depuisGw01.CleIdempotence.Should().NotBe(depuisGw02.CleIdempotence);
    }

    [Fact]
    public void CleIdempotence_RssiDifferent_NeChangePasLaCle()
    {

        var horodatage = DateTimeOffset.UtcNow;

        var premiere = new MesureRssi("TAG-001", "GW-01", -70, TypeTechnologie.Bluetooth, horodatage);
        var seconde = new MesureRssi("TAG-001", "GW-01", -72, TypeTechnologie.Bluetooth, horodatage);

        seconde.CleIdempotence.Should().Be(premiere.CleIdempotence);
    }
}
