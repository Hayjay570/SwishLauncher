using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;

namespace SwishLauncher.App.Views;

public sealed partial class MediaDetailPage : Page
{
    public MediaDetailViewModel ViewModel { get; }

    public MediaDetailPage()
    {
        ViewModel = App.Services.GetRequiredService<MediaDetailViewModel>();
        InitializeComponent();

        // Subscribe here rather than OnNavigatedTo so it's wired once for
        // the lifetime of this page instance
        ViewModel.PlayRequested += OnPlayRequested;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is MediaEntry entry)
            ViewModel.LoadFrom(entry);
    }

    private void OnPlayRequested(object? sender, string filePath)
    {
        Frame.Navigate(
            typeof(MediaPlayerPage),
            filePath,
            new DrillInNavigationTransitionInfo());
    }
}

