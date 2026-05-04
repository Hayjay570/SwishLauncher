using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SwishLauncher.App.Views;

public sealed partial class StreamingPlaceholderPage : Page
{
    public StreamingPlaceholderPage() => InitializeComponent();

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
