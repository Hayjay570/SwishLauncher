using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Media.Sources;

/// <summary>
/// Scans the user's Videos folder (and any extra configured folders) for
/// common video file extensions and returns a <see cref="MediaEntry"/> per file.
/// Thumbnail extraction is deferred to a future week; ThumbnailPath stays null.
/// </summary>
public class LocalVideoSource : IMediaSource
{
    public string SourceName => "Local Videos";

    // Extensions we recognise as video
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".ts",
        ".mpg", ".mpeg", ".flv", ".webm", ".divx", ".xvid"
    };

    // Root folders to scan — Videos folder by default; extend in Week 9 Settings
    private readonly IReadOnlyList<string> _rootFolders;

    public LocalVideoSource(IEnumerable<string>? extraFolders = null)
    {
        var defaults = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };

        if (extraFolders is not null)
            defaults.AddRange(extraFolders.Where(Directory.Exists));

        _rootFolders = defaults.Where(Directory.Exists).ToList();
    }

    public Task<IEnumerable<MediaEntry>> ScanAsync(CancellationToken ct = default)
        => Task.Run(() => Scan(ct), ct);

    private IEnumerable<MediaEntry> Scan(CancellationToken ct)
    {
        foreach (var folder in _rootFolders)
        {
            ct.ThrowIfCancellationRequested();

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*.*",
                    SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                var ext = Path.GetExtension(file);
                if (!VideoExtensions.Contains(ext)) continue;

                yield return BuildEntry(file);
            }
        }
    }

    private static MediaEntry BuildEntry(string filePath)
    {
        var info = new FileInfo(filePath);

        // Derive a friendly title from the filename:
        // strip extension, replace dots/underscores/hyphens with spaces,
        // then title-case. E.g. "my.movie.2023.mkv" → "My Movie 2023"
        var raw = Path.GetFileNameWithoutExtension(filePath);
        var title = SanitiseTitle(raw);

        return new MediaEntry
        {
            Title         = title,
            Type          = MediaType.Movie,
            FilePath      = filePath,
            ThumbnailPath = null,           // thumbnail extraction deferred
            DateAdded     = info.CreationTimeUtc,
        };
    }

    private static string SanitiseTitle(string raw)
        => System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(raw.Replace('.', ' ').Replace('_', ' ').Replace('-', ' '));
}
