using System.Net.Http.Json;
using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

public class ClientReferentiel : IClientReferentiel
{
    private readonly HttpClient _http;
    private readonly OptionsApi _options;

    public ClientReferentiel(HttpClient http, OptionsApi options)
    {
        _http = http;
        _options = options;
    }

    public async Task<IReadOnlyList<EquipementDto>> ObtenirEquipementsAsync(CancellationToken jeton = default) =>
        await ObtenirAsync<EquipementDto>("/api/equipements", jeton);

    public async Task<IReadOnlyList<PasserelleDto>> ObtenirPasserellesAsync(CancellationToken jeton = default) =>
        await ObtenirAsync<PasserelleDto>("/api/referentiel/passerelles", jeton);

    public async Task<IReadOnlyList<ZoneDto>> ObtenirZonesAsync(CancellationToken jeton = default) =>
        await ObtenirAsync<ZoneDto>("/api/referentiel/zones", jeton);

    public async Task<IReadOnlyList<PositionDto>> ObtenirHistoriqueAsync(
        Guid baliseId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken jeton = default)
    {
        // Format ISO 8601 : PostgreSQL et ASP.NET Core l'acceptent tous les deux.
        var chemin = $"/api/positions/historique/{baliseId}" +
                     $"?debut={Uri.EscapeDataString(debut.ToString("o"))}" +
                     $"&fin={Uri.EscapeDataString(fin.ToString("o"))}";
        return await ObtenirAsync<PositionDto>(chemin, jeton);
    }

    private async Task<IReadOnlyList<T>> ObtenirAsync<T>(string chemin, CancellationToken jeton)
    {
        var url = _options.UrlBase.TrimEnd('/') + chemin;
        var reponse = await _http.GetFromJsonAsync<List<T>>(url, jeton);
        return reponse ?? [];
    }
}
