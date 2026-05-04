using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using SwishLauncher.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly GameLibraryService _library;

    /// <summary>Top-5 most-recently-added games (Featured strip).</summary>
    public ObservableCollection<GameEntry> FeaturedGames { get; } = [];

    /// <summary>Games with a LastPlayed date, newest first.</summary>
    public ObservableCollection<GameEntry> RecentlyPlayed { get; } = [];

    /// <summary>Favourited games, ordered by playtime descending.</summary>
    public ObservableCollection<GameEntry> Favourites { get; } = [];

    public HomeViewModel(GameLibraryService library)
    {
        _library = library;
        Title = "Home";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var featured = await _library.GetFeaturedAsync();
            var recent   = await _library.GetRecentlyPlayedAsync();
            var favs     = await _library.GetFavouritesAsync();

            FeaturedGames.Clear();
            foreach (var g in featured) FeaturedGames.Add(g);

            RecentlyPlayed.Clear();
            foreach (var g in recent)   RecentlyPlayed.Add(g);

            Favourites.Clear();
            foreach (var g in favs)     Favourites.Add(g);
        }
        finally { IsBusy = false; }
    }
}
