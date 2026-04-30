using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SwishLauncher.App.Services;
using SwishLauncher.App.Views;

namespace SwishLauncher.App;

public sealed partial class MainWindow : Window
{
    // Keep a reference so the service isn't GC'd
    private readonly GamepadNavigationService _gamepadNav;

    public MainWindow()
    {
        InitializeComponent();
        SetupWindow();

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
        DispatcherQueue.TryEnqueue(() =>
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen));
    }

    private bool _navigating;

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {

        var currentType = ContentFrame.CurrentSourcePageType;
        if (currentType == typeof(MediaPlayerPage) ||
            currentType == typeof(MediaDetailPage) ||
            currentType == typeof(MediaFolderPage) ||
            currentType == typeof(GameDetailPage))
            return;

        // args.IsSettingsSelected is a focus-driven change when we're mid-navigation;
        // the more reliable guard is to check if we're already on that page.
        if (_navigating) return;

        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            var pageType = tag switch
            {
                "Home" => typeof(HomePage),
                "Games" => typeof(GamesPage),
                "Media" => typeof(MediaPage),
                "Settings" => typeof(SettingsPage),
                _ => typeof(HomePage)
            };

            // Don't navigate if focus drifted to the already-active tab
            if (ContentFrame.CurrentSourcePageType == pageType) return;

            _navigating = true;
            NavigateTo(tag, args.RecommendedNavigationTransitionInfo);
            _navigating = false;
        }
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
