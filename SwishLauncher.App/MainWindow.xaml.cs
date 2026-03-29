using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SwishLauncher.App.Views;

namespace SwishLauncher.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupWindow();

        NavigationViewControl.SelectionChanged += NavView_SelectionChanged;

        // Set initial selection
        NavigationViewControl.SelectedItem =
            NavigationViewControl.MenuItems[0];

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

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
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