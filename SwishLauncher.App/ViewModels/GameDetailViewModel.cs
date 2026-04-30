using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using System;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class GameDetailViewModel : BaseViewModel
{
    [ObservableProperty] private string  _platform       = string.Empty;
    [ObservableProperty] private string  _description    = string.Empty;
    [ObservableProperty] private string  _lastPlayed     = "Never";

    // double? avoids the WinRT AOT marshalling ArgumentException that
    // [ObservableProperty] on a plain double triggers in WinUI 3 / CommunityToolkit 8.x.
    private double _rating;
    public double Rating
    {
        get => _rating;
        set { if (_rating != value) { _rating = value; OnPropertyChanged(); } }
    }

    [ObservableProperty] private string  _playtime       = "0 min";
    [ObservableProperty] private string? _coverArtPath;
    [ObservableProperty] private bool    _isFavourite;
    [ObservableProperty] private string  _launchUri      = string.Empty;

    public void LoadFrom(GameEntry entry)
    {
        Title        = entry.Title;
        Platform     = entry.Platform.ToUpperInvariant();
        Description  = entry.Description ?? "No description available.";
        Rating       = entry.Rating ?? 0;
        CoverArtPath = entry.CoverArtPath;
        LaunchUri    = entry.LaunchUri;
        Playtime     = entry.PlaytimeMinutes >= 60
            ? $"{entry.PlaytimeMinutes / 60}h {entry.PlaytimeMinutes % 60}min"
            : $"{entry.PlaytimeMinutes} min";
        LastPlayed   = entry.LastPlayed.HasValue
            ? entry.LastPlayed.Value.ToString("d MMM yyyy")
            : "Never";
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (string.IsNullOrWhiteSpace(LaunchUri)) return;
        IsBusy = true;
        try
        {
            var uri = new Uri(LaunchUri, UriKind.Absolute);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleFavourite() => IsFavourite = !IsFavourite;
}
