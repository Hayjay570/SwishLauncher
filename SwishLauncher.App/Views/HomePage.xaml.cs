using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.Helpers;
using SwishLauncher.App.ViewModels;

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

        // Register focus regions for RB/LB gamepad cycling.
        // Order: content area first, then action buttons.
        WindowHelper.Current?.SetPageFocusRegions(ContentRegion, ActionsRegion);

        // Kick off an initial data load each time the page is shown.
        ViewModel.RefreshCommand.Execute(null);
    }
}
