using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Interfaces;

namespace FactoryTrack.Api.Endpoints;

public static class PositionsEndpoints
{
    private const int HEURES_HISTORIQUE_MAXIMUM = 24;

    public static IEndpointRouteBuilder MapperPositions(this IEndpointRouteBuilder routes)
    {
        var groupe = routes.MapGroup("/api/positions").WithTags("Positions");

        groupe.MapGet("/etage/{etage:int}", ObtenirDernieres)
              .WithName("ObtenirDernieresPositions")
              .WithSummary("Dernieres positions connues d'un etage. Sert a la resynchronisation apres reconnexion.");

        groupe.MapGet("/historique/{baliseId:guid}", ObtenirHistorique)
              .WithName("ObtenirHistorique")
              .WithSummary("Trajectoire d'une balise sur un intervalle.");

        return routes;
    }

    private static async Task<IResult> ObtenirDernieres(
        int etage, IDepotPositions depot, CancellationToken jeton)
    {
        var positions = await depot.ObtenirDernieresAsync(etage, jeton);
        return Results.Ok(positions.Select(Convertir));
    }

    private static async Task<IResult> ObtenirHistorique(
        Guid baliseId,
        DateTimeOffset? debut,
        DateTimeOffset? fin,
        IDepotPositions depot,
        CancellationToken jeton)
    {
        var borneFin = fin ?? DateTimeOffset.UtcNow;
        var borneDebut = debut ?? borneFin.AddMinutes(-30);

        if (borneDebut >= borneFin)
            return Results.Problem(
                title: "Intervalle invalide",
                detail: "La borne de debut doit preceder la borne de fin.",
                statusCode: StatusCodes.Status400BadRequest);

        if ((borneFin - borneDebut).TotalHours > HEURES_HISTORIQUE_MAXIMUM)
            return Results.Problem(
                title: "Intervalle trop large",
                detail: $"L'intervalle ne peut exceder {HEURES_HISTORIQUE_MAXIMUM} heures.",
                statusCode: StatusCodes.Status400BadRequest);

        var positions = await depot.ObtenirHistoriqueAsync(baliseId, borneDebut, borneFin, jeton);
        return Results.Ok(positions.Select(Convertir));
    }

    private static PositionDto Convertir(Domain.Entites.Position p) => new(
        p.BaliseIdentifiant, p.X, p.Y, p.Etage, p.Precision,
        p.Technologie.ToString(), p.NombreAncres, p.Horodatage);
}
