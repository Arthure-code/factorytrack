using FactoryTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FactoryTrack.IntegrationTests;

/// <summary>
/// Instance TimescaleDB ephemere partagee entre tests d'une meme classe.
/// Choix delibere : PAS de base en memoire. Une base SQLite ou InMemory ne
/// reproduit ni les hypertables, ni le comportement DISTINCT ON, ni les
/// types Npgsql specifiques (timestamptz). Les faux positifs sur ces points
/// nous auraient exactement fait perdre le benefice du test d'integration.
/// </summary>
public abstract class BaseTimescaleDb : IAsyncLifetime
{
    private readonly PostgreSqlContainer _conteneur = new PostgreSqlBuilder()
        .WithImage("timescale/timescaledb:latest-pg16")
        .WithDatabase("factorytrack")
        .WithUsername("factorytrack")
        .WithPassword("factorytrack")
        .Build();

    protected string ChaineConnexion => _conteneur.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _conteneur.StartAsync();
        await ExecuterSchemaAsync();
    }

    public async Task DisposeAsync() => await _conteneur.DisposeAsync();

    protected AppDbContext CreerContexte()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ChaineConnexion)
            .Options;

        return new AppDbContext(options);
    }

    private async Task ExecuterSchemaAsync()
    {
        // On rejoue le schema declaratif du repo. Le fichier est reference en tant que
        // MSBuild None avec CopyToOutputDirectory, donc present a cote du binaire.
        var chemin = Path.Combine(AppContext.BaseDirectory, "01-schema.sql");
        var sql = await File.ReadAllTextAsync(chemin);

        await using var contexte = CreerContexte();
        await contexte.Database.OpenConnectionAsync();
        await contexte.Database.ExecuteSqlRawAsync(sql);
    }
}
