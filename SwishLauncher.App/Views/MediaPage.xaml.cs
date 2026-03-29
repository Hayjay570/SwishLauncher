using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;

namespace SwishLauncher.App.Views;

public sealed partial class MediaPage : Page
{
    public MediaViewModel ViewModel { get; }

    public MediaPage()
    {
        ViewModel = App.Services.GetRequiredService<MediaViewModel>();
        InitializeComponent();
    }

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
}
