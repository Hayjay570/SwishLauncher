using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace SwishLauncher.App.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private ElementTheme _appTheme = ElementTheme.Default;

    [ObservableProperty]
    private bool _useHighContrast;

    public SettingsViewModel()
    {
        Title = "Settings";
    }

    /// <summary>
    /// Called by SettingsPage.xaml.cs after the command fires
    /// to propagate the theme to the root FrameworkElement.
    /// </summary>
    [RelayCommand]
    private void SetTheme(ElementTheme theme) => AppTheme = theme;
}
