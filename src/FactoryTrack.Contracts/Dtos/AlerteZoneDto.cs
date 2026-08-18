namespace FactoryTrack.Contracts.Dtos;

/// <summary>
/// Notification d'une transition zone / equipement. Emise uniquement au
/// changement d'etat : la meme alerte a chaque tour serait du bruit.
///
/// Semantique selon le type de zone :
///   - ZoneInterdite : AlerteZoneEntree = est entre dans la zone
///   - ZonePerimetre : AlerteZoneEntree = est SORTI du perimetre
///     (dans les deux cas, "AlerteZoneEntree" signifie "vient d'entrer en alerte")
/// </summary>
public sealed record AlerteZoneDto(
    string BaliseIdentifiant,
    Guid ZoneId,
    string ZoneNom,
    bool ZoneInterdite,
    bool ZonePerimetre,
    int Etage,
    DateTimeOffset Horodatage);
