using FactoryTrack.Domain.Options;
using FactoryTrack.Ingestion.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FactoryTrack.UnitTests;

public class GardeIdempotenceTests
{
    private static GardeIdempotence CreerGarde(int retentionSecondes = 120)
    {
        var options = Options.Create(new OptionsPositionnement
        {
            RetentionIdempotenceSecondes = retentionSecondes
        });
        return new GardeIdempotence(options);
    }

    [Fact]
    public void Accepter_CleNouvelle_RetourneTrue()
    {
        using var garde = CreerGarde();

        var accepte = garde.Accepter("TAG-001|GW-01|1234567890");

        accepte.Should().BeTrue();
    }

    [Fact]
    public void Accepter_MemeCle_DeuxiemeFois_RetourneFalse()
    {
        using var garde = CreerGarde();
        const string cle = "TAG-001|GW-01|1234567890";

        garde.Accepter(cle);
        var accepteBis = garde.Accepter(cle);

        accepteBis.Should().BeFalse();
    }

    [Fact]
    public void Accepter_ClesDifferentes_AcceptesIndependamment()
    {
        using var garde = CreerGarde();

        garde.Accepter("TAG-001|GW-01|1").Should().BeTrue();
        garde.Accepter("TAG-001|GW-02|1").Should().BeTrue();
        garde.Accepter("TAG-002|GW-01|1").Should().BeTrue();
    }

    [Fact]
    public void Dispose_LibereRessources_SansException()
    {
        var garde = CreerGarde();

        var action = () => garde.Dispose();

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_AppelePlusieurs_Fois_EstIdempotent()
    {

        var garde = CreerGarde();

        var action = () =>
        {
            garde.Dispose();
            garde.Dispose();
        };

        action.Should().NotThrow();
    }
}
