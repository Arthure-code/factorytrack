using FactoryTrack.Ingestion.Services;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

/// <summary>
/// Prouve les deux invariants du hors-ordre : une mesure plus ancienne est
/// refusee, une mesure au meme horodatage exact est aussi refusee (>= et
/// non >). Ces tests protegent contre une regression qui ferait reculer un
/// equipement sur le plan.
/// </summary>
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
        // Un evenement au meme instant est deja traite : on ne le rejoue pas.
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        var accepte = garde.Accepter("TAG-001", maintenant);

        accepte.Should().BeFalse();
    }

    [Fact]
    public void Accepter_BalisesDifferentes_NInterferentPas()
    {
        // Deux balises n'ont pas d'ordre commun : chacune a sa propre horloge.
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-A", maintenant.AddSeconds(10));
        var accepte = garde.Accepter("TAG-B", maintenant);

        accepte.Should().BeTrue();
    }

    [Fact]
    public void Accepter_MesurePlusAncienneApresRejet_EtatConserve()
    {
        // Un rejet ne doit pas ecraser le dernier horodatage accepte.
        var garde = new GardeHorsOrdre();
        var maintenant = DateTimeOffset.UtcNow;

        garde.Accepter("TAG-001", maintenant);
        garde.Accepter("TAG-001", maintenant.AddSeconds(-5)); // refuse

        var accepteApresRejet = garde.Accepter("TAG-001", maintenant.AddSeconds(1));

        accepteApresRejet.Should().BeTrue();
    }
}
