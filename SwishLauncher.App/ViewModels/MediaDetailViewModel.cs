using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using System;

namespace SwishLauncher.App.ViewModels;

public partial class MediaDetailViewModel : BaseViewModel
{
    [ObservableProperty] private string  _mediaType    = string.Empty;
    [ObservableProperty] private string  _description  = string.Empty;
    [ObservableProperty] private string  _year         = string.Empty;
    [ObservableProperty] private string  _genre        = string.Empty;

    // Music-specific
    [ObservableProperty] private string  _artist       = string.Empty;
    [ObservableProperty] private string  _album        = string.Empty;

    // TV-specific
    [ObservableProperty] private string  _showTitle    = string.Empty;
    [ObservableProperty] private string  _episode      = string.Empty;  // e.g. "S02 E05"

    // Manual property — avoids WinRT AOT marshalling ArgumentException on plain double
    // in CommunityToolkit.Mvvm 8.x / WinUI 3.
    private double _rating;
    public double Rating
    {
        get => _rating;
        set { if (_rating != value) { _rating = value; OnPropertyChanged(); } }
    }

    [ObservableProperty] private string  _dateAdded    = string.Empty;
    [ObservableProperty] private string? _thumbnailPath;
    [ObservableProperty] private bool    _isFavourite;
    [ObservableProperty] private string  _filePath     = string.Empty;

    // Controls whether music-specific rows are shown in the detail page
    [ObservableProperty] private bool    _isMusic;
    // Controls whether TV-specific rows are shown
    [ObservableProperty] private bool    _isTv;

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
        ThumbnailPath = entry.ThumbnailPath ?? entry.PosterPath;
        FilePath      = entry.FilePath;
        Year          = entry.Year.HasValue ? entry.Year.Value.ToString() : "Unknown";
        DateAdded     = entry.DateAdded.ToString("d MMM yyyy");
        Genre         = entry.Genre ?? string.Empty;

        // TMDB rating is 0–10; RatingControl is 0–5, so halve it.
        Rating = entry.Rating.HasValue ? Math.Round(entry.Rating.Value / 2.0, 1) : 0;

        // Music fields
        IsMusic      = entry.Type == Core.Models.MediaType.Music;
        Artist       = entry.Artist  ?? string.Empty;
        Album        = entry.Album   ?? string.Empty;

        // TV fields
        IsTv         = entry.Type == Core.Models.MediaType.TvEpisode;
        ShowTitle    = entry.ShowTitle ?? string.Empty;
        Episode      = (entry.SeasonNumber.HasValue && entry.EpisodeNumber.HasValue)
                       ? $"S{entry.SeasonNumber:D2}  E{entry.EpisodeNumber:D2}"
                       : string.Empty;
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
