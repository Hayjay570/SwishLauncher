using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using SwishLauncher.Core.Services;

namespace SwishLauncher.App.ViewModels;

public partial class GamesViewModel : BaseViewModel
{
    private readonly GameLibraryService _library;
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<GameEntry> Games { get; } = [];

    [ObservableProperty] private GameEntry? _selectedGame;
    [ObservableProperty] private int _selectedIndex = 0;

    public GamesViewModel(GameLibraryService library)
    {
        _library = library;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Title = "Games";
    }

    /// <summary>
    /// Fast load from DB — used on every page navigation.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Games.Clear();
        try
        {
            var games = await _library.GetAllAsync();
            foreach (var g in games)
                Games.Add(g);
        }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// Full scan + sync — triggered by the refresh button.
    /// Can take a few seconds, so it's a separate command.
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        Games.Clear();
        try
        {
            var games = await _library.ScanAndSyncAsync();
            foreach (var g in games)
                Games.Add(g);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectGame(GameEntry? game) => SelectedGame = game;
}