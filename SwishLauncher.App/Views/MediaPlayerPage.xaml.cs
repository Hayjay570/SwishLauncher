using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace SwishLauncher.App.Views;

/// <summary>
/// Full-screen in-app media player page. Receives a local file path string
/// as its navigation parameter and plays it via MediaPlayerElement.
/// Audio files play with a black background (no video track — expected).
/// </summary>
public sealed partial class MediaPlayerPage : Page
{
    public MediaPlayerPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not string filePath) return;

        // Local paths need explicit file:/// scheme for MediaSource
        var uri = new System.Uri("file:///" + filePath.Replace('\\', '/'));
        var source = MediaSource.CreateFromUri(uri);

        // ── Sidecar subtitle support ───────────────────────────────────────
        // Look for an external subtitle file next to the video.
        // Supported: .srt (SubRip), .vtt (WebVTT) — checked in that order.
        // The TimedTextSource is attached before the source is assigned to the
        // player so the track is available from the first frame.
        AttachSidecarSubtitles(source, filePath);

        Player.Source = source;
        Player.MediaPlayer.Play();
    }

    private static void AttachSidecarSubtitles(MediaSource source, string videoPath)
    {
        var dir = System.IO.Path.GetDirectoryName(videoPath) ?? string.Empty;
        var name = System.IO.Path.GetFileNameWithoutExtension(videoPath);

        string[] extensions = [".srt", ".vtt"];
        foreach (var ext in extensions)
        {
            var subPath = System.IO.Path.Combine(dir, name + ext);
            if (!System.IO.File.Exists(subPath)) continue;

            var subUri = new System.Uri("file:///" + subPath.Replace('\\', '/'));
            var timedText = TimedTextSource.CreateFromUri(subUri);
            source.ExternalTimedTextSources.Add(timedText);
            break; // Attach first match only — prefer .srt over .vtt
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // Stop and release the player when navigating away
        Player.MediaPlayer.Pause();
        Player.Source = null;
    }

    private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack(new SlideNavigationTransitionInfo
                { Effect = SlideNavigationTransitionEffect.FromLeft });
    }
}
