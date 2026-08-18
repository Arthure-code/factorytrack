namespace FactoryTrack.Contracts.Dtos;

/// <summary>
/// Notification qu'un equipement vient d'entrer ou de sortir d'une zone. Emise
/// uniquement a la transition : recevoir la meme alerte a chaque tour serait du bruit.
/// </summary>
public sealed record AlerteZoneDto(
    string BaliseIdentifiant,
    Guid ZoneId,
    string ZoneNom,
    bool ZoneInterdite,
    int Etage,
    DateTimeOffset Horodatage);
