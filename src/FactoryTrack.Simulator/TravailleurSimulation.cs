using FactoryTrack.Contracts.Grpc;
using FactoryTrack.Domain.Entites;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace FactoryTrack.Simulator;

public class TravailleurSimulation : BackgroundService
{
    private const double PUISSANCE_REFERENCE = -59;

    private readonly OptionsSimulateur _options;
    private readonly ILogger<TravailleurSimulation> _journal;
    private readonly Random _alea = new(42);

    public TravailleurSimulation(IOptions<OptionsSimulateur> options, ILogger<TravailleurSimulation> journal)
    {
        _options = options.Value;
        _journal = journal;
    }

    protected override async Task ExecuteAsync(CancellationToken jeton)
    {
        var passerelles = ConstruirePasserelles();
        var equipements = ConstruireEquipements();
        var generateur = new GenerateurMesures(_alea);

        _journal.LogInformation(
            "Simulation : {Equipements} equipements, {Passerelles} passerelles, periode {Periode} ms.",
            equipements.Count, passerelles.Count, _options.PeriodeMs);

        while (!jeton.IsCancellationRequested)
        {
            try
            {
                await DiffuserAsync(equipements, passerelles, generateur, jeton);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _journal.LogError(ex, "Flux interrompu. Nouvelle tentative dans 5 s.");
                await Task.Delay(TimeSpan.FromSeconds(5), jeton);
            }
        }
    }

    private async Task DiffuserAsync(
        List<ModeleEquipementSimule> equipements,
        List<Passerelle> passerelles,
        GenerateurMesures generateur,
        CancellationToken jeton)
    {
        using var canal = GrpcChannel.ForAddress(_options.UrlIngestion);
        var client = new ServiceIngestion.ServiceIngestionClient(canal);

        using var appel = client.EnvoyerMesures(cancellationToken: jeton);
        var deltaSecondes = _options.PeriodeMs / 1000.0;

        while (!jeton.IsCancellationRequested)
        {
            var horodatage = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);

            foreach (var equipement in equipements)
            {
                equipement.Avancer(_options.VitesseMax, deltaSecondes);

                foreach (var passerelle in passerelles)
                {
                    if (_alea.NextDouble() < _options.TauxPerte)
                        continue;

                    var message = new MesureRssiMessage
                    {
                        BaliseId = equipement.BaliseId,
                        PasserelleId = passerelle.Identifiant,
                        Rssi = generateur.CalculerRssi(
                            equipement.X, equipement.Y, passerelle, PUISSANCE_REFERENCE, _options.BruitRssi),
                        Technologie = TypeTechnologie.Bluetooth,
                        Horodatage = horodatage
                    };

                    await appel.RequestStream.WriteAsync(message, jeton);

                    if (_alea.NextDouble() < _options.TauxDoublon)
                        await appel.RequestStream.WriteAsync(message, jeton);
                }
            }

            await Task.Delay(_options.PeriodeMs, jeton);
        }

        await appel.RequestStream.CompleteAsync();
    }

    private List<Passerelle> ConstruirePasserelles() =>
[
    new() { Identifiant = "GW-01", X = 0, Y = 0, Etage = 0, Active = true },
        new() { Identifiant = "GW-02", X = _options.LargeurUsine, Y = 0, Etage = 0, Active = true },
        new() { Identifiant = "GW-03", X = _options.LargeurUsine, Y = _options.HauteurUsine, Etage = 0, Active = true },
        new() { Identifiant = "GW-04", X = 0, Y = _options.HauteurUsine, Etage = 0, Active = true }
];

    private List<ModeleEquipementSimule> ConstruireEquipements() =>
        Enumerable.Range(1, _options.NombreEquipements)
                  .Select(i => new ModeleEquipementSimule(
                      $"TAG-{i:D3}", _options.LargeurUsine, _options.HauteurUsine, _alea))
                  .ToList();
}
