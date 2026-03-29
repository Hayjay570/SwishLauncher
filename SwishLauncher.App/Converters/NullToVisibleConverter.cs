using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SwishLauncher.App.Converters;

/// <summary>
/// Returns Visible when the value is null or empty string (no image path set),
/// Collapsed when an image path is present.
/// Used to show the placeholder FontIcon when CoverArtPath / ThumbnailPath is null.
/// </summary>
public sealed class NullToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is null || value is string s && string.IsNullOrWhiteSpace(s)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
