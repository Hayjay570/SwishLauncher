using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    /// <summary>Greeting shown on the home hero banner.</summary>
    [ObservableProperty]
    private string _greeting = "Welcome back!";

    /// <summary>Currently highlighted/featured item title (stub).</summary>
    [ObservableProperty]
    private string _featuredTitle = "Nothing featured yet";

    public HomeViewModel()
    {
        Title = "Home";
    }

    /// <summary>
    /// Refreshes featured content.
    /// Call from OnNavigatedTo or wire to a Refresh button.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            // TODO: pull real featured items from IGameSource / IMediaSource
            await Task.Delay(300); // placeholder latency
            FeaturedTitle = "Featured content loaded";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
