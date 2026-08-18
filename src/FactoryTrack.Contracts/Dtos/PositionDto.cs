namespace FactoryTrack.Contracts.Dtos;

/// <summary>Position diffusee aux clients. Partage par MAUI et Blazor.</summary>
public sealed record PositionDto(
    string BaliseIdentifiant,
    double X,
    double Y,
    int Etage,
    double Precision,
    string Technologie,
    int NombreAncres,
    DateTimeOffset Horodatage);
