using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FactoryTrack.Infrastructure.Depots;

public class DepotReferentiel : IDepotReferentiel
{
    private readonly AppDbContext _contexte;

    public DepotReferentiel(AppDbContext contexte) => _contexte = contexte;

    public async Task<IReadOnlyList<Passerelle>> ObtenirPasserellesAsync(CancellationToken jeton = default) =>
        await _contexte.Passerelles.AsNoTracking().ToListAsync(jeton);

    public async Task<IReadOnlyList<Balise>> ObtenirBalisesAsync(CancellationToken jeton = default) =>
        await _contexte.Balises.AsNoTracking().ToListAsync(jeton);

    public async Task<IReadOnlyList<Equipement>> ObtenirEquipementsAsync(CancellationToken jeton = default) =>
        await _contexte.Equipements.AsNoTracking().Include(e => e.Balise).ToListAsync(jeton);

    public async Task<IReadOnlyList<Zone>> ObtenirZonesAsync(CancellationToken jeton = default) =>
        await _contexte.Zones.AsNoTracking().ToListAsync(jeton);
}
