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

/// <summary>
/// Instancie l'API ASP.NET Core au-dessus du TimescaleDB ephemere et interroge
/// chaque endpoint reel. Couvre Program.cs, Endpoints/*, DepotPositions et
/// DepotReferentiel en un seul aller-retour HTTP par cas.
/// </summary>
public class EndpointsIntegrationTests : BaseTimescaleDb, IAsyncLifetime
{
    private WebApplicationFactory<Program>? _fabrique;

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
                    // Neutralise les hosted services : ils demanderaient un cycle
                    // de vie complet inutile pour tester les endpoints REST.
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
            X = 0, Y = 0, Etage = 0,
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
    }
}
