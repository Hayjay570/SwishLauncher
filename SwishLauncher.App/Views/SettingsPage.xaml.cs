<<<<<<< HEAD
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
=======
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using Microsoft.Extensions.DependencyInjection;
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
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
<<<<<<< HEAD

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
=======
}

>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
