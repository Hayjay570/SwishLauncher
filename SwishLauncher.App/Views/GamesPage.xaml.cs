using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
using System;
using Windows.System;

namespace SwishLauncher.App.Views;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; }
    private readonly SettingsViewModel _settings;

    public GamesPage()
    {
        ViewModel  = App.Services.GetRequiredService<GamesViewModel>();
        _settings  = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        WindowHelper.Current?.SetPageFocusRegions(GamesCoverFlow);
        ViewModel.LoadCommand.Execute(null);
        BuildStoresFlyout();
    }

    // ── Stores flyout ─────────────────────────────────────────────────────────

    private void BuildStoresFlyout()
    {
        StoresFlyoutPanel.Children.Clear();

        void AddEntry(string label, string glyph, bool isEnabled, string launchUri)
        {
            if (!isEnabled) return;

            var btn = new Button
            {
                Background   = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush  = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                HorizontalAlignment          = HorizontalAlignment.Stretch,
                HorizontalContentAlignment   = HorizontalAlignment.Left,
                Padding      = new Thickness(8, 10, 8, 10),
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            panel.Children.Add(new FontIcon { Glyph = glyph, FontSize = 16 });
            panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            btn.Content = panel;

            var uri = new Uri(launchUri);
            btn.Click += (_, _) =>
            {
                StoresFlyout.Hide();
                _ = Launcher.LaunchUriAsync(uri);
            };

            StoresFlyoutPanel.Children.Add(btn);
        }

        AddEntry("Steam",            "\uE7FC", _settings.IsSteamEnabled,  "steam://open/games");
        AddEntry("Xbox / Microsoft", "\uE990", _settings.IsXboxEnabled,   "ms-windows-store://pdp/?ProductId=9MV0B5HZVK9Z");
        AddEntry("Manual library",   "\uE8F4", _settings.IsManualEnabled, "swishlauncher://manual");

        // If nothing is enabled, show a hint
        if (StoresFlyoutPanel.Children.Count == 0)
        {
            StoresFlyoutPanel.Children.Add(new TextBlock
            {
                Text    = "No stores enabled.\nEnable them in Settings.",
                Opacity = 0.6,
                TextWrapping = TextWrapping.Wrap,
                Margin  = new Thickness(8, 4, 8, 4)
            });
        }
    }

    // ── CoverFlow activation ──────────────────────────────────────────────────

    private void GamesCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not GameEntry entry) return;

        Frame.Navigate(
            typeof(GameDetailPage),
            entry,
            new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }
}
