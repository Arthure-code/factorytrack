namespace FactoryTrack.Domain.Interfaces;

/// <summary>Rejette les mesures deja traitees.</summary>
public interface IGardeIdempotence
{
    /// <summary>Retourne vrai si la cle est nouvelle, faux si elle a deja ete vue.</summary>
    bool Accepter(string cleIdempotence);
}
