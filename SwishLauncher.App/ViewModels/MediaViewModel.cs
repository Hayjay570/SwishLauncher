using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    private readonly SwishDbContext _db;
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<MediaEntry> MediaItems { get; } = [];

    [ObservableProperty]
    private MediaEntry? _selectedItem;

    [ObservableProperty]
    private int _selectedIndex;

    public MediaViewModel(SwishDbContext db)
    {
        _db = db;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Title = "Media";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        MediaItems.Clear();
        try
        {
            await Task.Run(() =>
            {
                foreach (var m in _db.Media)
                    _dispatcherQueue.TryEnqueue(() => MediaItems.Add(m));
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
