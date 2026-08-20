using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Infrastructure.Depots;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FactoryTrack.UnitTests;

public class CacheReferentielTests
{
    [Fact]
    public async Task ObtenirAsync_DeuxAppelsConsecutifs_NInterrogeLeDepotQuUneFois()
    {
        var depot = new DepotCompteur();
        var cache = new CacheReferentiel(new ServiceCollection().BuildServiceProvider());

        var premier = await cache.ObtenirAsync(depot);
        var second = await cache.ObtenirAsync(depot);

        depot.Appels.Should().Be(1);
        premier.Passerelles.Should().ContainKey("GW-01");
        second.Balises.Should().ContainKey("TAG-001");
    }

    [Fact]
    public async Task Invalider_ForceUnRechargementAuProchainAppel()
    {
        var depot = new DepotCompteur();
        var cache = new CacheReferentiel(new ServiceCollection().BuildServiceProvider());

        await cache.ObtenirAsync(depot);
        cache.Invalider();
        await cache.ObtenirAsync(depot);

        depot.Appels.Should().Be(2);
    }

    private sealed class DepotCompteur : IDepotReferentiel
    {
        public int Appels { get; private set; }

        public Task<IReadOnlyList<Passerelle>> ObtenirPasserellesAsync(CancellationToken jeton = default)
        {
            Appels++;
            return Task.FromResult<IReadOnlyList<Passerelle>>(
                [new Passerelle { Identifiant = "GW-01", X = 0, Y = 0, Etage = 0, Active = true }]);
        }

        public Task<IReadOnlyList<Balise>> ObtenirBalisesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Balise>>(
                [new Balise { Identifiant = "TAG-001", PuissanceReference = -59 }]);

        public Task<IReadOnlyList<Equipement>> ObtenirEquipementsAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Equipement>>([]);

        public Task<IReadOnlyList<Zone>> ObtenirZonesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Zone>>([]);

        public Task<IReadOnlyList<MachineFixe>> ObtenirMachinesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<MachineFixe>>([]);
    }
}
