using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
using System;
using System.Collections.Specialized;

namespace SwishLauncher.App.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    // The game currently displayed in the hero banner.
    private GameEntry? _heroEntry;

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        InitializeComponent();

        // When FeaturedGames is populated (after LoadCommand completes) we
        // update the hero banner.  CollectionChanged fires on the UI thread
        // because HomeViewModel populates on the dispatcher.
        ViewModel.FeaturedGames.CollectionChanged += OnFeaturedGamesChanged;
    }

    // ── Navigation lifecycle ───────────────────────────────────────────────

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Register focus regions so RB/LB cycles the three strip sections.
        WindowHelper.Current?.SetPageFocusRegions(FeaturedSection, RecentSection, FavouritesSection);
        // Reload every visit so new scans are reflected immediately.
        ViewModel.LoadCommand.Execute(null);
    }

    // ── Hero banner ────────────────────────────────────────────────────────

    private void OnFeaturedGamesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // We only care about a full reset (Clear → Add) which is what LoadAsync does.
        // Grab the first entry and populate the banner.
        if (ViewModel.FeaturedGames.Count == 0)
        {
            HeroBanner.Visibility = Visibility.Collapsed;
            _heroEntry = null;
            return;
        }

        _heroEntry = ViewModel.FeaturedGames[0];
        PopulateHero(_heroEntry);
    }

    private void PopulateHero(GameEntry entry)
    {
        // Source badge: upper-case platform label
        HeroSourceBadge.Text = (entry.Platform ?? string.Empty).ToUpperInvariant();

        // Title
        HeroTitle.Text = entry.Title;

        // Cover art — reuse PathToImageSourceConverter logic inline to avoid
        // a second converter instance just for the hero image.
        if (!string.IsNullOrWhiteSpace(entry.CoverArtPath) &&
            System.IO.File.Exists(entry.CoverArtPath))
        {
            HeroImage.Source = new BitmapImage(new Uri(entry.CoverArtPath));
        }
        else
        {
            HeroImage.Source = null;
        }

        // Show banner and animate it in
        HeroBanner.Visibility = Visibility.Visible;
        var sb = (Storyboard)HeroBanner.Resources["HeroEntranceSb"];
        sb.Begin();
    }

    // ── Hero button handlers ───────────────────────────────────────────────

    private void HeroPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_heroEntry is null) return;
        // Launch the game if it has a launch URI; otherwise open the detail page.
        if (!string.IsNullOrWhiteSpace(_heroEntry.LaunchUri))
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri(_heroEntry.LaunchUri));
        }
        else
        {
            NavigateToDetail(_heroEntry);
        }
    }

    private void HeroDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_heroEntry is not null)
            NavigateToDetail(_heroEntry);
    }

    private void NavigateToDetail(GameEntry entry)
    {
        Frame.Navigate(
            typeof(GameDetailPage),
            entry,
            new SlideNavigationTransitionInfo
                { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    // ── Section card click ─────────────────────────────────────────────────
    // Shared by all three section ItemsRepeaters. The Button's DataContext
    // is the GameEntry supplied by the ItemsRepeater.
    private void GameCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameEntry entry })
            NavigateToDetail(entry);
    }
}
