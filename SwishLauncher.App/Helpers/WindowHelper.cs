using Microsoft.UI.Xaml;

namespace SwishLauncher.App.Helpers;

/// <summary>
/// Safe way for pages and controls to reach the MainWindow instance
/// without relying on XamlRoot.Content casting, which can return null
/// during certain navigation lifecycle moments.
/// </summary>
public static class WindowHelper
{
    /// <summary>
    /// Returns the current MainWindow, or null if it hasn't been created yet.
    /// Prefer this over (App.Current as App)?.Window casts in page code-behinds.
    /// </summary>
    public static MainWindow? Current =>
        (Application.Current as App)?.ActiveWindow as MainWindow;
}
