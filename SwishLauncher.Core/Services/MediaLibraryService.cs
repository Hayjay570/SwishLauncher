using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Services;

public class MediaLibraryService(
    SwishDbContext db,
    IEnumerable<IMediaSource> sources,
    TmdbMetadataService tmdb,
    ILogger<MediaLibraryService> logger)
{
    /// <summary>
    /// Scans all registered sources, upserts into SQLite, runs TMDB enrichment
    /// on any entries that don't yet have metadata, then returns the full library.
    /// </summary>
    public async Task<IReadOnlyList<MediaEntry>> ScanAndSyncAsync(
        CancellationToken ct = default)
    {
        // ── 1. Scan all sources in parallel ───────────────────────────────
        var scanTasks = sources
            .Select(s => ScanSourceSafeAsync(s, ct))
            .ToList();

        var results = await Task.WhenAll(scanTasks);
        var scanned = results.SelectMany(x => x).ToList();

        logger.LogInformation(
            "Media scan complete. {Count} entries found across {Sources} sources.",
            scanned.Count, sources.Count());

        // ── 2. Upsert into SQLite ─────────────────────────────────────────
        await UpsertAsync(scanned, ct);

        // ── 3. TMDB enrichment — only entries missing metadata ────────────
        // Load all un-enriched video entries (Music is handled by TagLib#).
        var toEnrich = await db.Media
            .Where(m => m.TmdbId == null &&
                        (m.Type == MediaType.Movie || m.Type == MediaType.TvEpisode))
            .ToListAsync(ct);

        if (toEnrich.Count > 0)
        {
            logger.LogInformation("TMDB: enriching {Count} entries.", toEnrich.Count);
            // forceRefresh:true — re-enriches bad cached matches and re-detects TV type
            await tmdb.EnrichBatchAsync(toEnrich, forceRefresh: true, ct);

            // Persist enriched fields back to SQLite
            foreach (var entry in toEnrich)
            {
                await db.Media
                    .Where(m => m.Id == entry.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.TmdbId,       entry.TmdbId)
                        .SetProperty(m => m.Title,         entry.Title)
                        .SetProperty(m => m.ShowTitle,     entry.ShowTitle)
                        .SetProperty(m => m.Description,   entry.Description)
                        .SetProperty(m => m.Year,          entry.Year)
                        .SetProperty(m => m.Rating,        entry.Rating)
                        .SetProperty(m => m.PosterPath,    entry.PosterPath)
                        .SetProperty(m => m.ThumbnailPath, entry.ThumbnailPath)
                        .SetProperty(m => m.SeasonNumber,  entry.SeasonNumber)
                        .SetProperty(m => m.EpisodeNumber, entry.EpisodeNumber),
                        ct);
            }

            logger.LogInformation("TMDB enrichment persisted.");
        }

        return await db.Media
            .AsNoTracking()
            .OrderBy(m => m.Title)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns the persisted library without scanning.
    /// Used on page navigation when a fresh scan isn't needed.
    /// </summary>
    public Task<List<MediaEntry>> GetAllAsync(CancellationToken ct = default)
        => db.Media.AsNoTracking().OrderBy(m => m.Title).ToListAsync(ct);

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private async Task<IEnumerable<MediaEntry>> ScanSourceSafeAsync(
        IMediaSource source, CancellationToken ct)
    {
        try
        {
            return await source.ScanAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Media source '{Source}' failed during scan.", source.SourceName);
            return [];
        }
    }

    private async Task UpsertAsync(IEnumerable<MediaEntry> incoming, CancellationToken ct)
    {
        var byPath = incoming
            .GroupBy(e => e.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var validPaths = byPath
            .Select(e => e.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPaths = await db.Media
            .AsNoTracking()
            .Select(m => new { m.Id, m.FilePath, m.ThumbnailPath, m.TmdbId })
            .ToListAsync(ct);

        var existingByPath = existingPaths.ToDictionary(
            x => x.FilePath, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in byPath)
        {
            ct.ThrowIfCancellationRequested();

            if (existingByPath.TryGetValue(entry.FilePath, out var existing))
            {
                await db.Media
                    .Where(m => m.Id == existing.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.Title,  entry.Title)
                        .SetProperty(m => m.Type,   entry.Type)
                        // Preserve thumbnail/poster from enrichment if already set
                        .SetProperty(m => m.ThumbnailPath,
                            entry.ThumbnailPath ?? existing.ThumbnailPath)
                        // Preserve tag-derived fields for audio
                        .SetProperty(m => m.Artist,      entry.Artist)
                        .SetProperty(m => m.Album,       entry.Album)
                        .SetProperty(m => m.TrackNumber, entry.TrackNumber)
                        .SetProperty(m => m.Genre,       entry.Genre ?? null)
                        .SetProperty(m => m.Year,
                            entry.Year ?? null),
                        ct);
            }
            else
            {
                entry.DateAdded = DateTime.UtcNow;
                db.Media.Add(entry);
            }
        }

        await db.SaveChangesAsync(ct);

        // Tombstone — remove entries whose files no longer exist
        if (validPaths.Count > 0)
        {
            var toDelete = existingPaths
                .Where(e => !validPaths.Contains(e.FilePath))
                .Select(e => e.Id)
                .ToList();

            if (toDelete.Count > 0)
                await db.Media
                    .Where(m => toDelete.Contains(m.Id))
                    .ExecuteDeleteAsync(ct);
        }
    }
}
