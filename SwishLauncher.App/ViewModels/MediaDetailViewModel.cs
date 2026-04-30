using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using System;

namespace SwishLauncher.App.ViewModels;

public partial class MediaDetailViewModel : BaseViewModel
{
    [ObservableProperty] private string  _mediaType   = string.Empty;
    [ObservableProperty] private string  _description = string.Empty;
    [ObservableProperty] private string  _year        = string.Empty;

    // Manual property — avoids WinRT AOT marshalling ArgumentException on plain double
    // in CommunityToolkit.Mvvm 8.x / WinUI 3.
    private double _rating;
    public double Rating
    {
        get => _rating;
        set { if (_rating != value) { _rating = value; OnPropertyChanged(); } }
    }

    [ObservableProperty] private string  _dateAdded   = string.Empty;
    [ObservableProperty] private string? _thumbnailPath;
    [ObservableProperty] private bool    _isFavourite;
    [ObservableProperty] private string  _filePath    = string.Empty;

    /// <summary>
    /// Raised when the user presses Play. MediaDetailPage subscribes and
    /// navigates to MediaPlayerPage, keeping navigation out of the ViewModel.
    /// </summary>
    public event EventHandler<string>? PlayRequested;

    public void LoadFrom(MediaEntry entry)
    {
        Title         = entry.Title;
        MediaType     = entry.Type.ToString().ToUpperInvariant();
        Description   = entry.Description ?? "No description available.";
        ThumbnailPath = entry.ThumbnailPath;
        FilePath      = entry.FilePath;
        Year          = entry.Year.HasValue ? entry.Year.Value.ToString() : "Unknown";
        DateAdded     = entry.DateAdded.ToString("d MMM yyyy");
    }

    [RelayCommand]
    private void Play()
    {
        if (!string.IsNullOrWhiteSpace(FilePath))
            PlayRequested?.Invoke(this, FilePath);
    }

    [RelayCommand]
    private void ToggleFavourite() => IsFavourite = !IsFavourite;
}

