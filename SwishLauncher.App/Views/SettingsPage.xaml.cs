using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;

namespace SwishLauncher.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        WindowHelper.Current?.SetPageFocusRegions(SettingsScroller);
    }

    private void ThemeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tag }) return;

        var theme = tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark"  => ElementTheme.Dark,
            _       => ElementTheme.Default
        };

        ViewModel.SetThemeCommand.Execute(theme);

        // Propagate immediately to the root frame so Mica updates
        if (WindowHelper.Current?.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }
}
