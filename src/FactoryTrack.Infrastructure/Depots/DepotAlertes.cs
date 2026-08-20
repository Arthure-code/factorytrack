using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FactoryTrack.Infrastructure.Depots;

public class DepotAlertes : IDepotAlertes
{
    private readonly AppDbContext _contexte;

    public DepotAlertes(AppDbContext contexte) => _contexte = contexte;

    public async Task EnregistrerAsync(AlerteHistorique alerte, CancellationToken jeton = default)
    {
        _contexte.Alertes.Add(alerte);
        await _contexte.SaveChangesAsync(jeton);
    }

    public async Task<IReadOnlyList<AlerteHistorique>> ObtenirAsync(
        DateTimeOffset? debut = null,
        DateTimeOffset? fin = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        int limite = 200,
        CancellationToken jeton = default)
    {
        var requete = _contexte.Alertes.AsNoTracking().AsQueryable();

        if (debut.HasValue) requete = requete.Where(a => a.Horodatage >= debut.Value);
        if (fin.HasValue) requete = requete.Where(a => a.Horodatage <= fin.Value);
        if (zoneId.HasValue) requete = requete.Where(a => a.ZoneId == zoneId.Value);
        if (!string.IsNullOrWhiteSpace(baliseIdentifiant))
            requete = requete.Where(a => a.BaliseIdentifiant == baliseIdentifiant);

        return await requete
            .OrderByDescending(a => a.Horodatage)
            .Take(Math.Clamp(limite, 1, 1000))
            .ToListAsync(jeton);
    }

    public async Task<int> SupprimerAsync(Guid id, CancellationToken jeton = default)
    {
        return await _contexte.Alertes
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync(jeton);
    }

    public async Task<int> SupprimerLotAsync(
        DateTimeOffset? avant = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        CancellationToken jeton = default)
    {

        if (!avant.HasValue && !zoneId.HasValue && string.IsNullOrWhiteSpace(baliseIdentifiant))
            throw new ArgumentException("Au moins un critere de suppression est requis.");

        var requete = _contexte.Alertes.AsQueryable();
        if (avant.HasValue) requete = requete.Where(a => a.Horodatage < avant.Value);
        if (zoneId.HasValue) requete = requete.Where(a => a.ZoneId == zoneId.Value);
        if (!string.IsNullOrWhiteSpace(baliseIdentifiant))
            requete = requete.Where(a => a.BaliseIdentifiant == baliseIdentifiant);

        return await requete.ExecuteDeleteAsync(jeton);
    }
}
