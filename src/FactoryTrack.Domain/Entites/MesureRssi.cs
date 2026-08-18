using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

/// <summary>Mesure brute recue d'une passerelle. Immuable.</summary>
public sealed record MesureRssi(
    string BaliseId,
    string PasserelleId,
    int Rssi,
    TypeTechnologie Technologie,
    DateTimeOffset Horodatage)
{
    /// <summary>Cle d'idempotence : une meme mesure ne doit produire qu'une position.</summary>
    public string CleIdempotence => $"{BaliseId}|{PasserelleId}|{Horodatage.ToUnixTimeMilliseconds()}";
}
