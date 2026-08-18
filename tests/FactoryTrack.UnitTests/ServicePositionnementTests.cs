using FactoryTrack.Domain.Entites;
using FactoryTrack.Domain.Enums;
using FactoryTrack.Domain.Options;
using FactoryTrack.Positioning;
using FluentAssertions;
using Xunit;

namespace FactoryTrack.UnitTests;

public class ServicePositionnementTests
{
    private static readonly Dictionary<string, Passerelle> Passerelles = new()
    {
        ["GW-01"] = new Passerelle { Identifiant = "GW-01", X = 0, Y = 0, Etage = 0, Active = true },
        ["GW-02"] = new Passerelle { Identifiant = "GW-02", X = 60, Y = 0, Etage = 0, Active = true },
        ["GW-03"] = new Passerelle { Identifiant = "GW-03", X = 60, Y = 40, Etage = 0, Active = true },
        ["GW-04"] = new Passerelle { Identifiant = "GW-04", X = 0, Y = 40, Etage = 0, Active = true }
    };

    [Fact]
    public void Calculer_QuatreAncres_ProduitUnePosition()
    {
        var service = new ServicePositionnement(new OptionsPositionnement());
        var balise = CreerBalise(TypeTechnologie.Bluetooth);
        var mesures = CreerMesuresPour(x: 30, y: 20, balise);

        var resultat = service.Calculer(mesures, Passerelles, balise);

        resultat.Reussi.Should().BeTrue();
        resultat.Position.Should().NotBeNull();
        resultat.Position!.NombreAncres.Should().Be(4);
        resultat.Position.X.Should().BeApproximately(30, 8);
        resultat.Position.Y.Should().BeApproximately(20, 8);
    }

    [Fact]
    public void Calculer_UneSeuleMesure_Echoue()
    {
        var service = new ServicePositionnement(new OptionsPositionnement());
        var balise = CreerBalise(TypeTechnologie.Bluetooth);

        var mesures = new List<MesureRssi>
        {
            new("TAG-001", "GW-01", -70, TypeTechnologie.Bluetooth, DateTimeOffset.UtcNow)
        };

        var resultat = service.Calculer(mesures, Passerelles, balise);

        resultat.Reussi.Should().BeFalse();
        resultat.Position.Should().BeNull();
    }

    [Fact]
    public void Calculer_PasserelleInconnue_EstIgnoree()
    {
        var service = new ServicePositionnement(new OptionsPositionnement());
        var balise = CreerBalise(TypeTechnologie.Bluetooth);

        var mesures = CreerMesuresPour(30, 20, balise);
        mesures.Add(new MesureRssi("TAG-001", "GW-FANTOME", -65, TypeTechnologie.Bluetooth, DateTimeOffset.UtcNow));

        var resultat = service.Calculer(mesures, Passerelles, balise);

        resultat.Reussi.Should().BeTrue();
        resultat.Position!.NombreAncres.Should().Be(4, "la passerelle inconnue ne compte pas comme une ancre");
    }

    [Fact]
    public void Calculer_Uwb_AnnonceUneMeilleurePrecisionQueBluetooth()
    {
        var service = new ServicePositionnement(new OptionsPositionnement());

        var baliseBluetooth = CreerBalise(TypeTechnologie.Bluetooth, "TAG-BT");
        var baliseUwb = CreerBalise(TypeTechnologie.Uwb, "TAG-UWB");

        var precisionBluetooth = service
            .Calculer(CreerMesuresPour(30, 20, baliseBluetooth), Passerelles, baliseBluetooth)
            .Position!.Precision;

        var precisionUwb = service
            .Calculer(CreerMesuresPour(30, 20, baliseUwb), Passerelles, baliseUwb)
            .Position!.Precision;

        precisionUwb.Should().BeLessThanOrEqualTo(precisionBluetooth);
    }

    [Fact]
    public void Calculer_HorodatageRetourne_EstLePlusRecentDuGroupe()
    {
        var service = new ServicePositionnement(new OptionsPositionnement());
        var balise = CreerBalise(TypeTechnologie.Bluetooth);
        var reference = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        var mesures = CreerMesuresPour(30, 20, balise);

        for (var i = 0; i < mesures.Count; i++)
            mesures[i] = mesures[i] with { Horodatage = reference.AddSeconds(i) };

        var resultat = service.Calculer(mesures, Passerelles, balise);

        resultat.Position!.Horodatage.Should().Be(reference.AddSeconds(mesures.Count - 1));
    }

    private static Balise CreerBalise(TypeTechnologie technologie, string identifiant = "TAG-001") => new()
    {
        Id = Guid.NewGuid(),
        Identifiant = identifiant,
        Technologie = technologie,
        PuissanceReference = -59
    };

    /// <summary>Genere les RSSI theoriques qu'auraient mesures les passerelles pour un point donne.</summary>
    private static List<MesureRssi> CreerMesuresPour(double x, double y, Balise balise)
    {
        const double EXPOSANT = 2.8;
        var horodatage = DateTimeOffset.UtcNow;

        return Passerelles.Values.Select(passerelle =>
        {
            var distance = Math.Max(0.1, Math.Sqrt(
                Math.Pow(x - passerelle.X, 2) + Math.Pow(y - passerelle.Y, 2)));

            var rssi = (int)Math.Round(
                balise.PuissanceReference - 10 * EXPOSANT * Math.Log10(distance));

            return new MesureRssi(balise.Identifiant, passerelle.Identifiant, rssi, balise.Technologie, horodatage);
        }).ToList();
    }
}
