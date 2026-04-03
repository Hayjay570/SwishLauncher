using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
using System;

namespace SwishLauncher.App.Views;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; }

    public GamesPage()
    {
        ViewModel = App.Services.GetRequiredService<GamesViewModel>();
        InitializeComponent();
    }

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
}
