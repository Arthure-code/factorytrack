using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using FactoryTrack.Contracts.Dtos;
using FactoryTrack.Mobile.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace FactoryTrack.Mobile.Controls;

/// <summary>
/// Rendu du plan de l'usine en coordonnees locales (metres). L'axe Y est
/// inverse a l'affichage : la convention industrielle place l'origine en
/// bas a gauche, celle de SKCanvas est en haut a gauche.
/// </summary>
public class VuePlanUsine : SKCanvasView
{
    public static readonly BindableProperty LargeurUsineProperty =
        BindableProperty.Create(nameof(LargeurUsine), typeof(double), typeof(VuePlanUsine), 60.0,
            propertyChanged: (b, _, _) => ((VuePlanUsine)b).InvalidateSurface());

    public static readonly BindableProperty HauteurUsineProperty =
        BindableProperty.Create(nameof(HauteurUsine), typeof(double), typeof(VuePlanUsine), 40.0,
            propertyChanged: (b, _, _) => ((VuePlanUsine)b).InvalidateSurface());

    public static readonly BindableProperty PasserellesProperty =
        BindableProperty.Create(nameof(Passerelles), typeof(IEnumerable), typeof(VuePlanUsine), null,
            propertyChanged: (b, o, n) => ((VuePlanUsine)b).ObserverCollection(o, n));

    public static readonly BindableProperty ZonesProperty =
        BindableProperty.Create(nameof(Zones), typeof(IEnumerable), typeof(VuePlanUsine), null,
            propertyChanged: (b, o, n) => ((VuePlanUsine)b).ObserverCollection(o, n));

    public static readonly BindableProperty EquipementsProperty =
        BindableProperty.Create(nameof(Equipements), typeof(IEnumerable), typeof(VuePlanUsine), null,
            propertyChanged: (b, o, n) => ((VuePlanUsine)b).ObserverEquipements(o, n));

    public static readonly BindableProperty AfficherPrecisionProperty =
        BindableProperty.Create(nameof(AfficherPrecision), typeof(bool), typeof(VuePlanUsine), false,
            propertyChanged: (b, _, _) => ((VuePlanUsine)b).InvalidateSurface());

    public static readonly BindableProperty EquipementTapeCommandProperty =
        BindableProperty.Create(nameof(EquipementTapeCommand), typeof(ICommand), typeof(VuePlanUsine), null);

    public double LargeurUsine
    {
        get => (double)GetValue(LargeurUsineProperty);
        set => SetValue(LargeurUsineProperty, value);
    }

    public double HauteurUsine
    {
        get => (double)GetValue(HauteurUsineProperty);
        set => SetValue(HauteurUsineProperty, value);
    }

    public IEnumerable? Passerelles
    {
        get => (IEnumerable?)GetValue(PasserellesProperty);
        set => SetValue(PasserellesProperty, value);
    }

    public IEnumerable? Zones
    {
        get => (IEnumerable?)GetValue(ZonesProperty);
        set => SetValue(ZonesProperty, value);
    }

    public IEnumerable? Equipements
    {
        get => (IEnumerable?)GetValue(EquipementsProperty);
        set => SetValue(EquipementsProperty, value);
    }

    public bool AfficherPrecision
    {
        get => (bool)GetValue(AfficherPrecisionProperty);
        set => SetValue(AfficherPrecisionProperty, value);
    }

    public ICommand? EquipementTapeCommand
    {
        get => (ICommand?)GetValue(EquipementTapeCommandProperty);
        set => SetValue(EquipementTapeCommandProperty, value);
    }

    private readonly HashSet<INotifyPropertyChanged> _abonnesItem = new();

    // Cache des positions ecran des equipements pour le hit-test au tap.
    // Rempli a chaque rendu ; le premier tap avant un rendu ne fait rien.
    private readonly List<(EquipementApercu Apercu, SKPoint Position)> _dernieresPositionsEcran = new();

    public VuePlanUsine()
    {
        PaintSurface += Dessiner;
        EnableTouchEvents = true;
        Touch += AuTouche;
    }

    private void AuTouche(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Pressed)
            return;

        const float SEUIL_PIXELS = 20;
        var commande = EquipementTapeCommand;
        if (commande is null) return;

        EquipementApercu? cible = null;
        float meilleureDistance = SEUIL_PIXELS;

        foreach (var (apercu, position) in _dernieresPositionsEcran)
        {
            var dx = e.Location.X - position.X;
            var dy = e.Location.Y - position.Y;
            var distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance < meilleureDistance)
            {
                meilleureDistance = distance;
                cible = apercu;
            }
        }

        if (cible is not null && commande.CanExecute(cible))
        {
            commande.Execute(cible);
            e.Handled = true;
        }
    }

    private void ObserverCollection(object? ancienne, object? nouvelle)
    {
        if (ancienne is INotifyCollectionChanged ancienNotif)
            ancienNotif.CollectionChanged -= AuChangementCollection;

        if (nouvelle is INotifyCollectionChanged nouveauNotif)
            nouveauNotif.CollectionChanged += AuChangementCollection;

        InvalidateSurface();
    }

    private void ObserverEquipements(object? ancienne, object? nouvelle)
    {
        // Desabonnement des anciens items pour eviter la fuite.
        foreach (var item in _abonnesItem)
            item.PropertyChanged -= AuChangementItem;

        _abonnesItem.Clear();

        if (ancienne is INotifyCollectionChanged ancienNotif)
            ancienNotif.CollectionChanged -= AuChangementCollectionEquipements;

        if (nouvelle is INotifyCollectionChanged nouveauNotif)
            nouveauNotif.CollectionChanged += AuChangementCollectionEquipements;

        if (nouvelle is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                AbonnerItem(item);
        }

        InvalidateSurface();
    }

    private void AuChangementCollectionEquipements(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems)
                DesabonnerItem(item);
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
                AbonnerItem(item);
        }

        InvalidateSurface();
    }

    private void AbonnerItem(object item)
    {
        if (item is INotifyPropertyChanged notif && _abonnesItem.Add(notif))
            notif.PropertyChanged += AuChangementItem;
    }

    private void DesabonnerItem(object item)
    {
        if (item is INotifyPropertyChanged notif && _abonnesItem.Remove(notif))
            notif.PropertyChanged -= AuChangementItem;
    }

    private void AuChangementItem(object? sender, PropertyChangedEventArgs e) => InvalidateSurface();

    private void AuChangementCollection(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateSurface();

    private void Dessiner(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        var tailleCanvas = e.Info;
        var largeurUsine = Math.Max(LargeurUsine, 1);
        var hauteurUsine = Math.Max(HauteurUsine, 1);

        var echelle = (float)Math.Min(
            tailleCanvas.Width / largeurUsine,
            tailleCanvas.Height / hauteurUsine);

        var largeurTracee = (float)(largeurUsine * echelle);
        var hauteurTracee = (float)(hauteurUsine * echelle);
        var offsetX = (tailleCanvas.Width - largeurTracee) / 2f;
        var offsetY = (tailleCanvas.Height - hauteurTracee) / 2f;

        SKPoint VersEcran(double x, double y) => new(
            offsetX + (float)(x * echelle),
            offsetY + hauteurTracee - (float)(y * echelle));

        DessinerFond(canvas, offsetX, offsetY, largeurTracee, hauteurTracee);
        DessinerGrille(canvas, largeurUsine, hauteurUsine, VersEcran);
        DessinerZones(canvas, echelle, VersEcran);
        DessinerPasserelles(canvas, VersEcran);
        DessinerEquipements(canvas, echelle, VersEcran);
    }

    private static void DessinerFond(SKCanvas canvas, float x, float y, float largeur, float hauteur)
    {
        using var fond = new SKPaint { Color = new SKColor(0xF6, 0xF7, 0xF9), Style = SKPaintStyle.Fill };
        canvas.DrawRect(x, y, largeur, hauteur, fond);

        using var cadre = new SKPaint
        {
            Color = new SKColor(0x30, 0x3A, 0x4A),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawRect(x, y, largeur, hauteur, cadre);
    }

    private static void DessinerGrille(SKCanvas canvas, double largeur, double hauteur, Func<double, double, SKPoint> versEcran)
    {
        using var trait = new SKPaint
        {
            Color = new SKColor(0xE0, 0xE4, 0xEA),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        // Grille tous les 5 metres.
        for (double x = 0; x <= largeur; x += 5)
        {
            var a = versEcran(x, 0);
            var b = versEcran(x, hauteur);
            canvas.DrawLine(a, b, trait);
        }

        for (double y = 0; y <= hauteur; y += 5)
        {
            var a = versEcran(0, y);
            var b = versEcran(largeur, y);
            canvas.DrawLine(a, b, trait);
        }
    }

    private void DessinerZones(SKCanvas canvas, float echelle, Func<double, double, SKPoint> versEcran)
    {
        if (Zones is null) return;

        foreach (var obj in Zones)
        {
            if (obj is not ZoneDto zone) continue;

            var coinHaut = versEcran(zone.XMin, zone.YMax);
            var coinBas = versEcran(zone.XMax, zone.YMin);
            var rect = new SKRect(coinHaut.X, coinHaut.Y, coinBas.X, coinBas.Y);

            var couleurFond = zone.Interdite
                ? new SKColor(0xE7, 0x4C, 0x3C, 0x30)
                : new SKColor(0x2E, 0xCC, 0x71, 0x25);

            var couleurBord = zone.Interdite
                ? new SKColor(0xC0, 0x39, 0x2B)
                : new SKColor(0x27, 0xAE, 0x60);

            using var fond = new SKPaint { Color = couleurFond, Style = SKPaintStyle.Fill };
            using var bord = new SKPaint
            {
                Color = couleurBord,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };

            canvas.DrawRect(rect, fond);
            canvas.DrawRect(rect, bord);

            using var texte = new SKPaint { Color = couleurBord, IsAntialias = true };
            using var police = new SKFont { Size = 12 };
            canvas.DrawText(zone.Nom, rect.Left + 4, rect.Top + 14, SKTextAlign.Left, police, texte);
        }
    }

    private void DessinerPasserelles(SKCanvas canvas, Func<double, double, SKPoint> versEcran)
    {
        if (Passerelles is null) return;

        using var croix = new SKPaint
        {
            Color = new SKColor(0x2C, 0x3E, 0x50),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };

        using var texte = new SKPaint { Color = new SKColor(0x2C, 0x3E, 0x50), IsAntialias = true };
        using var police = new SKFont { Size = 10 };

        foreach (var obj in Passerelles)
        {
            if (obj is not PasserelleDto p) continue;

            var pt = versEcran(p.X, p.Y);
            const float TAILLE = 6;

            canvas.DrawLine(pt.X - TAILLE, pt.Y - TAILLE, pt.X + TAILLE, pt.Y + TAILLE, croix);
            canvas.DrawLine(pt.X - TAILLE, pt.Y + TAILLE, pt.X + TAILLE, pt.Y - TAILLE, croix);

            canvas.DrawText(p.Identifiant, pt.X + 8, pt.Y - 6, SKTextAlign.Left, police, texte);
        }
    }

    private void DessinerEquipements(SKCanvas canvas, float echelle, Func<double, double, SKPoint> versEcran)
    {
        _dernieresPositionsEcran.Clear();

        if (Equipements is null) return;

        foreach (var obj in Equipements)
        {
            if (obj is not EquipementApercu eq || !eq.AUnePosition) continue;

            var pt = versEcran(eq.X, eq.Y);
            _dernieresPositionsEcran.Add((eq, pt));

            // Priorite : alerte zone interdite > silencieux > actif.
            SKColor couleurBord;
            SKColor couleurTexte;

            if (eq.DansZoneInterdite)
            {
                couleurBord = new SKColor(0xC0, 0x39, 0x2B);
                couleurTexte = new SKColor(0x94, 0x31, 0x26);
            }
            else if (eq.Silencieux)
            {
                couleurBord = new SKColor(0x7F, 0x8C, 0x8D);
                couleurTexte = new SKColor(0x55, 0x62, 0x63);
            }
            else
            {
                couleurBord = new SKColor(0x21, 0x77, 0xBB);
                couleurTexte = new SKColor(0x1A, 0x5D, 0x92);
            }

            if (AfficherPrecision)
            {
                // Contour discret, pointille : lu comme "zone d'incertitude" sans masquer les voisins.
                var rayonPrecision = Math.Max(3, (float)(eq.Precision * echelle));
                using var halo = new SKPaint
                {
                    Color = couleurBord.WithAlpha(0x60),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 3 }, 0)
                };
                canvas.DrawCircle(pt, rayonPrecision, halo);
            }

            using var point = new SKPaint { Color = couleurBord, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var contourPoint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };
            using var etiquette = new SKPaint { Color = couleurTexte, IsAntialias = true };
            using var policeEtiquette = new SKFont { Size = 11, Embolden = true };

            canvas.DrawCircle(pt, 5, point);
            canvas.DrawCircle(pt, 5, contourPoint);
            canvas.DrawText(eq.Code, pt.X + 7, pt.Y - 7, SKTextAlign.Left, policeEtiquette, etiquette);
        }
    }
}
