using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Interfaces;

namespace FactoryTrack.Api.Endpoints;

public static class ReferentielEndpoints
{
    public static IEndpointRouteBuilder MapperReferentiel(this IEndpointRouteBuilder routes)
    {
        var groupe = routes.MapGroup("/api/referentiel").WithTags("Referentiel");

        groupe.MapGet("/passerelles", async (IDepotReferentiel depot, CancellationToken jeton) =>
        {
            var passerelles = await depot.ObtenirPasserellesAsync(jeton);
            return Results.Ok(passerelles.Select(p =>
                new PasserelleDto(p.Identifiant, p.X, p.Y, p.Etage, p.Active)));
        })
        .WithName("ObtenirPasserelles");

        groupe.MapGet("/zones", async (IDepotReferentiel depot, CancellationToken jeton) =>
        {
            var zones = await depot.ObtenirZonesAsync(jeton);
            return Results.Ok(zones.Select(z =>
                new ZoneDto(z.Id, z.Nom, z.Etage, z.XMin, z.YMin, z.XMax, z.YMax, z.Interdite, z.Perimetre)));
        })
        .WithName("ObtenirZones");

        groupe.MapGet("/machines", async (IDepotReferentiel depot, CancellationToken jeton) =>
        {
            var machines = await depot.ObtenirMachinesAsync(jeton);
            return Results.Ok(machines.Select(m =>
                new MachineFixeDto(m.Id, m.Code, m.Nom, m.Etage, m.X, m.Y, m.Largeur, m.Hauteur)));
        })
        .WithName("ObtenirMachines");

        return routes;
    }
}
