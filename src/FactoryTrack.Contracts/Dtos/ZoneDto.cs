namespace FactoryTrack.Contracts.Dtos;

public sealed record ZoneDto(
    Guid Id,
    string Nom,
    int Etage,
    double XMin,
    double YMin,
    double XMax,
    double YMax,
    bool Interdite,
    bool Perimetre);
