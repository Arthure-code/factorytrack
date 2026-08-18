using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Infrastructure.Depots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryTrack.Infrastructure;

public static class InjectionDependances
{
    public const string CHAINE_CONNEXION = "FactoryTrack";

    public static IServiceCollection AjouterInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString(CHAINE_CONNEXION),
                npgsql => npgsql.EnableRetryOnFailure(3)));

        services.AddScoped<IDepotPositions, DepotPositions>();
        services.AddScoped<IDepotReferentiel, DepotReferentiel>();
        services.AddScoped<IDepotAlertes, DepotAlertes>();
        services.AddSingleton<CacheReferentiel>();

        return services;
    }
}
