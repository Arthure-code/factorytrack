namespace FactoryTrack.Domain.Interfaces;

public interface IGardeIdempotence
{
    bool Accepter(string cleIdempotence);
}
