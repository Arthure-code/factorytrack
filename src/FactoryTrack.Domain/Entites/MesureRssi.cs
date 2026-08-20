using FactoryTrack.Domain.Enums;

namespace FactoryTrack.Domain.Entites;

public sealed record MesureRssi(
    string BaliseId,
    string PasserelleId,
    int Rssi,
    TypeTechnologie Technologie,
    DateTimeOffset Horodatage)
{
    public string CleIdempotence => $"{BaliseId}|{PasserelleId}|{Horodatage.ToUnixTimeMilliseconds()}";
}
