using FactoryTrack.Domain.Interfaces;

namespace FactoryTrack.Ingestion.Services;

public sealed class GardesIngestion
{
    public IGardeIdempotence Idempotence { get; }
    public GardeHorsOrdre HorsOrdre { get; }

    public GardesIngestion(IGardeIdempotence idempotence, GardeHorsOrdre horsOrdre)
    {
        Idempotence = idempotence;
        HorsOrdre = horsOrdre;
    }
}
