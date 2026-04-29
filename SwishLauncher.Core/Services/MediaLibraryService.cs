using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Services;

public class MediaLibraryService(
    SwishDbContext db,
    IEnumerable<IMediaSource> sources,
    ILogger<MediaLibraryService> logger)
{
    /// <summary>
    /// Scans all registered sources, upserts into SQLite,
    /// and returns the complete library sorted alphabetically.
    /// </summary>
    public async Task<IReadOnlyList<MediaEntry>> ScanAndSyncAsync(
        CancellationToken ct = default)
    {
        var scanTasks = sources
            .Select(s => ScanSourceSafeAsync(s, ct))
            .ToList();

        var results = await Task.WhenAll(scanTasks);
        var scanned = results.SelectMany(x => x).ToList();

        logger.LogInformation(
            "Media scan complete. {Count} entries found across {Sources} sources.",
            scanned.Count, sources.Count());

        await UpsertAsync(scanned, ct);

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

        // Build a set of all currently-valid paths for the delete pass
        var validPaths = byPath
            .Select(e => e.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPaths = await db.Media
            .AsNoTracking()
            .Select(m => new { m.Id, m.FilePath, m.ThumbnailPath })
            .ToListAsync(ct);

        var existingByPath = existingPaths.ToDictionary(
            x => x.FilePath, x => x, StringComparer.OrdinalIgnoreCase);

        // Insert / update
        foreach (var entry in byPath)
        {
            ct.ThrowIfCancellationRequested();

            if (existingByPath.TryGetValue(entry.FilePath, out var existing))
            {
                await db.Media
                    .Where(m => m.Id == existing.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.Title, entry.Title)
                        .SetProperty(m => m.Type, entry.Type)
                        .SetProperty(m => m.ThumbnailPath,
                            entry.ThumbnailPath ?? existing.ThumbnailPath),
                        ct);
            }
            else
            {
                entry.DateAdded = DateTime.UtcNow;
                db.Media.Add(entry);
            }
        }

        await db.SaveChangesAsync(ct);

        // ?? Tombstone pass — delete entries whose files no longer exist ????????
        // Only runs if the scan actually found something, to avoid wiping the DB
        // if all sources fail (e.g. external drive disconnected).
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
