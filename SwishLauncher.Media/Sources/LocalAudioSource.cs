using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Media.Sources;

/// <summary>
/// Scans the user's Music folder (and any extra configured folders) for
/// common audio file extensions and returns a <see cref="MediaEntry"/> per file.
/// Album art extraction is deferred to a future week; ThumbnailPath stays null.
/// </summary>
public class LocalAudioSource : IMediaSource
{
    public string SourceName => "Local Audio";

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".aac", ".m4a", ".ogg", ".wma",
        ".wav", ".alac", ".aiff", ".opus"
    };

    private readonly IReadOnlyList<string> _rootFolders;

    public LocalAudioSource(IEnumerable<string>? extraFolders = null)
    {
        var defaults = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
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
                if (!AudioExtensions.Contains(ext)) continue;

                yield return BuildEntry(file);
            }
        }
    }

    private static MediaEntry BuildEntry(string filePath)
    {
        var info = new FileInfo(filePath);
        var raw = Path.GetFileNameWithoutExtension(filePath);
        var title = SanitiseTitle(raw);

        return new MediaEntry
        {
            Title         = title,
            Type          = MediaType.Music,
            FilePath      = filePath,
            ThumbnailPath = null,
            DateAdded     = info.CreationTimeUtc,
        };
    }

    private static string SanitiseTitle(string raw)
        => System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(raw.Replace('.', ' ').Replace('_', ' ').Replace('-', ' '));
}
