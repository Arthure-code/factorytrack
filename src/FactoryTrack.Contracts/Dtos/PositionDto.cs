namespace FactoryTrack.Contracts.Dtos;

public sealed record PositionDto(
    string BaliseIdentifiant,
    double X,
    double Y,
    int Etage,
    double Precision,
    string Technologie,
    int NombreAncres,
    DateTimeOffset Horodatage);
