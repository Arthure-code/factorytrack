using System.Net;
using System.Net.Http.Json;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Enums;
using FactoryTrack.Infrastructure.Depots;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FactoryTrack.IntegrationTests;

public class EndpointsIntegrationTests : BaseTimescaleDb, IAsyncLifetime
{
    private WebApplicationFactory<Program>? _fabrique;
    private Guid _baliseId;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SemerReferentielAsync();

        _fabrique = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:FactoryTrack", ChaineConnexion);
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {

                    var hosted = services
                        .Where(s => s.ServiceType.FullName?.EndsWith("IHostedService") == true)
                        .ToList();
                    foreach (var d in hosted)
                        services.Remove(d);
                });
            });
    }

    public new Task DisposeAsync()
    {
        _fabrique?.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Health_RetourneHealthy()
    {
        var client = _fabrique!.CreateClient();
        var reponse = await client.GetAsync("/health");

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reponse.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task GetEquipements_RetourneListeAvecSeeds()
    {
        var client = _fabrique!.CreateClient();
        var equipements = await client.GetFromJsonAsync<List<EquipementDto>>("/api/equipements");

        equipements.Should().NotBeNull().And.HaveCountGreaterThan(0);
        equipements!.Should().Contain(e => e.Code == "EQ-INT-01");
    }

    [Fact]
    public async Task GetPasserelles_RetourneListe()
    {
        var client = _fabrique!.CreateClient();
        var passerelles = await client.GetFromJsonAsync<List<PasserelleDto>>("/api/referentiel/passerelles");

        passerelles.Should().NotBeNull().And.HaveCount(1);
        passerelles![0].Identifiant.Should().Be("GW-TEST");
    }

    [Fact]
    public async Task GetZones_RetourneListe()
    {
        var client = _fabrique!.CreateClient();
        var zones = await client.GetFromJsonAsync<List<ZoneDto>>("/api/referentiel/zones");

        zones.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMachines_RetourneListe()
    {
        var client = _fabrique!.CreateClient();
        var machines = await client.GetFromJsonAsync<List<MachineFixeDto>>("/api/referentiel/machines");

        machines.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoriquePositions_IntervalleValide_RetourneListe()
    {
        var client = _fabrique!.CreateClient();
        var baliseId = Guid.NewGuid();
        var fin = DateTimeOffset.UtcNow;
        var debut = fin.AddMinutes(-10);

        var url = $"/api/positions/historique/{baliseId}" +
                  $"?debut={Uri.EscapeDataString(debut.ToString("o"))}" +
                  $"&fin={Uri.EscapeDataString(fin.ToString("o"))}";

        var positions = await client.GetFromJsonAsync<List<PositionDto>>(url);

        positions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoriquePositions_DebutApresFin_Retourne400()
    {
        var client = _fabrique!.CreateClient();
        var baliseId = Guid.NewGuid();
        var maintenant = DateTimeOffset.UtcNow;

        var url = $"/api/positions/historique/{baliseId}" +
                  $"?debut={Uri.EscapeDataString(maintenant.AddHours(1).ToString("o"))}" +
                  $"&fin={Uri.EscapeDataString(maintenant.ToString("o"))}";

        var reponse = await client.GetAsync(url);

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAlertes_ListeVide_Initialement()
    {
        var client = _fabrique!.CreateClient();
        var alertes = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>("/api/alertes");

        alertes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task DeleteAlerte_Inexistante_Retourne404()
    {
        var client = _fabrique!.CreateClient();

        var reponse = await client.DeleteAsync($"/api/alertes/{Guid.NewGuid()}");

        reponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAlertes_SansCritere_Retourne400()
    {
        var client = _fabrique!.CreateClient();

        var reponse = await client.DeleteAsync("/api/alertes");

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SemerReferentielAsync()
    {
        await using var contexte = CreerContexte();

        var passerelle = new Passerelle
        {
            Id = Guid.NewGuid(),
            Identifiant = "GW-TEST",
            X = 0,
            Y = 0,
            Etage = 0,
            Active = true
        };
        var balise = new Balise
        {
            Id = Guid.NewGuid(),
            Identifiant = "TAG-INT-01",
            Technologie = TypeTechnologie.Bluetooth,
            PuissanceReference = -59
        };
        var equipement = new Equipement
        {
            Id = Guid.NewGuid(),
            Code = "EQ-INT-01",
            Nom = "Equipement de test",
            Categorie = "Test",
            BaliseId = balise.Id,
            Etat = EtatEquipement.Actif,
            DateModification = DateTimeOffset.UtcNow
        };

        contexte.Passerelles.Add(passerelle);
        contexte.Balises.Add(balise);
        contexte.Equipements.Add(equipement);
        await contexte.SaveChangesAsync();

        _baliseId = balise.Id;
    }

    [Fact]
    public async Task GetDernieresPositions_EtEquipements_RefletentLaPositionEnregistree()
    {
        await using (var contexte = CreerContexte())
        {
            var depot = new DepotPositions(contexte);
            await depot.EnregistrerAsync(new Domain.Entites.Position
            {
                BaliseId = _baliseId,
                BaliseIdentifiant = "TAG-INT-01",
                X = 12.5,
                Y = 7.5,
                Etage = 0,
                Precision = 2.0,
                Technologie = TypeTechnologie.Bluetooth,
                NombreAncres = 4,
                Horodatage = DateTimeOffset.UtcNow
            });
        }

        var client = _fabrique!.CreateClient();

        var positions = await client.GetFromJsonAsync<List<PositionDto>>("/api/positions/etage/0");
        positions.Should().NotBeNull().And.ContainSingle(p => p.BaliseIdentifiant == "TAG-INT-01");
        positions![0].X.Should().Be(12.5);

        var equipements = await client.GetFromJsonAsync<List<EquipementDto>>("/api/equipements");
        var equipement = equipements!.Single(e => e.Code == "EQ-INT-01");
        equipement.DernierePosition.Should().NotBeNull();
        equipement.DernierePosition!.Y.Should().Be(7.5);
        equipement.Silencieux.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistoriquePositions_IntervalleTropLarge_Retourne400()
    {
        var client = _fabrique!.CreateClient();
        var fin = DateTimeOffset.UtcNow;
        var debut = fin.AddHours(-25);

        var url = $"/api/positions/historique/{Guid.NewGuid()}" +
                  $"?debut={Uri.EscapeDataString(debut.ToString("o"))}" +
                  $"&fin={Uri.EscapeDataString(fin.ToString("o"))}";

        var reponse = await client.GetAsync(url);

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Alertes_CycleDeVieComplet_FiltresSuppressionsEtLot()
    {
        var zoneInterdite = Guid.NewGuid();
        var zonePerimetre = Guid.NewGuid();
        var reference = DateTimeOffset.UtcNow;
        Guid alerteIndividuelle;

        await using (var contexte = CreerContexte())
        {
            var alertes = new[]
            {
                CreerAlerte("TAG-A", zoneInterdite, reference.AddMinutes(-10)),
                CreerAlerte("TAG-A", zoneInterdite, reference.AddMinutes(-5)),
                CreerAlerte("TAG-B", zonePerimetre, reference.AddMinutes(-1))
            };
            contexte.Alertes.AddRange(alertes);
            await contexte.SaveChangesAsync();
            alerteIndividuelle = alertes[2].Id;
        }

        var client = _fabrique!.CreateClient();

        var toutes = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>("/api/alertes");
        toutes.Should().HaveCount(3);
        toutes![0].BaliseIdentifiant.Should().Be("TAG-B", "le journal est trie du plus recent au plus ancien");

        var parBalise = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>("/api/alertes?baliseId=TAG-A");
        parBalise.Should().HaveCount(2);

        var parZone = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>($"/api/alertes?zoneId={zonePerimetre}");
        parZone.Should().ContainSingle().Which.BaliseIdentifiant.Should().Be("TAG-B");

        var debut = Uri.EscapeDataString(reference.AddMinutes(-7).ToString("o"));
        var fin = Uri.EscapeDataString(reference.ToString("o"));
        var parIntervalle = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>(
            $"/api/alertes?debut={debut}&fin={fin}&limite=10");
        parIntervalle.Should().HaveCount(2);

        var suppression = await client.DeleteAsync($"/api/alertes/{alerteIndividuelle}");
        suppression.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var suppressionLot = await client.DeleteAsync("/api/alertes?baliseId=TAG-A");
        suppressionLot.StatusCode.Should().Be(HttpStatusCode.OK);

        var restantes = await client.GetFromJsonAsync<List<AlerteHistoriqueDto>>("/api/alertes");
        restantes.Should().BeEmpty();
    }

    [Fact]
    public async Task DepotAlertes_SupprimerLotSansCritere_LeveArgumentException()
    {
        await using var contexte = CreerContexte();
        var depot = new DepotAlertes(contexte);

        var action = async () => await depot.SupprimerLotAsync();

        await action.Should().ThrowAsync<ArgumentException>();
    }

    private static AlerteHistorique CreerAlerte(string balise, Guid zoneId, DateTimeOffset horodatage) => new()
    {
        Id = Guid.NewGuid(),
        BaliseIdentifiant = balise,
        CodeEquipement = "EQ-INT-01",
        ZoneId = zoneId,
        ZoneNom = "Zone de test",
        ZoneInterdite = true,
        ZonePerimetre = false,
        EstEntree = true,
        Etage = 0,
        Horodatage = horodatage
    };
}
