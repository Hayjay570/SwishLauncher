using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Models;

namespace SwishLauncher.App.Views;

public sealed partial class MediaDetailPage : Page
{
    public MediaDetailViewModel ViewModel { get; }

    public MediaDetailPage()
    {
        ViewModel = App.Services.GetRequiredService<MediaDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MediaEntry entry)
            ViewModel.LoadFrom(entry);
    }
}
