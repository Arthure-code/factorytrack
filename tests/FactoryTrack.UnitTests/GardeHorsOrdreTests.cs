using FactoryTrack.Ingestion.Services;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class GardeHorsOrdreTests
{
    [Fact]
    public void Accepter_PremiereMesure_EstToujoursAcceptee()
    {
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        var accepte = garde.Accepter("TAG-001", maintenant);

        accepte.Should().BeTrue();
    }

    [Fact]
    public void Accepter_MesurePlusRecente_EstAcceptee()
    {
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        var accepte = garde.Accepter("TAG-001", maintenant.AddSeconds(1));

        accepte.Should().BeTrue();
    }

    [Fact]
    public void Accepter_MesurePlusAncienne_EstRefusee()
    {
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        var accepte = garde.Accepter("TAG-001", maintenant.AddSeconds(-1));

        accepte.Should().BeFalse();
    }

    [Fact]
    public void Accepter_MemeHorodatageExact_EstRefusee()
    {

        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        var accepte = garde.Accepter("TAG-001", maintenant);

        accepte.Should().BeFalse();
    }

    [Fact]
    public void Accepter_BalisesDifferentes_NInterferentPas()
    {

        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-A", maintenant.AddSeconds(10));
        var accepte = garde.Accepter("TAG-B", maintenant);

        accepte.Should().BeTrue();
    }

    [Fact]
    public void Accepter_MesurePlusAncienneApresRejet_EtatConserve()
    {

        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        garde.Accepter("TAG-001", maintenant.AddSeconds(-5));

        var accepteApresRejet = garde.Accepter("TAG-001", maintenant.AddSeconds(1));

        accepteApresRejet.Should().BeTrue();
    }
}
