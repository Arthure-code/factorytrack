namespace FactoryTrack.Contracts.Dtos;

public sealed record AlerteZoneDto(
    string BaliseIdentifiant,
    Guid ZoneId,
    string ZoneNom,
    bool ZoneInterdite,
    bool ZonePerimetre,
    int Etage,
    DateTimeOffset Horodatage);
