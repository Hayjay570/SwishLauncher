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

        Player.Source = source;
        Player.MediaPlayer.Play();
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
