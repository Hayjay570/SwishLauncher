using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;
using System.Collections.ObjectModel;

namespace SwishLauncher.App.Views;

/// <summary>
/// Displays the contents of a <see cref="MediaGroup"/> as a mixed coverflow of
/// sub-folder cards and media item cards. Navigating into a sub-folder creates
/// a new instance of this same page (recursive drill-in), so arbitrary nesting
/// depth is handled naturally — e.g. Shows → Season 1 → Episode files.
///
/// Back navigation uses Frame.GoBack() which the NavigationView's back button
/// or the Escape key (configured in MainWindow) will also trigger correctly.
/// </summary>
public sealed partial class MediaFolderPage : Page
{
    // The items shown in the coverflow for this level
    private readonly ObservableCollection<MediaBrowserItem> _items = [];

    public MediaFolderPage()
    {
        InitializeComponent();
        // Wire the coverflow's ItemsSource in code-behind — same pattern as GamesPage
        FolderCoverFlow.ItemsSource = _items;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not MediaGroup group) return;

        FolderTitleBlock.Text = group.FolderName;

        _items.Clear();

        // Sub-folders first (alphabetical), then loose files (alphabetical)
        // — same ordering as the root level in MediaViewModel.RebuildTree()
        foreach (var sub in group.SubGroups)
            _items.Add(new MediaBrowserItem { IsFolder = true, Group = sub });

        foreach (var entry in group.Items)
            _items.Add(new MediaBrowserItem { IsFolder = false, Entry = entry });
    }

    private void FolderCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not MediaBrowserItem browserItem) return;

        if (browserItem.IsFolder && browserItem.Group is not null)
        {
            // Drill deeper into a sub-folder — another MediaFolderPage instance
            Frame.Navigate(
                typeof(MediaFolderPage),
                browserItem.Group,
                new DrillInNavigationTransitionInfo());
        }
        else if (!browserItem.IsFolder && browserItem.Entry is not null)
        {
            Frame.Navigate(
                typeof(MediaDetailPage),
                browserItem.Entry,
                new DrillInNavigationTransitionInfo());
        }
    }

    private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack(new SlideNavigationTransitionInfo
                { Effect = SlideNavigationTransitionEffect.FromLeft });
    }
}
