using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwishLauncher.Core.Models;
using SwishLauncher.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SwishLauncher.App.ViewModels;

// ── MediaGroup ─────────────────────────────────────────────────────────────
// Represents one folder node in the media tree. Can contain sub-folders
// (SubGroups) and/or direct media files (Items) — both are valid at any level,
// which handles series with multiple seasons, loose files in a root folder, etc.

public class MediaGroup(string folderName, string? fullPath = null)
{
    public string  FolderName { get; } = folderName;
    public string? FullPath   { get; } = fullPath;

    public ObservableCollection<MediaGroup> SubGroups { get; } = [];
    public ObservableCollection<MediaEntry> Items     { get; } = [];
}

// ── MediaBrowserItem ───────────────────────────────────────────────────────
// A unified wrapper used as the ItemsSource element for CoverFlowControl on
// both MediaPage and MediaFolderPage. Lets one DataTemplate handle both folder
// cards and media item cards without requiring two separate controls.

public class MediaBrowserItem
{
    public bool        IsFolder { get; init; }
    public MediaGroup? Group    { get; init; }   // valid when IsFolder = true
    public MediaEntry? Entry    { get; init; }   // valid when IsFolder = false
    public string      Label    => IsFolder ? Group!.FolderName : Entry!.Title;
}

// ── MediaViewModel ─────────────────────────────────────────────────────────

public partial class MediaViewModel : BaseViewModel
{
    private readonly MediaLibraryService _library;

    // The flat list — kept as the source of truth for RebuildTree()
    private readonly List<MediaEntry> _allItems = [];

    // Root browser items exposed to MediaPage's CoverFlowControl
    public ObservableCollection<MediaBrowserItem> RootItems { get; } = [];

    [ObservableProperty] private int _selectedIndex;

    public MediaViewModel(MediaLibraryService library)
    {
        _library = library;
        Title = "Media";
    }

    /// <summary>Fast load from DB — used on first page navigation.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        //SelectedIndex = 0;
        try
        {
            var items = await _library.GetAllAsync();
            _allItems.Clear();
            _allItems.AddRange(items);
            RebuildTree();
        }
        finally { IsBusy = false; }
    }

    /// <summary>Full scan + sync — triggered by the Scan button.</summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        //SelectedIndex = 0;
        try
        {
            var items = await _library.ScanAndSyncAsync();
            _allItems.Clear();
            _allItems.AddRange(items);
            RebuildTree();
        }
        finally { IsBusy = false; }
    }

    // ── Tree building ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds a recursive folder tree from the flat <see cref="_allItems"/> list
    /// and populates <see cref="RootItems"/> with the top-level browser items.
    ///
    /// Strategy:
    ///   1. Determine each item's path relative to the nearest scan root
    ///      (%USERPROFILE%\Videos or %USERPROFILE%\Music).
    ///   2. Split the relative path into segments. Items with no sub-folder
    ///      segment are "loose" and surface directly at root level.
    ///   3. Items with one or more sub-folder segments are grouped recursively.
    ///   4. Root-level folders and loose files are mixed into RootItems —
    ///      folders first (alphabetical), then loose files (alphabetical).
    /// </summary>
    private void RebuildTree()
    {
        RootItems.Clear();

        // Known scan roots — matches what LocalVideoSource / LocalAudioSource use
        var scanRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        };

        // Build a virtual root group that owns the whole tree
        var root = new MediaGroup("root");

        foreach (var entry in _allItems.OrderBy(m => m.Title))
            InsertEntry(root, entry, scanRoots);

        // Flatten root into RootItems: sub-folders first, then loose files
        foreach (var sub in root.SubGroups.OrderBy(g => g.FolderName))
            RootItems.Add(new MediaBrowserItem { IsFolder = true, Group = sub });

        foreach (var item in root.Items)
            RootItems.Add(new MediaBrowserItem { IsFolder = false, Entry = item });
    }

    /// <summary>
    /// Recursively inserts <paramref name="entry"/> into the group tree rooted
    /// at <paramref name="parent"/>, computing the relative path against the
    /// first matching scan root and splitting on directory separators.
    /// </summary>
    private static void InsertEntry(MediaGroup parent, MediaEntry entry, string[] scanRoots)
    {
        // Find the relative path from the nearest scan root
        var relative = GetRelativePath(entry.FilePath, scanRoots);

        // Split into segments — first N-1 are folder names, last is the filename
        var parts = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        // Only filename, no sub-folder — loose file directly under scan root
        if (parts.Length <= 1)
        {
            parent.Items.Add(entry);
            return;
        }

        // Walk / create folder nodes for all segments except the last (filename)
        var current = parent;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var folderName = parts[i];
            var existing   = current.SubGroups.FirstOrDefault(g => g.FolderName == folderName);

            if (existing is null)
            {
                var folderPath = Path.Combine(
                    current.FullPath ?? string.Empty, folderName);
                existing = new MediaGroup(folderName, folderPath);
                current.SubGroups.Add(existing);
            }

            current = existing;
        }

        current.Items.Add(entry);
    }

    private static string GetRelativePath(string filePath, string[] roots)
    {
        foreach (var root in roots)
        {
            if (!string.IsNullOrEmpty(root) &&
                filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                var rel = filePath[root.Length..].TrimStart(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return rel;
            }
        }

        // File isn't under any known root — surface just the filename
        return Path.GetFileName(filePath);
    }
}
