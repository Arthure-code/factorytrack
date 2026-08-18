using System.Net.Http.Json;
using System.Text;
using FactoryTrack.Contracts.Dtos;

namespace FactoryTrack.Mobile.Services;

public class ClientAlertes : IClientAlertes
{
    private readonly HttpClient _http;
    private readonly OptionsApi _options;

    public ClientAlertes(HttpClient http, OptionsApi options)
    {
        _http = http;
        _options = options;
    }

    public async Task<IReadOnlyList<AlerteHistoriqueDto>> ObtenirAsync(
        DateTimeOffset? debut = null,
        DateTimeOffset? fin = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        int limite = 200,
        CancellationToken jeton = default)
    {
        var url = _options.UrlBase.TrimEnd('/') + "/api/alertes" + Query(new[]
        {
            (nameof(debut), debut?.ToString("o")),
            (nameof(fin), fin?.ToString("o")),
            (nameof(zoneId), zoneId?.ToString()),
            ("baliseId", baliseIdentifiant),
            (nameof(limite), limite.ToString())
        });

        var reponse = await _http.GetFromJsonAsync<List<AlerteHistoriqueDto>>(url, jeton);
        return reponse ?? [];
    }

    public async Task SupprimerAsync(Guid id, CancellationToken jeton = default)
    {
        var url = _options.UrlBase.TrimEnd('/') + $"/api/alertes/{id}";
        var reponse = await _http.DeleteAsync(url, jeton);
        reponse.EnsureSuccessStatusCode();
    }

    public async Task SupprimerLotAsync(
        DateTimeOffset? avant = null,
        Guid? zoneId = null,
        string? baliseIdentifiant = null,
        CancellationToken jeton = default)
    {
        var url = _options.UrlBase.TrimEnd('/') + "/api/alertes" + Query(new[]
        {
            (nameof(avant), avant?.ToString("o")),
            (nameof(zoneId), zoneId?.ToString()),
            ("baliseId", baliseIdentifiant)
        });

        var reponse = await _http.DeleteAsync(url, jeton);
        reponse.EnsureSuccessStatusCode();
    }

    private static string Query(IEnumerable<(string cle, string? valeur)> parametres)
    {
        var b = new StringBuilder();
        var premier = true;
        foreach (var (cle, valeur) in parametres)
        {
            if (string.IsNullOrWhiteSpace(valeur)) continue;
            b.Append(premier ? '?' : '&');
            b.Append(cle).Append('=').Append(Uri.EscapeDataString(valeur));
            premier = false;
        }
        return b.ToString();
    }
}
