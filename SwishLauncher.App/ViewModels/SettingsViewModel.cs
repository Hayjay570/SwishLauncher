using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.UI;
using System.Collections.ObjectModel;

namespace SwishLauncher.App.ViewModels;

// ── Streaming service descriptor ─────────────────────────────────────────────

public partial class StreamingServiceItem : ObservableObject
{
    public string Name   { get; set; } = string.Empty;
    public string Url    { get; set; } = string.Empty;
    public string Glyph  { get; set; } = "\uE8B2";
    public Color  Color1 { get; set; } = Colors.DarkRed;
    public Color  Color2 { get; set; } = Colors.Black;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        ApplicationData.Current.LocalSettings.Values[$"streaming_{Name}"] = value;
    }
}

// ── SettingsViewModel ─────────────────────────────────────────────────────────

public partial class SettingsViewModel : BaseViewModel
{
    // Appearance
    [ObservableProperty]
    private ElementTheme _appTheme = ElementTheme.Default;

    [ObservableProperty]
    private bool _useHighContrast;

    // Store toggles
    [ObservableProperty]
    private bool _isSteamEnabled;

    [ObservableProperty]
    private bool _isXboxEnabled;

    [ObservableProperty]
    private bool _isManualEnabled;

    partial void OnIsSteamEnabledChanged(bool value)   => Persist("store_Steam",   value);
    partial void OnIsXboxEnabledChanged(bool value)    => Persist("store_Xbox",    value);
    partial void OnIsManualEnabledChanged(bool value)  => Persist("store_Manual",  value);

    // Streaming services
    public ObservableCollection<StreamingServiceItem> StreamingServices { get; } = new();

    public SettingsViewModel()
    {
        Title = "Settings";
        LoadSettings();
    }

    [RelayCommand]
    private void SetTheme(ElementTheme theme) => AppTheme = theme;

    private static void Persist(string key, bool value) =>
        ApplicationData.Current.LocalSettings.Values[key] = value;

    private static bool Load(string key, bool defaultValue = true)
    {
        var v = ApplicationData.Current.LocalSettings.Values[key];
        return v is bool b ? b : defaultValue;
    }

    private static Color Hex(byte r, byte g, byte b) =>
        Color.FromArgb(255, r, g, b);

    private void LoadSettings()
    {
        _isSteamEnabled  = Load("store_Steam");
        _isXboxEnabled   = Load("store_Xbox");
        _isManualEnabled = Load("store_Manual");

        var catalogue = new[]
        {
            new StreamingServiceItem { Name = "Netflix",     Url = "https://www.netflix.com",            Glyph = "\uE714", Color1 = Hex(229,  9, 20), Color2 = Hex( 86,  0,  0) },
            new StreamingServiceItem { Name = "Prime Video", Url = "https://www.amazon.com/Prime-Video",  Glyph = "\uE8B2", Color1 = Hex(  0,168,224), Color2 = Hex(  0, 61, 82) },
            new StreamingServiceItem { Name = "Disney+",     Url = "https://www.disneyplus.com",          Glyph = "\uE734", Color1 = Hex( 17, 52,166), Color2 = Hex(  5, 15, 64) },
            new StreamingServiceItem { Name = "Apple TV+",   Url = "https://tv.apple.com",                Glyph = "\uE8EA", Color1 = Hex( 85, 85, 85), Color2 = Hex( 17, 17, 17) },
            new StreamingServiceItem { Name = "HBO Max",     Url = "https://www.max.com",                 Glyph = "\uE7F4", Color1 = Hex(107, 10,201), Color2 = Hex( 26,  0, 58) },
            new StreamingServiceItem { Name = "Hulu",        Url = "https://www.hulu.com",                Glyph = "\uE786", Color1 = Hex( 28,231,131), Color2 = Hex( 10, 74, 43) },
            new StreamingServiceItem { Name = "Crunchyroll", Url = "https://www.crunchyroll.com",         Glyph = "\uE8B2", Color1 = Hex(244,117, 33), Color2 = Hex( 90, 36,  0) },
            new StreamingServiceItem { Name = "YouTube",     Url = "https://www.youtube.com",             Glyph = "\uE714", Color1 = Hex(255,  0,  0), Color2 = Hex( 96,  0,  0) },
        };

        foreach (var svc in catalogue)
        {
            bool defaultOn = svc.Name is "Netflix" or "Prime Video" or "Disney+";
            svc.IsEnabled = Load($"streaming_{svc.Name}", defaultOn);
            StreamingServices.Add(svc);
        }
    }
}
