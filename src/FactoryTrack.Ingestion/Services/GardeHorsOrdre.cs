using System.Collections.Concurrent;

namespace FactoryTrack.Ingestion.Services;

/// <summary>
/// Une mesure plus ancienne que la derniere traitee pour la meme balise est ignoree :
/// l'appliquer ferait reculer l'equipement sur le plan.
/// </summary>
public class GardeHorsOrdre
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _dernierHorodatage = new();

    public bool Accepter(string baliseId, DateTimeOffset horodatage)
    {
        var accepte = true;

        _dernierHorodatage.AddOrUpdate(
            baliseId,
            horodatage,
            (_, precedent) =>
            {
                if (horodatage <= precedent)
                {
                    accepte = false;
                    return precedent;
                }

                return horodatage;
            });

        return accepte;
    }
}
