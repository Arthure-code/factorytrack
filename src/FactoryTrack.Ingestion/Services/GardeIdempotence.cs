using System.Collections.Concurrent;
using FactoryTrack.Domain.Interfaces;
using FactoryTrack.Domain.Options;
using Microsoft.Extensions.Options;

namespace FactoryTrack.Ingestion.Services;

public class GardeIdempotence : IGardeIdempotence, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cles = new();
    private readonly TimeSpan _retention;
    private readonly Timer _nettoyage;
    private bool _disposed;

    public GardeIdempotence(IOptions<OptionsPositionnement> options)
    {
        _retention = TimeSpan.FromSeconds(options.Value.RetentionIdempotenceSecondes);
        _nettoyage = new Timer(_ => Purger(), null, _retention, _retention);
    }

    public bool Accepter(string cleIdempotence) =>
        _cles.TryAdd(cleIdempotence, DateTimeOffset.UtcNow);

    private void Purger()
    {
        var limite = DateTimeOffset.UtcNow - _retention;

        foreach (var (cle, date) in _cles)
        {
            if (date < limite)
                _cles.TryRemove(cle, out _);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            _nettoyage.Dispose();

        _disposed = true;
    }
}
