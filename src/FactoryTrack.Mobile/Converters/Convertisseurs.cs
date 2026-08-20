using System.Globalization;

namespace FactoryTrack.Mobile.Converters;

public class ConvertisseurEtatConnexion : IValueConverter
{
    private static readonly Color Vert = Color.FromArgb("#27AE60");
    private static readonly Color Rouge = Color.FromArgb("#C0392B");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Vert : Rouge;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ConvertisseurLibelleConnexion : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "En direct" : "Hors ligne";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ConvertisseurInverseBool : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}

public class ConvertisseurTexteVersVisible : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ConvertisseurCompteVersVisible : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ConvertisseurEtatPrecision : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "☑" : "☐";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class ConvertisseurAlerteCouleur : IValueConverter
{
    private static readonly Color Rouge = Color.FromArgb("#C0392B");
    private static readonly Color Gris = Color.FromArgb("#7F8C8D");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Rouge : Gris;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
