using Microsoft.UI.Xaml.Data;
using System;

namespace SwishLauncher.App.Converters;

/// <summary>Inverts a bool — used to disable buttons while IsBusy is true.</summary>
public sealed class BoolNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;
}
