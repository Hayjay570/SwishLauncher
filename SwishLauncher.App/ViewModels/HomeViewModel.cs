<<<<<<< HEAD
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
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SwishLauncher.App.ViewModels;

public partial class HomeViewModel : ObservableObject { }
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
