using System.Collections;
using System.Collections.Specialized;
using FactoryTrack.Contracts.Dtos;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace FactoryTrack.Mobile.Controls;

/// <summary>
/// Trace du chemin d'une balise. Fond de plan discret + polyline degradee
/// (gris pour les positions anciennes, bleu vif pour la plus recente).
/// </summary>
public class VueTraceEquipement : SKCanvasView
{
    public static readonly BindableProperty LargeurUsineProperty =
        BindableProperty.Create(nameof(LargeurUsine), typeof(double), typeof(VueTraceEquipement), 60.0,
            propertyChanged: (b, _, _) => ((VueTraceEquipement)b).InvalidateSurface());

    public static readonly BindableProperty HauteurUsineProperty =
        BindableProperty.Create(nameof(HauteurUsine), typeof(double), typeof(VueTraceEquipement), 40.0,
            propertyChanged: (b, _, _) => ((VueTraceEquipement)b).InvalidateSurface());

    public static readonly BindableProperty PasserellesProperty =
        BindableProperty.Create(nameof(Passerelles), typeof(IEnumerable), typeof(VueTraceEquipement), null,
            propertyChanged: (b, o, n) => ((VueTraceEquipement)b).ObserverCollection(o, n));

    public static readonly BindableProperty PositionsProperty =
        BindableProperty.Create(nameof(Positions), typeof(IEnumerable), typeof(VueTraceEquipement), null,
            propertyChanged: (b, o, n) => ((VueTraceEquipement)b).ObserverCollection(o, n));

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

    public IEnumerable? Positions
    {
        get => (IEnumerable?)GetValue(PositionsProperty);
        set => SetValue(PositionsProperty, value);
    }

    public VueTraceEquipement()
    {
        PaintSurface += Dessiner;
    }

    private void ObserverCollection(object? ancienne, object? nouvelle)
    {
        if (ancienne is INotifyCollectionChanged a) a.CollectionChanged -= AuChangement;
        if (nouvelle is INotifyCollectionChanged n) n.CollectionChanged += AuChangement;
        InvalidateSurface();
    }

    private void AuChangement(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateSurface();

    private void Dessiner(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        var largeurUsine = Math.Max(LargeurUsine, 1);
        var hauteurUsine = Math.Max(HauteurUsine, 1);
        var info = e.Info;

        var echelle = (float)Math.Min(info.Width / largeurUsine, info.Height / hauteurUsine);
        var largeurTracee = (float)(largeurUsine * echelle);
        var hauteurTracee = (float)(hauteurUsine * echelle);
        var offsetX = (info.Width - largeurTracee) / 2f;
        var offsetY = (info.Height - hauteurTracee) / 2f;

        SKPoint Vers(double x, double y) => new(
            offsetX + (float)(x * echelle),
            offsetY + hauteurTracee - (float)(y * echelle));

        // Fond discret pour situer la trace dans l'usine.
        using var fond = new SKPaint { Color = new SKColor(0xF8, 0xF9, 0xFB), Style = SKPaintStyle.Fill };
        canvas.DrawRect(offsetX, offsetY, largeurTracee, hauteurTracee, fond);

        using var cadre = new SKPaint
        {
            Color = new SKColor(0xC5, 0xCC, 0xD6),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        canvas.DrawRect(offsetX, offsetY, largeurTracee, hauteurTracee, cadre);

        DessinerPasserelles(canvas, Vers);
        DessinerTrace(canvas, Vers);
    }

    private void DessinerPasserelles(SKCanvas canvas, Func<double, double, SKPoint> vers)
    {
        if (Passerelles is null) return;

        using var croix = new SKPaint
        {
            Color = new SKColor(0xC5, 0xCC, 0xD6),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };

        foreach (var obj in Passerelles)
        {
            if (obj is not PasserelleDto p) continue;

            var pt = vers(p.X, p.Y);
            const float T = 4;
            canvas.DrawLine(pt.X - T, pt.Y - T, pt.X + T, pt.Y + T, croix);
            canvas.DrawLine(pt.X - T, pt.Y + T, pt.X + T, pt.Y - T, croix);
        }
    }

    private void DessinerTrace(SKCanvas canvas, Func<double, double, SKPoint> vers)
    {
        if (Positions is null) return;

        var points = new List<SKPoint>();
        foreach (var obj in Positions)
        {
            if (obj is PositionDto p)
                points.Add(vers(p.X, p.Y));
        }

        if (points.Count == 0) return;

        // Segments avec opacite croissante : le present est plus vif que le passe.
        using var segment = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        for (var i = 1; i < points.Count; i++)
        {
            var progression = (float)i / points.Count;
            var alpha = (byte)(80 + (int)(175 * progression));
            segment.Color = new SKColor(0x21, 0x77, 0xBB, alpha);
            canvas.DrawLine(points[i - 1], points[i], segment);
        }

        // Point de depart : cercle vide.
        using var depart = new SKPaint
        {
            Color = new SKColor(0x7F, 0x8C, 0x8D),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawCircle(points[0], 4, depart);

        // Position actuelle : point plein.
        using var actuel = new SKPaint
        {
            Color = new SKColor(0x21, 0x77, 0xBB),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var contour = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawCircle(points[^1], 6, actuel);
        canvas.DrawCircle(points[^1], 6, contour);
    }
}
