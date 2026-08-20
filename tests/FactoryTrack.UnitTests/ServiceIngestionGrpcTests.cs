using FactoryTrack.Contracts.Grpc;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using FactoryTrack.Infrastructure.Depots;
using FactoryTrack.Ingestion.Services;
using FactoryTrack.Positioning;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using TechnologieDomaine = FactoryTrack.Domain.Enums.TypeTechnologie;

namespace FactoryTrack.UnitTests;

public class ServiceIngestionGrpcTests
{
    private const double EXPOSANT = 2.8;
    private const int PUISSANCE_REFERENCE = -59;

    private static readonly List<Passerelle> Passerelles =
    [
        new() { Identifiant = "GW-01", X = 0, Y = 0, Etage = 0, Active = true },
        new() { Identifiant = "GW-02", X = 60, Y = 0, Etage = 0, Active = true },
        new() { Identifiant = "GW-03", X = 60, Y = 40, Etage = 0, Active = true },
        new() { Identifiant = "GW-04", X = 0, Y = 40, Etage = 0, Active = true }
    ];

    private static readonly Balise BaliseConnue = new()
    {
        Id = Guid.NewGuid(),
        Identifiant = "TAG-001",
        Technologie = TechnologieDomaine.Bluetooth,
        PuissanceReference = PUISSANCE_REFERENCE
    };

    [Fact]
    public async Task EnvoyerMesures_QuatreAncres_ProduitEtPublieUnePosition()
    {
        var banc = new BancEssai();
        var horodatage = DateTimeOffset.UtcNow;
        var messages = CreerMessagesPour(x: 30, y: 20, horodatage);

        var accuse = await banc.Service.EnvoyerMesures(new FluxLecture(messages), new ContexteAppelTest());

        accuse.Recues.Should().Be(4);
        accuse.Acceptees.Should().Be(4);
        accuse.PositionsCalculees.Should().Be(1);

        banc.DepotPositions.Enregistrees.Should().HaveCount(1);
        banc.Publicateur.Publiees.Should().HaveCount(1);
        banc.Publicateur.Publiees[0].X.Should().BeApproximately(30, 8);
        banc.Publicateur.Publiees[0].Y.Should().BeApproximately(20, 8);
    }

    [Fact]
    public async Task EnvoyerMesures_MesureDupliquee_EstRejeteeSansSecondePosition()
    {
        var banc = new BancEssai();
        var horodatage = DateTimeOffset.UtcNow;
        var messages = CreerMessagesPour(30, 20, horodatage);
        messages.Add(messages[0].Clone());

        var accuse = await banc.Service.EnvoyerMesures(new FluxLecture(messages), new ContexteAppelTest());

        accuse.Recues.Should().Be(5);
        accuse.RejeteesDoublon.Should().Be(1);
        accuse.PositionsCalculees.Should().Be(1);
        banc.DepotPositions.Enregistrees.Should().HaveCount(1);
    }

    [Fact]
    public async Task EnvoyerMesures_FluxPlusAncien_EstRejeteHorsOrdre()
    {
        var banc = new BancEssai();
        var horodatage = DateTimeOffset.UtcNow;

        await banc.Service.EnvoyerMesures(
            new FluxLecture(CreerMessagesPour(30, 20, horodatage)), new ContexteAppelTest());

        var accuse = await banc.Service.EnvoyerMesures(
            new FluxLecture(CreerMessagesPour(30, 20, horodatage.AddSeconds(-30))), new ContexteAppelTest());

        accuse.RejeteesHorsOrdre.Should().Be(1);
        accuse.PositionsCalculees.Should().Be(0);
        banc.DepotPositions.Enregistrees.Should().HaveCount(1);
    }

    [Fact]
    public async Task EnvoyerMesures_BaliseInconnue_NeProduitAucunePosition()
    {
        var banc = new BancEssai();
        var horodatage = DateTimeOffset.UtcNow;
        var messages = CreerMessagesPour(30, 20, horodatage);
        foreach (var message in messages)
            message.BaliseId = "TAG-FANTOME";

        var accuse = await banc.Service.EnvoyerMesures(new FluxLecture(messages), new ContexteAppelTest());

        accuse.Acceptees.Should().Be(4);
        accuse.PositionsCalculees.Should().Be(0);
        banc.DepotPositions.Enregistrees.Should().BeEmpty();
    }

    [Fact]
    public async Task EnvoyerMesures_MoinsDAncresQueLeMinimum_NeProduitRien()
    {
        var banc = new BancEssai();
        var horodatage = DateTimeOffset.UtcNow;
        var messages = CreerMessagesPour(30, 20, horodatage).Take(2).ToList();

        var accuse = await banc.Service.EnvoyerMesures(new FluxLecture(messages), new ContexteAppelTest());

        accuse.Acceptees.Should().Be(2);
        accuse.PositionsCalculees.Should().Be(0);
        banc.Publicateur.Publiees.Should().BeEmpty();
    }

    [Fact]
    public async Task Ping_RetourneLaVersionEtUnHorodatage()
    {
        var banc = new BancEssai();

        var reponse = await banc.Service.Ping(new RequetePing(), new ContexteAppelTest());

        reponse.Version.Should().Be("1.0.0");
        reponse.HorodatageServeur.Should().NotBeNull();
    }

    private static List<MesureRssiMessage> CreerMessagesPour(double x, double y, DateTimeOffset horodatage)
    {
        return Passerelles.Select(passerelle =>
        {
            var distance = Math.Max(0.1, Math.Sqrt(
                Math.Pow(x - passerelle.X, 2) + Math.Pow(y - passerelle.Y, 2)));

            var rssi = (int)Math.Round(
                PUISSANCE_REFERENCE - 10 * EXPOSANT * Math.Log10(distance));

            return new MesureRssiMessage
            {
                BaliseId = BaliseConnue.Identifiant,
                PasserelleId = passerelle.Identifiant,
                Rssi = rssi,
                Technologie = TypeTechnologie.Bluetooth,
                Horodatage = Timestamp.FromDateTimeOffset(horodatage)
            };
        }).ToList();
    }

    private sealed class BancEssai
    {
        public ServiceIngestionGrpc Service { get; }
        public FauxDepotPositions DepotPositions { get; } = new();
        public FauxPublicateur Publicateur { get; } = new();

        public BancEssai()
        {
            var options = Options.Create(new OptionsPositionnement());

            var services = new ServiceCollection();
            services.AddSingleton<IDepotReferentiel>(new FauxDepotReferentiel());
            services.AddSingleton<IDepotPositions>(DepotPositions);
            var fournisseur = services.BuildServiceProvider();

            Service = new ServiceIngestionGrpc(
                new ServicePositionnement(options.Value),
                new GardesIngestion(new GardeIdempotence(options), new GardeHorsOrdre()),
                Publicateur,
                fournisseur.GetRequiredService<IServiceScopeFactory>(),
                new CacheReferentiel(fournisseur),
                options,
                NullLogger<ServiceIngestionGrpc>.Instance);
        }
    }

    private sealed class FauxDepotReferentiel : IDepotReferentiel
    {
        public Task<IReadOnlyList<Passerelle>> ObtenirPasserellesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Passerelle>>(Passerelles);

        public Task<IReadOnlyList<Balise>> ObtenirBalisesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Balise>>([BaliseConnue]);

        public Task<IReadOnlyList<Equipement>> ObtenirEquipementsAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Equipement>>([]);

        public Task<IReadOnlyList<Zone>> ObtenirZonesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Zone>>([]);

        public Task<IReadOnlyList<MachineFixe>> ObtenirMachinesAsync(CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<MachineFixe>>([]);
    }

    private sealed class FauxDepotPositions : IDepotPositions
    {
        public List<Domain.Entites.Position> Enregistrees { get; } = [];

        public Task EnregistrerAsync(Domain.Entites.Position position, CancellationToken jeton = default)
        {
            Enregistrees.Add(position);
            return Task.CompletedTask;
        }

        public Task EnregistrerLotAsync(IReadOnlyCollection<Domain.Entites.Position> positions, CancellationToken jeton = default)
        {
            Enregistrees.AddRange(positions);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Domain.Entites.Position>> ObtenirDernieresAsync(int? etage, CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Domain.Entites.Position>>([]);

        public Task<IReadOnlyList<Domain.Entites.Position>> ObtenirHistoriqueAsync(
            Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default) =>
            Task.FromResult<IReadOnlyList<Domain.Entites.Position>>([]);
    }

    private sealed class FauxPublicateur : IPublicateurPositions
    {
        public List<Domain.Entites.Position> Publiees { get; } = [];

        public Task PublierAsync(Domain.Entites.Position position, CancellationToken jeton = default)
        {
            Publiees.Add(position);
            return Task.CompletedTask;
        }
    }

    private sealed class FluxLecture : IAsyncStreamReader<MesureRssiMessage>
    {
        private readonly IEnumerator<MesureRssiMessage> _enumerateur;

        public FluxLecture(IEnumerable<MesureRssiMessage> messages) => _enumerateur = messages.GetEnumerator();

        public MesureRssiMessage Current => _enumerateur.Current;

        public Task<bool> MoveNext(CancellationToken cancellationToken) =>
            Task.FromResult(_enumerateur.MoveNext());
    }

    private sealed class ContexteAppelTest : ServerCallContext
    {
        protected override string MethodCore => "EnvoyerMesures";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore { get; } = [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore { get; } = new(null, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;
    }
}
