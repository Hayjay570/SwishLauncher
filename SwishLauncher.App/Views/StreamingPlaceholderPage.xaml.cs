using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SwishLauncher.App.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.System;

namespace SwishLauncher.App.Views;

public sealed partial class StreamingPlaceholderPage : Page, INotifyPropertyChanged
{
    // ── Observable properties for x:Bind ─────────────────────────────────────

    private ObservableCollection<StreamingServiceItem> _enabledServices = new();
    public  ObservableCollection<StreamingServiceItem>  EnabledServices
    {
        get => _enabledServices;
        private set { _enabledServices = value; PropertyChanged?.Invoke(this, new(nameof(EnabledServices))); }
    }

    private int _selectedIndex;
    public  int  SelectedIndex
    {
        get => _selectedIndex;
        set { _selectedIndex = value; PropertyChanged?.Invoke(this, new(nameof(SelectedIndex))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Construction ──────────────────────────────────────────────────────────

    private readonly SettingsViewModel _settings;

    public StreamingPlaceholderPage()
    {
        _settings = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshServices();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshServices()
    {
        EnabledServices.Clear();
        foreach (var svc in _settings.StreamingServices)
        {
            if (svc.IsEnabled)
                EnabledServices.Add(svc);
        }

        SelectedIndex = 0;

        // Show/hide empty state
        EmptyPanel.Visibility  = EnabledServices.Count == 0 ? Visibility.Visible  : Visibility.Collapsed;
        StreamingCoverFlow.Visibility = EnabledServices.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void StreamingCoverFlow_ItemActivated(object sender, object? item)
    {
        if (item is not StreamingServiceItem svc) return;
        _ = Launcher.LaunchUriAsync(new System.Uri(svc.Url));
    }
}
