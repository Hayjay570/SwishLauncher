using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
<<<<<<< HEAD
using Microsoft.UI.Xaml.Media.Animation;
using SwishLauncher.App.Services;
=======
using Microsoft.UI.Xaml.Media;
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
using SwishLauncher.App.Views;

namespace SwishLauncher.App;

public sealed partial class MainWindow : Window
{
<<<<<<< HEAD
    // Keep a reference so the service isn't GC'd
    private readonly GamepadNavigationService _gamepadNav;

=======
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
    public MainWindow()
    {
        InitializeComponent();
        SetupWindow();

<<<<<<< HEAD
        // ── Page transitions ───────────────────────────────────────────────
        // SlideNavigationTransitionInfo gives a horizontal slide between tabs,
        // which feels natural on a TV/gamepad. Swap to DrillInNavigationTransitionInfo
        // for a zoom-in feel, or EntranceThemeTransition for a simple fade-up.
        ContentFrame.ContentTransitions =
        [
            new NavigationThemeTransition()
        ];

        // ── Gamepad navigation ─────────────────────────────────────────────
        _gamepadNav = GamepadNavigationService.Attach(NavigationViewControl, ContentFrame);

        // ── Tab selection ──────────────────────────────────────────────────
        NavigationViewControl.SelectionChanged += NavView_SelectionChanged;
        NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];

        // Escape exits full-screen
=======
        NavigationViewControl.SelectionChanged += NavView_SelectionChanged;

        // Set initial selection
        NavigationViewControl.SelectedItem =
            NavigationViewControl.MenuItems[0];

>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
        if (Content is FrameworkElement root)
        {
            root.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                    AppWindow.SetPresenter(AppWindowPresenterKind.Default);
            };
        }
    }

    private void SetupWindow()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(null);
        AppWindow.Title = "SwishLauncher";
<<<<<<< HEAD
=======

>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
        DispatcherQueue.TryEnqueue(() =>
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen));
    }

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
<<<<<<< HEAD
        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
            NavigateTo(tag, args.RecommendedNavigationTransitionInfo);
    }

    private void NavigateTo(string tag,
        NavigationTransitionInfo? transitionOverride = null)
    {
        var pageType = tag switch
        {
            "Home"     => typeof(HomePage),
            "Games"    => typeof(GamesPage),
            "Media"    => typeof(MediaPage),
            "Settings" => typeof(SettingsPage),
            _          => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType == pageType) return;

        // Use the NavigationView's recommended transition so left/right tabs
        // slide in the correct direction, falling back to a slide transition.
        var info = transitionOverride
            ?? new SlideNavigationTransitionInfo
               { Effect = SlideNavigationTransitionEffect.FromRight };

        ContentFrame.Navigate(pageType, null, info);
    }

    /// <summary>
    /// Called by pages on NavigatedTo so RB/LB cycles through their focus regions.
    /// Pass focusable root-level panels/controls in logical order.
    /// </summary>
    public void SetPageFocusRegions(params UIElement[] regions)
        => _gamepadNav.RegisterFocusRegions(regions);
}
=======
        if (args.SelectedItem is NavigationViewItem item &&
            item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        var pageType = tag switch
        {
            "Home" => typeof(HomePage),
            "Games" => typeof(GamesPage),
            "Media" => typeof(MediaPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
            ContentFrame.Navigate(pageType);
    }
}
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
