using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;

namespace SwishLauncher.App.Views;

public sealed partial class MediaPage : Page
{
    public MediaViewModel ViewModel { get; }

    private bool _loaded;

    public MediaPage()
    {
        ViewModel = App.Services.GetRequiredService<MediaViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!_loaded)
        {
            _loaded = true;
            ViewModel.LoadCommand.Execute(null);
        }
    }

    private void MediaCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not MediaBrowserItem browserItem) return;

        if (browserItem.IsFolder && browserItem.Group is not null)
        {
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
                new SlideNavigationTransitionInfo
                    { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}

