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

        // Keep the nav bar in sync when GoBack() lands on a top-level page
        ContentFrame.Navigated += ContentFrame_Navigated;

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

    // The tab the user actually intends to be on. Kept separate from the
    // NavigationView's SelectedItem so we can restore it after GoBack() causes
    // SelectionChanged to fire spuriously (SelectionFollowsFocus side-effect).
    private string _intendedTag = "Home";
    private bool _suppressSelectionChanged;

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (_suppressSelectionChanged) return;

        if (args.SelectedItem is not NavigationViewItem { Tag: string tag }) return;

        var pageType = tag switch
        {
            "Home"     => typeof(HomePage),
            "Games"    => typeof(GamesPage),
            "Media"    => typeof(MediaPage),
            "Settings" => typeof(SettingsPage),
            _          => typeof(HomePage)
        };

        // If focus drifted while on a sub-page, the nav bar selection may have
        // changed — restore the intended item without navigating.
        var currentType = ContentFrame.CurrentSourcePageType;
        bool onSubPage = currentType == typeof(MediaPlayerPage) ||
                         currentType == typeof(MediaDetailPage) ||
                         currentType == typeof(MediaFolderPage) ||
                         currentType == typeof(GameDetailPage);

        if (onSubPage)
        {
            RestoreNavSelection();
            return;
        }

        // Genuine user-initiated tab change
        if (ContentFrame.CurrentSourcePageType == pageType) return;

        _intendedTag = tag;
        NavigateTo(tag, args.RecommendedNavigationTransitionInfo);
    }

    /// <summary>
    /// Restores the NavigationView's visual selection to match _intendedTag
    /// without triggering another SelectionChanged.
    /// </summary>
    private void RestoreNavSelection()
    {
        _suppressSelectionChanged = true;
        foreach (var obj in NavigationViewControl.MenuItems)
        {
            if (obj is NavigationViewItem nvi && nvi.Tag is string t && t == _intendedTag)
            {
                NavigationViewControl.SelectedItem = nvi;
                break;
            }
        }
        _suppressSelectionChanged = false;
    }

    /// <summary>
    /// After every navigation, update _intendedTag to reflect which top-level
    /// tab owns the current page. This covers both direct tab navigations and
    /// GoBack() returning from a sub-page, ensuring RestoreNavSelection() always
    /// snaps to the correct tab even if SelectionChanged fires mid-transition.
    /// </summary>
    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var tag = e.SourcePageType switch
        {
            var t when t == typeof(HomePage)       => "Home",
            var t when t == typeof(GamesPage)      => "Games",
            var t when t == typeof(GameDetailPage) => "Games",
            var t when t == typeof(MediaPage)      => "Media",
            var t when t == typeof(MediaFolderPage)=> "Media",
            var t when t == typeof(MediaDetailPage)=> "Media",
            var t when t == typeof(MediaPlayerPage)=> "Media",
            var t when t == typeof(SettingsPage)   => "Settings",
            _                                      => (string?)null
        };

        if (tag is not null)
            _intendedTag = tag;
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
