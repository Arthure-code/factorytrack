using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Enums;
using FactoryTrack.Infrastructure.Depots;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.IntegrationTests;

/// <summary>
/// Le depot est teste sur une vraie hypertable TimescaleDB. Deux invariants
/// non triviaux : le DISTINCT ON par balise doit rendre la plus recente ;
/// et une insertion en doublon (meme balise + meme horodatage) doit etre
/// rejetee au niveau contrainte, pas silencieusement acceptee.
/// </summary>
public class DepotPositionsIntegrationTests : BaseTimescaleDb
{
    [Fact]
    public async Task ObtenirDernieres_TousEtages_RetourneUneLignePar_Balise()
    {
        await using var contexte = CreerContexte();
        var depot = new DepotPositions(contexte);

        var balise = Guid.NewGuid();
        var baseInstant = DateTimeOffset.UtcNow.AddMinutes(-10);

        for (var i = 0; i < 5; i++)
        {
            await depot.EnregistrerAsync(new Position
            {
                BaliseId = balise,
                BaliseIdentifiant = "TAG-INT-01",
                X = i, Y = i,
                Etage = 0,
                Precision = 2.5,
                Technologie = TypeTechnologie.Bluetooth,
                NombreAncres = 4,
                Horodatage = baseInstant.AddSeconds(i * 10)
            });
        }

        var dernieres = await depot.ObtenirDernieresAsync(etage: null);

        dernieres.Should().HaveCount(1);
        dernieres[0].X.Should().Be(4);
        dernieres[0].Y.Should().Be(4);
    }

    [Fact]
    public async Task Enregistrer_MemeBaliseMemeHorodatage_LeveConflit()
    {
        await using var contexte = CreerContexte();
        var depot = new DepotPositions(contexte);

        var balise = Guid.NewGuid();
        var horodatage = DateTimeOffset.UtcNow;

        await depot.EnregistrerAsync(Fabriquer(balise, horodatage, x: 1));

        var doublon = Fabriquer(balise, horodatage, x: 2);

        // On veut prouver que la contrainte primaire (BaliseId, Horodatage) protege
        // le stockage : sans elle, la surveillance du silence et le DISTINCT ON
        // deviendraient non deterministes.
        var action = async () =>
        {
            await using var autreContexte = CreerContexte();
            var autreDepot = new DepotPositions(autreContexte);
            await autreDepot.EnregistrerAsync(doublon);
        };

        await action.Should().ThrowAsync<Exception>();
    }

    private static Position Fabriquer(Guid balise, DateTimeOffset horodatage, double x) => new()
    {
        BaliseId = balise,
        BaliseIdentifiant = "TAG-INT-02",
        X = x, Y = 0,
        Etage = 0,
        Precision = 3,
        Technologie = TypeTechnologie.Bluetooth,
        NombreAncres = 4,
        Horodatage = horodatage
    };
}
