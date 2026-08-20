using System.Collections.Concurrent;

namespace FactoryTrack.Positioning;

public class FiltrePosition
{
    private readonly ConcurrentDictionary<string, (double X, double Y)> _dernieresPositions = new();
    private readonly double _alpha;
    private readonly double _sautMaximal;

    public FiltrePosition(double alpha, double sautMaximalMetres)
    {
        if (alpha is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha doit etre dans l'intervalle ]0, 1].");

        _alpha = alpha;
        _sautMaximal = sautMaximalMetres;
    }

    public (double X, double Y) Lisser(string baliseId, double x, double y)
    {
        if (!_dernieresPositions.TryGetValue(baliseId, out var precedente))
        {
            _dernieresPositions[baliseId] = (x, y);
            return (x, y);
        }

        var deplacement = Math.Sqrt(Math.Pow(x - precedente.X, 2) + Math.Pow(y - precedente.Y, 2));

        var alpha = deplacement > _sautMaximal ? _alpha / 4 : _alpha;

        var lissee = (
            X: alpha * x + (1 - alpha) * precedente.X,
            Y: alpha * y + (1 - alpha) * precedente.Y);

        _dernieresPositions[baliseId] = lissee;

        return lissee;
    }

    public void Oublier(string baliseId) => _dernieresPositions.TryRemove(baliseId, out _);
}
