using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Interfaces;

namespace FactoryTrack.Infrastructure.Depots;

/// <summary>
/// Le referentiel change rarement mais est lu a chaque mesure. Sans cache,
/// l'ingestion ferait un aller-retour en base par paquet radio.
/// </summary>
public class CacheReferentiel
{
    private static readonly TimeSpan DUREE_VALIDITE = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _fournisseur;
    private readonly SemaphoreSlim _verrou = new(1, 1);

    private Dictionary<string, Passerelle> _passerelles = new();
    private Dictionary<string, Balise> _balises = new();
    private DateTimeOffset _dernierChargement = DateTimeOffset.MinValue;

    public CacheReferentiel(IServiceProvider fournisseur) => _fournisseur = fournisseur;

    public async Task<(IReadOnlyDictionary<string, Passerelle> Passerelles,
                       IReadOnlyDictionary<string, Balise> Balises)> ObtenirAsync(
        IDepotReferentiel depot, CancellationToken jeton = default)
    {
        if (DateTimeOffset.UtcNow - _dernierChargement < DUREE_VALIDITE)
            return (_passerelles, _balises);

        await _verrou.WaitAsync(jeton);

        try
        {
            if (DateTimeOffset.UtcNow - _dernierChargement < DUREE_VALIDITE)
                return (_passerelles, _balises);

            var passerelles = await depot.ObtenirPasserellesAsync(jeton);
            var balises = await depot.ObtenirBalisesAsync(jeton);

            _passerelles = passerelles.ToDictionary(p => p.Identifiant);
            _balises = balises.ToDictionary(b => b.Identifiant);
            _dernierChargement = DateTimeOffset.UtcNow;

            return (_passerelles, _balises);
        }
        finally
        {
            _verrou.Release();
        }
    }

    public void Invalider() => _dernierChargement = DateTimeOffset.MinValue;
}
