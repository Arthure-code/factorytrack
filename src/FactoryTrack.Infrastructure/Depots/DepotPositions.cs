using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FactoryTrack.Infrastructure.Depots;

public class DepotPositions : IDepotPositions
{
    private readonly AppDbContext _contexte;

    public DepotPositions(AppDbContext contexte) => _contexte = contexte;

    public async Task EnregistrerAsync(Position position, CancellationToken jeton = default)
    {
        _contexte.Positions.Add(position);
        await _contexte.SaveChangesAsync(jeton);
    }

    public async Task EnregistrerLotAsync(IReadOnlyCollection<Position> positions, CancellationToken jeton = default)
    {
        if (positions.Count == 0)
            return;

        _contexte.Positions.AddRange(positions);
        await _contexte.SaveChangesAsync(jeton);
    }

    /// <summary>
    /// Derniere position connue par balise. DISTINCT ON est propre a PostgreSQL et
    /// bien plus rapide ici qu'un GROUP BY suivi d'une jointure.
    /// </summary>
    public async Task<IReadOnlyList<Position>> ObtenirDernieresAsync(int? etage, CancellationToken jeton = default)
    {
        // Le filtre par etage est optionnel : null equivaut a "tous etages confondus".
        // On passe -1 comme sentinelle inutilisee quand etage est null pour garder une
        // signature parametree (les parametres nuls sont mal supportes par FromSqlRaw).
        return await _contexte.Positions
            .FromSqlRaw(
                """
                SELECT DISTINCT ON ("BaliseId") *
                FROM positions
                WHERE ({0}::integer IS NULL OR "Etage" = {0}::integer)
                ORDER BY "BaliseId", "Horodatage" DESC
                """, etage.HasValue ? etage.Value : (object)DBNull.Value)
            .AsNoTracking()
            .ToListAsync(jeton);
    }

    public async Task<IReadOnlyList<Position>> ObtenirHistoriqueAsync(
        Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default)
    {
        return await _contexte.Positions
            .AsNoTracking()
            .Where(p => p.BaliseId == baliseId && p.Horodatage >= debut && p.Horodatage <= fin)
            .OrderBy(p => p.Horodatage)
            .ToListAsync(jeton);
    }
}
