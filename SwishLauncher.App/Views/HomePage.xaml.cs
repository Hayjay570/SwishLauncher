using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;

namespace SwishLauncher.App.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Reload data each time the Home tab is entered so new scans are reflected.
        ViewModel.LoadCommand.Execute(null);
    }

    // ── Card click ─────────────────────────────────────────────────────────
    // All three section templates share this handler. The Button's DataContext
    // is the GameEntry bound by ItemsRepeater.
    private void GameCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameEntry entry })
        {
            Frame.Navigate(
                typeof(GameDetailPage),
                entry,
                new SlideNavigationTransitionInfo
                    { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
