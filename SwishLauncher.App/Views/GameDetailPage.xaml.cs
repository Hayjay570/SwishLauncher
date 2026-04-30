using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;

namespace SwishLauncher.App.Views;

public sealed partial class GameDetailPage : Page
{
    public GameDetailViewModel ViewModel { get; }

    public GameDetailPage()
    {
        ViewModel = App.Services.GetRequiredService<GameDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is GameEntry entry)
            ViewModel.LoadFrom(entry);
    }

    private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack(new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo
                { Effect = Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromLeft });
    }
}
