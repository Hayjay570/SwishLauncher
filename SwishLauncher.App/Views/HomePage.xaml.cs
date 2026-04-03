<<<<<<< HEAD
using Microsoft.Extensions.DependencyInjection;
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

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        InitializeComponent();
    }
<<<<<<< HEAD

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Register focus regions for RB/LB gamepad cycling.
        // Order: content area first, then action buttons.
        WindowHelper.Current?.SetPageFocusRegions(ContentRegion, ActionsRegion);

        // Kick off an initial data load each time the page is shown.
        ViewModel.RefreshCommand.Execute(null);
    }
=======
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
}
