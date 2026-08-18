using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Enums;
using FactoryTrack.Domain.Options;

namespace FactoryTrack.Positioning;

/// <summary>
/// Chaine complete : mesures RSSI d'une meme balise vers une position filtree.
/// Aucune dependance a l'infrastructure : entierement testable unitairement.
/// </summary>
public class ServicePositionnement
{
    private readonly OptionsPositionnement _options;
    private readonly FiltrePosition _filtre;

    public ServicePositionnement(OptionsPositionnement options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _filtre = new FiltrePosition(options.AlphaLissage, options.SautMaximalMetres);
    }

    /// <param name="mesures">Mesures d'une meme balise, provenant de passerelles distinctes.</param>
    /// <param name="passerelles">Referentiel des passerelles, indexe par identifiant.</param>
    /// <param name="balise">Balise concernee.</param>
    public ResultatPositionnement Calculer(
        IReadOnlyList<MesureRssi> mesures,
        IReadOnlyDictionary<string, Passerelle> passerelles,
        Balise balise)
    {
        ArgumentNullException.ThrowIfNull(mesures);
        ArgumentNullException.ThrowIfNull(passerelles);
        ArgumentNullException.ThrowIfNull(balise);

        var ancres = new List<Ancre>();

        foreach (var mesure in mesures)
        {
            if (!passerelles.TryGetValue(mesure.PasserelleId, out var passerelle) || !passerelle.Active)
                continue;

            var distance = CalculateurDistance.Convertir(
                mesure.Rssi, balise.PuissanceReference, _options.ExposantPropagation);

            ancres.Add(new Ancre(passerelle.X, passerelle.Y, passerelle.Etage, distance));
        }

        var resultat = Trilateration.Resoudre(ancres, _options.AncresMinimales);

        if (!resultat.Reussi)
            return ResultatPositionnement.Echec(resultat.Motif ?? "Trilateration impossible.");

        var (x, y) = _filtre.Lisser(balise.Identifiant, resultat.X, resultat.Y);

        var position = new Position
        {
            BaliseId = balise.Id,
            BaliseIdentifiant = balise.Identifiant,
            X = Math.Round(x, 3),
            Y = Math.Round(y, 3),
            Etage = ancres[0].Etage,
            Precision = EstimerPrecision(resultat.ResiduMoyen, balise.Technologie),
            Technologie = balise.Technologie,
            NombreAncres = ancres.Count,
            Horodatage = mesures.Max(m => m.Horodatage)
        };

        return ResultatPositionnement.Succes(position);
    }

    /// <summary>
    /// L'UWB mesure un temps de vol, le Bluetooth une puissance : les incertitudes
    /// ne sont pas du meme ordre. Le plancher traduit cette difference physique.
    /// </summary>
    private static double EstimerPrecision(double residuMoyen, TypeTechnologie technologie)
    {
        var plancher = technologie == TypeTechnologie.Uwb ? 0.3 : 2.0;
        return Math.Round(Math.Max(plancher, residuMoyen), 2);
    }
}

public sealed record ResultatPositionnement(bool Reussi, Position? Position, string? Motif)
{
    public static ResultatPositionnement Succes(Position position) => new(true, position, null);
    public static ResultatPositionnement Echec(string motif) => new(false, null, motif);
}
