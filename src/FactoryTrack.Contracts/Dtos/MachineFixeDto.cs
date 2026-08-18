namespace FactoryTrack.Contracts.Dtos;

public sealed record MachineFixeDto(
    Guid Id,
    string Code,
    string Nom,
    int Etage,
    double X,
    double Y,
    double Largeur,
    double Hauteur);
