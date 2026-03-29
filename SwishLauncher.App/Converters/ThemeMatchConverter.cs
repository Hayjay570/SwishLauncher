using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SwishLauncher.App.Converters;

/// <summary>
/// Returns true when the bound ElementTheme matches the ConverterParameter string.
/// Used to drive the IsChecked state of the theme RadioButtons on SettingsPage.
/// 
/// Usage in XAML:
///   IsChecked="{x:Bind ViewModel.AppTheme, Mode=OneWay,
///       Converter={StaticResource ThemeMatchConverter},
///       ConverterParameter=Dark}"
/// </summary>
public sealed class ThemeMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ElementTheme current || parameter is not string label)
            return false;

        return label switch
        {
            "Light"   => current == ElementTheme.Light,
            "Dark"    => current == ElementTheme.Dark,
            "Default" => current == ElementTheme.Default,
            _         => false
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
