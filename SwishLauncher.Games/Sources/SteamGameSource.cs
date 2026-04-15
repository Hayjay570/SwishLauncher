using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;
using System.Text.RegularExpressions;

namespace SwishLauncher.Games.Sources;

public class SteamGameSource : IGameSource
{
    public string SourceName => "Steam";

    public Task<IEnumerable<GameEntry>> ScanAsync(CancellationToken ct = default)
        => Task.Run(() => Scan(ct), ct);

    private IEnumerable<GameEntry> Scan(CancellationToken ct)
    {
        var steamPath = FindSteamPath();
        if (steamPath is null) yield break;

        // appcache always lives under the Steam root, regardless of which
        // library the game is installed to
        //var librarycache = Path.Combine(steamPath, "appcache", "librarycache");
        var librarycache = Path.Combine(steamPath, "appcache", "librarycache");
        System.Diagnostics.Debug.WriteLine($"[Steam] root={steamPath}");
        System.Diagnostics.Debug.WriteLine($"[Steam] librarycache exists={Directory.Exists(librarycache)}");
        // Then list a few files if it exists
        if (Directory.Exists(librarycache))
            foreach (var f in Directory.EnumerateFiles(librarycache).Take(5))
                System.Diagnostics.Debug.WriteLine($"[Steam] art: {f}");
        if (Directory.Exists(librarycache))
            foreach (var f in Directory.EnumerateFiles(librarycache, "*.jpg").Take(20))
                System.Diagnostics.Debug.WriteLine($"[Steam] art: {Path.GetFileName(f)}");
        //here

        foreach (var libraryPath in FindLibraryPaths(steamPath))
        {
            ct.ThrowIfCancellationRequested();

            var appsDir = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(appsDir)) continue;

            foreach (var manifest in Directory.EnumerateFiles(appsDir, "appmanifest_*.acf"))
            {
                ct.ThrowIfCancellationRequested();
                var entry = ParseManifest(manifest, librarycache);
                if (entry is not null) yield return entry;
            }
        }
    }

    private static string? FindSteamPath()
    {
        // Try registry first (most reliable on Windows)
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        var path = key?.GetValue("SteamPath") as string;
        if (path is not null && Directory.Exists(path)) return path;

        // Fallback to common install location
        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }

    private static IEnumerable<string> FindLibraryPaths(string steamRoot)
    {
        // Steam itself is always library 0
        yield return steamRoot;

        var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) yield break;

        // Match: "path"  "D:\\Games\\Steam"
        var pathRx = new Regex(
            @"""path""\s+""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (Match m in pathRx.Matches(File.ReadAllText(vdf)))
        {
            var p = m.Groups[1].Value.Replace(@"\\", @"\");
            if (Directory.Exists(p)) yield return p;
        }
    }

    private static GameEntry? ParseManifest(string manifestPath, string librarycache)
    {
        var text = File.ReadAllText(manifestPath);

        var appId = ExtractVdfValue(text, "appid");
        var name = ExtractVdfValue(text, "name");
        var state = ExtractVdfValue(text, "StateFlags");

        if (appId is null || name is null) return null;
        if (!int.TryParse(state, out var flags) || (flags & 4) == 0) return null;

        // Art lives in librarycache\{appId}\{filename}
        var gameArtDir = Path.Combine(librarycache, appId);
        string? cover = null;

        foreach (var filename in new[] { "library_600x900.jpg", "header.jpg" })
        {
            var candidate = Path.Combine(gameArtDir, filename);
            if (File.Exists(candidate)) { cover = candidate; break; }
        }

        return new GameEntry
        {
            Title = name,
            Platform = "Steam",
            LaunchUri = $"steam://rungameid/{appId}",
            CoverArtPath = cover,
        };
    }

    private static string? ExtractVdfValue(string text, string key)
    {
        var m = Regex.Match(text,
            $@"""{Regex.Escape(key)}""\s+""([^""]+)""",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }
}
