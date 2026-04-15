using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Games.Sources;

public class XboxGameSource : IGameSource
{
    public string SourceName => "Xbox";

    public Task<IEnumerable<GameEntry>> ScanAsync(CancellationToken ct = default)
        => Task.Run(() => Scan(ct), ct);

    private static IEnumerable<GameEntry> Scan(CancellationToken ct)
    {
        // Xbox Game Pass / MS Store games appear in the Gaming Services package registry
        const string root =
            @"SOFTWARE\Microsoft\GamingServices\PackageRepository\Root";

        using var rootKey = Registry.LocalMachine.OpenSubKey(root);
        if (rootKey is null) yield break;

        foreach (var packageName in rootKey.GetSubKeyNames())
        {
            ct.ThrowIfCancellationRequested();

            using var pkgKey = rootKey.OpenSubKey(packageName);
            if (pkgKey is null) continue;

            // Each sub-key under the package is a title entry
            foreach (var titleName in pkgKey.GetSubKeyNames())
            {
                using var titleKey = pkgKey.OpenSubKey(titleName);
                if (titleKey is null) continue;

                var displayName = titleKey.GetValue("DisplayName") as string
                               ?? titleKey.GetValue("Name") as string;
                var pfn = titleKey.GetValue("PackageFamilyName") as string;

                if (string.IsNullOrWhiteSpace(displayName) ||
                    string.IsNullOrWhiteSpace(pfn)) continue;

                // Resolve @{...} resource references to plain text
                displayName = ResolveResourceString(displayName);
                if (string.IsNullOrWhiteSpace(displayName)) continue;

                // Cover art: Gaming Services caches square tiles under
                // %LOCALAPPDATA%\Packages\<pfn>\LocalCache — but availability
                // varies. We leave CoverArtPath null and let the placeholder
                // icon show; a future metadata-fetch pass can fill this in.
                yield return new GameEntry
                {
                    Title = displayName,
                    Platform = "Xbox",
                    LaunchUri = $"shell:AppsFolder\\{pfn}!App",
                };
            }
        }
    }

    /// <summary>
    /// Resource strings like "@{Halo_8wekyb3d8bbwe?ms-resource://...}" can't be
    /// resolved without SHLoadIndirectString (P/Invoke). For now we return null
    /// so unresolvable entries are skipped rather than showing raw resource URIs.
    /// A proper implementation would P/Invoke SHLoadIndirectString.
    /// </summary>
    private static string? ResolveResourceString(string s)
        => s.StartsWith('@') ? null : s;
}
