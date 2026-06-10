using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;
using TagLib;

namespace SwishLauncher.Media.Sources;

/// <summary>
/// Scans the user's Music folder (and any extra configured folders) for
/// common audio file extensions. Reads ID3/Vorbis/FLAC tags via TagLib# to
/// populate Title, Artist, Album, Year, TrackNumber, and embedded album art.
/// Falls back to filename-derived title when tags are absent or unreadable.
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

    // Album-art cache folder: %LocalAppData%\SwishLauncher\AlbumArt\
    private static readonly string ArtCacheDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SwishLauncher", "AlbumArt");

    public LocalAudioSource(IEnumerable<string>? extraFolders = null)
    {
        var defaults = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };

        if (extraFolders is not null)
            defaults.AddRange(extraFolders.Where(Directory.Exists));

        _rootFolders = defaults.Where(Directory.Exists).ToList();

        Directory.CreateDirectory(ArtCacheDir);
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
                files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                if (!AudioExtensions.Contains(System.IO.Path.GetExtension(file)))
                    continue;

                yield return BuildEntry(file);
            }
        }
    }

    private static MediaEntry BuildEntry(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var entry = new MediaEntry
        {
            Type      = MediaType.Music,
            FilePath  = filePath,
            DateAdded = fileInfo.CreationTimeUtc,
        };

        try
        {
            // TagLib.File.Create throws CorruptFileException / UnsupportedFormatException
            // for files it can't parse — catch and fall back gracefully.
            using var tagFile = TagLib.File.Create(filePath);
            var tag = tagFile.Tag;

            entry.Title       = tag.Title.NullIfWhiteSpace()
                                ?? SanitiseTitle(System.IO.Path.GetFileNameWithoutExtension(filePath));
            entry.Artist      = tag.FirstPerformer.NullIfWhiteSpace()
                                ?? tag.FirstAlbumArtist.NullIfWhiteSpace();
            entry.Album       = tag.Album.NullIfWhiteSpace();
            entry.Year        = tag.Year > 0 ? (int?)tag.Year : null;
            entry.TrackNumber = tag.Track > 0 ? (int?)tag.Track : null;
            entry.Genre       = tag.FirstGenre.NullIfWhiteSpace();

            // ── Embedded album art → cache to disk ────────────────────────
            // Only extract if we don't already have a cached file (avoids
            // re-writing on every scan for large libraries).
            var artPath = ExtractAlbumArt(tagFile, filePath);
            if (artPath is not null)
            {
                entry.ThumbnailPath = artPath;
                entry.PosterPath    = artPath;
            }
        }
        catch (Exception)
        {
            // Corrupt/unsupported file — derive title from filename only.
            entry.Title = SanitiseTitle(System.IO.Path.GetFileNameWithoutExtension(filePath));
        }

        return entry;
    }

    /// <summary>
    /// Extracts the first embedded picture from <paramref name="tagFile"/> and
    /// saves it as a JPEG in the album-art cache directory.
    /// Returns the cached file path, or null if no picture is embedded.
    /// </summary>
    private static string? ExtractAlbumArt(TagLib.File tagFile, string audioFilePath)
    {
        var pictures = tagFile.Tag.Pictures;
        if (pictures is null || pictures.Length == 0) return null;

        // Use a hash of the audio file path as the cache key so each file
        // gets a stable, unique name without path-separator issues.
        var hash    = Math.Abs(audioFilePath.GetHashCode()).ToString("x8");
        var artPath = System.IO.Path.Combine(ArtCacheDir, $"{hash}.jpg");

        if (!System.IO.File.Exists(artPath))
        {
            try
            {
                System.IO.File.WriteAllBytes(artPath, pictures[0].Data.Data);
            }
            catch
            {
                return null; // Write failed — skip silently
            }
        }

        return artPath;
    }

    private static string SanitiseTitle(string raw)
        => System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(raw.Replace('.', ' ').Replace('_', ' ').Replace('-', ' '));
}

// ── String extension helper ───────────────────────────────────────────────
file static class StringExtensions
{
    public static string? NullIfWhiteSpace(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;
}
