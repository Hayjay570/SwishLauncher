<<<<<<< HEAD
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class GamesViewModel : BaseViewModel
{
    private readonly SwishDbContext _db;
    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>Bound to the GridView / ListView on GamesPage.</summary>
    public ObservableCollection<GameEntry> Games { get; } = [];

    [ObservableProperty]
    private GameEntry? _selectedGame;

    [ObservableProperty]
    private int _selectedIndex = 0;

    public GamesViewModel(SwishDbContext db)
    {
        _db = db;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Title = "Games";
    }

    /// <summary>Loads all games from SQLite into the observable collection.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Games.Clear();
        try
        {
            await Task.Run(() =>
            {
                foreach (var g in _db.Games)
                    _dispatcherQueue.TryEnqueue(() => Games.Add(g));
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectGame(GameEntry? game) => SelectedGame = game;
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SwishLauncher.App.ViewModels;

public partial class GamesViewModel : ObservableObject { }
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
