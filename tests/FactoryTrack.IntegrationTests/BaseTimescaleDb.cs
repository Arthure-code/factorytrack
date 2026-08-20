using FactoryTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace FactoryTrack.IntegrationTests;

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

        var chemin = Path.Combine(AppContext.BaseDirectory, "01-schema.sql");
        var sql = await File.ReadAllTextAsync(chemin);

        await using var contexte = CreerContexte();
        await contexte.Database.OpenConnectionAsync();
        await contexte.Database.ExecuteSqlRawAsync(sql);
    }
}
