using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SwishLauncher.App.Converters;

/// <summary>
/// Returns Visible when the bound integer is zero (empty collection),
/// Collapsed otherwise. Used to show "No games found" empty-state text.
/// </summary>
public sealed class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
