namespace FactoryTrack.Contracts.Dtos;

public sealed record EquipementDto(
    Guid Id,
    string Code,
    string Nom,
    string? Categorie,
    Guid? BaliseId,
    string? BaliseIdentifiant,
    string Etat,
    PositionDto? DernierePosition,
    bool Silencieux);
