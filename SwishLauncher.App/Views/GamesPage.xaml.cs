<<<<<<< HEAD
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
using System;
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
using SwishLauncher.App.ViewModels;
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6

namespace SwishLauncher.App.Views;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; }

    public GamesPage()
    {
        ViewModel = App.Services.GetRequiredService<GamesViewModel>();
        InitializeComponent();
    }
<<<<<<< HEAD

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        WindowHelper.Current?.SetPageFocusRegions(GamesCoverFlow);
        ViewModel.LoadCommand.Execute(null);
    }

    private void GamesCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not GameEntry entry) return;

        // Slide detail page in from the right
        Frame.Navigate(
            typeof(GameDetailPage),
            entry,
            new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }
=======
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
}
