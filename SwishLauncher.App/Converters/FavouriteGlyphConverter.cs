using Microsoft.UI.Xaml.Data;
using System;

namespace SwishLauncher.App.Converters;

/// <summary>
/// Returns the Segoe MDL2 glyph for a filled star (favourite) or outline star (not favourite).
/// Bound to the FontIcon inside the Favourite toggle button on detail pages.
/// </summary>
public sealed class FavouriteGlyphConverter : IValueConverter
{
    // Segoe MDL2 Assets
    private const string FilledStar  = "\uE735"; // favourite (filled)
    private const string OutlineStar = "\uE734"; // not favourite (outline)

    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? FilledStar : OutlineStar;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
