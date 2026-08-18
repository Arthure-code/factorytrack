namespace FactoryTrack.Contracts.Dtos;

public sealed record AlerteHistoriqueDto(
    Guid Id,
    string BaliseIdentifiant,
    string CodeEquipement,
    Guid ZoneId,
    string ZoneNom,
    bool ZoneInterdite,
    bool ZonePerimetre,
    bool EstEntree,
    int Etage,
    DateTimeOffset Horodatage);
