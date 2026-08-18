namespace FactoryTrack.Simulator;

/// <summary>Marche aleatoire lissee, bornee au plan de l'usine.</summary>
public class ModeleEquipementSimule
{
    private readonly Random _alea;
    private readonly double _largeur;
    private readonly double _hauteur;

    private double _capX;
    private double _capY;

    public string BaliseId { get; }
    public double X { get; private set; }
    public double Y { get; private set; }

    public ModeleEquipementSimule(string baliseId, double largeur, double hauteur, Random alea)
    {
        BaliseId = baliseId;
        _largeur = largeur;
        _hauteur = hauteur;
        _alea = alea;

        X = alea.NextDouble() * largeur;
        Y = alea.NextDouble() * hauteur;

        var angle = alea.NextDouble() * 2 * Math.PI;
        _capX = Math.Cos(angle);
        _capY = Math.Sin(angle);
    }

    public void Avancer(double vitesseMax, double deltaSecondes)
    {
        // Le cap derive lentement : un chariot ne change pas de direction a chaque pas.
        _capX += (_alea.NextDouble() - 0.5) * 0.3;
        _capY += (_alea.NextDouble() - 0.5) * 0.3;

        var norme = Math.Sqrt(_capX * _capX + _capY * _capY);

        if (norme > 0)
        {
            _capX /= norme;
            _capY /= norme;
        }

        var distance = vitesseMax * deltaSecondes;

        X = Rebondir(X + _capX * distance, _largeur, ref _capX);
        Y = Rebondir(Y + _capY * distance, _hauteur, ref _capY);
    }

    private static double Rebondir(double valeur, double maximum, ref double cap)
    {
        if (valeur < 0)
        {
            cap = -cap;
            return 0;
        }

        if (valeur > maximum)
        {
            cap = -cap;
            return maximum;
        }

        return valeur;
    }
}
