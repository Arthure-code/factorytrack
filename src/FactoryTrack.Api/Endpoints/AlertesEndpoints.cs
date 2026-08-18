using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Interfaces;

namespace FactoryTrack.Api.Endpoints;

public static class AlertesEndpoints
{
    public static IEndpointRouteBuilder MapperAlertes(this IEndpointRouteBuilder routes)
    {
        var groupe = routes.MapGroup("/api/alertes").WithTags("Alertes");

        groupe.MapGet("/", ObtenirHistorique)
              .WithName("ObtenirHistoriqueAlertes")
              .WithSummary("Journal des transitions d'alertes, filtrable et paginable.");

        groupe.MapDelete("/{id:guid}", SupprimerUne)
              .WithName("SupprimerAlerte")
              .WithSummary("Suppression d'une entree individuelle.");

        groupe.MapDelete("/", SupprimerLot)
              .WithName("SupprimerLotAlertes")
              .WithSummary("Suppression par lot : avant une date, par zone ou par balise.");

        return routes;
    }

    private static async Task<IResult> ObtenirHistorique(
        DateTimeOffset? debut,
        DateTimeOffset? fin,
        Guid? zoneId,
        string? baliseId,
        int? limite,
        IDepotAlertes depot,
        CancellationToken jeton)
    {
        var alertes = await depot.ObtenirAsync(
            debut, fin, zoneId, baliseId, limite ?? 200, jeton);

        return Results.Ok(alertes.Select(a => new AlerteHistoriqueDto(
            a.Id, a.BaliseIdentifiant, a.CodeEquipement,
            a.ZoneId, a.ZoneNom, a.ZoneInterdite, a.ZonePerimetre,
            a.EstEntree, a.Etage, a.Horodatage)));
    }

    private static async Task<IResult> SupprimerUne(
        Guid id, IDepotAlertes depot, CancellationToken jeton)
    {
        var supprimees = await depot.SupprimerAsync(id, jeton);
        return supprimees == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> SupprimerLot(
        DateTimeOffset? avant,
        Guid? zoneId,
        string? baliseId,
        IDepotAlertes depot,
        CancellationToken jeton)
    {
        if (avant is null && zoneId is null && string.IsNullOrWhiteSpace(baliseId))
            return Results.Problem(
                title: "Aucun critere fourni",
                detail: "La suppression par lot exige au moins un des criteres : avant, zoneId, baliseId.",
                statusCode: StatusCodes.Status400BadRequest);

        var supprimees = await depot.SupprimerLotAsync(avant, zoneId, baliseId, jeton);
        return Results.Ok(new { supprimees });
    }
}
