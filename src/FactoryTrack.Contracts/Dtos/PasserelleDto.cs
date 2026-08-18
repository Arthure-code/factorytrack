namespace FactoryTrack.Contracts.Dtos;

public sealed record PasserelleDto(
    string Identifiant,
    double X,
    double Y,
    int Etage,
    bool Active);
