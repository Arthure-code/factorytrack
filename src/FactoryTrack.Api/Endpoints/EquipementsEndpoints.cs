using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using Microsoft.Extensions.Options;

namespace FactoryTrack.Api.Endpoints;

public static class EquipementsEndpoints
{
    public static IEndpointRouteBuilder MapperEquipements(this IEndpointRouteBuilder routes)
    {
        var groupe = routes.MapGroup("/api/equipements").WithTags("Equipements");

        groupe.MapGet("/", ObtenirEquipements)
              .WithName("ObtenirEquipements")
              .WithSummary("Liste les equipements et leur derniere position connue.");

        return routes;
    }

    private static async Task<IResult> ObtenirEquipements(
        IDepotReferentiel depotReferentiel,
        IDepotPositions depotPositions,
        IOptions<OptionsPositionnement> options,
        CancellationToken jeton)
    {
        var equipements = await depotReferentiel.ObtenirEquipementsAsync(jeton);
        var dernieres = await depotPositions.ObtenirDernieresAsync(etage: null, jeton);

        var parBalise = dernieres.ToDictionary(p => p.BaliseIdentifiant);
        var limite = DateTimeOffset.UtcNow.AddSeconds(-options.Value.DelaiSilenceSecondes);

        var resultat = equipements.Select(equipement =>
        {
            var identifiant = equipement.Balise?.Identifiant;
            PositionDto? position = null;
            var silencieux = true;

            if (identifiant is not null && parBalise.TryGetValue(identifiant, out var derniere))
            {
                position = new PositionDto(
                    derniere.BaliseIdentifiant, derniere.X, derniere.Y, derniere.Etage,
                    derniere.Precision, derniere.Technologie.ToString(),
                    derniere.NombreAncres, derniere.Horodatage);

                silencieux = derniere.Horodatage < limite;
            }

            return new EquipementDto(
                equipement.Id, equipement.Code, equipement.Nom, equipement.Categorie,
                equipement.BaliseId, identifiant, equipement.Etat.ToString(), position, silencieux);
        });

        return Results.Ok(resultat);
    }
}
