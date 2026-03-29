using CommunityToolkit.Mvvm.ComponentModel;

namespace SwishLauncher.App.ViewModels;

/// <summary>
/// Base class for all page ViewModels.
/// Provides IsBusy and Title plumbing used across every page.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;
}
