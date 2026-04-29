using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using System;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class MediaDetailViewModel : BaseViewModel
{
    [ObservableProperty] private string    _mediaType     = string.Empty;
    [ObservableProperty] private string    _description   = string.Empty;
    [ObservableProperty] private double    _rating;
    [ObservableProperty] private string    _year          = string.Empty;
    [ObservableProperty] private string    _dateAdded     = string.Empty;
    [ObservableProperty] private string?   _thumbnailPath;
    [ObservableProperty] private bool      _isFavourite;
    [ObservableProperty] private string    _filePath      = string.Empty;

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
    private async Task PlayAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath)) return;
        IsBusy = true;
        try
        {
            // FilePath is a local path like C:\Videos\movie.mp4.
            // Launcher.LaunchUriAsync requires a proper URI, so build one via
            // the Uri(string, UriKind) overload which handles absolute local paths,
            // or explicitly use the file:/// scheme.
            var uri = new Uri(FilePath, UriKind.Absolute);
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
