<<<<<<< HEAD
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
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

public sealed partial class MediaPage : Page
{
    public MediaViewModel ViewModel { get; }

    public MediaPage()
    {
        ViewModel = App.Services.GetRequiredService<MediaViewModel>();
        InitializeComponent();
    }
<<<<<<< HEAD

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        WindowHelper.Current?.SetPageFocusRegions(MediaCoverFlow);
        ViewModel.LoadCommand.Execute(null);
    }

    private void MediaCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not MediaEntry entry) return;

        Frame.Navigate(
            typeof(MediaDetailPage),
            entry,
            new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }
=======
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
}
