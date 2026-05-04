using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Services;

public class GameLibraryService(
    SwishDbContext db,
    IEnumerable<IGameSource> sources,
    ILogger<GameLibraryService> logger)
{
    /// <summary>
    /// Scans all registered sources, upserts new entries into SQLite,
    /// and returns the complete library sorted alphabetically.
    /// </summary>
    public async Task<IReadOnlyList<GameEntry>> ScanAndSyncAsync(
        CancellationToken ct = default)
    {
        var scanTasks = sources
            .Select(s => ScanSourceSafeAsync(s, ct))
            .ToList();

        var results = await Task.WhenAll(scanTasks);
        var scanned = results.SelectMany(x => x).ToList();

        logger.LogInformation(
            "Scan complete. {Count} entries found across {Sources} sources.",
            scanned.Count, sources.Count());

        await UpsertAsync(scanned, ct);

        return await db.Games
            .AsNoTracking()
            .OrderBy(g => g.Title)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns the persisted library without scanning.
    /// Used on page navigation when a fresh scan isn't needed.
    /// </summary>
    public Task<List<GameEntry>> GetAllAsync(CancellationToken ct = default)
        => db.Games.AsNoTracking().OrderBy(g => g.Title).ToListAsync(ct);

    private async Task<IEnumerable<GameEntry>> ScanSourceSafeAsync(
        IGameSource source, CancellationToken ct)
    {
        try
        {
            var entries = await source.ScanAsync(ct);
            return entries;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Source '{Source}' failed during scan.", source.SourceName);
            return [];
        }
    }

    private async Task UpsertAsync(IEnumerable<GameEntry> incoming, CancellationToken ct)
    {
        var byUri = incoming
            .GroupBy(e => e.LaunchUri)
            .Select(g => g.First())
            .ToList();

        if (byUri.Count == 0) return;

        // ── ADD THIS ──────────────────────────────────────────────────────────
        var validUris = byUri
            .Select(e => e.LaunchUri)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        // ─────────────────────────────────────────────────────────────────────

        var existingUris = await db.Games
            .AsNoTracking()
            .Select(g => new { g.Id, g.LaunchUri, g.CoverArtPath })
            .ToListAsync(ct);

        var existingByUri = existingUris.ToDictionary(x => x.LaunchUri, x => x);

        foreach (var entry in byUri)
        {
            ct.ThrowIfCancellationRequested();

            if (existingByUri.TryGetValue(entry.LaunchUri, out var existing))
            {
                await db.Games
                    .Where(g => g.Id == existing.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(g => g.Title, entry.Title)
                        .SetProperty(g => g.Platform, entry.Platform)
                        .SetProperty(g => g.CoverArtPath,
                            entry.CoverArtPath ?? existing.CoverArtPath),
                        ct);
            }
            else
            {
                entry.DateAdded = DateTime.UtcNow;
                db.Games.Add(entry);
            }
        }

        await db.SaveChangesAsync(ct);

        // ── ADD THIS ──────────────────────────────────────────────────────────
        var toDelete = existingUris
            .Where(e => !validUris.Contains(e.LaunchUri))
            .Select(e => e.Id)
            .ToList();

        if (toDelete.Count > 0)
            await db.Games
                .Where(g => toDelete.Contains(g.Id))
                .ExecuteDeleteAsync(ct);
        // ─────────────────────────────────────────────────────────────────────
    }

    // ── Home screen queries ────────────────────────────────────────────────

    /// <summary>Top 5 most recently added games (for the Featured strip).</summary>
    public Task<List<GameEntry>> GetFeaturedAsync(CancellationToken ct = default)
        => db.Games.AsNoTracking()
              .OrderByDescending(g => g.DateAdded)
              .Take(5)
              .ToListAsync(ct);

    /// <summary>Games with a LastPlayed date, ordered most-recent first.</summary>
    public Task<List<GameEntry>> GetRecentlyPlayedAsync(CancellationToken ct = default)
        => db.Games.AsNoTracking()
              .Where(g => g.LastPlayed != null)
              .OrderByDescending(g => g.LastPlayed)
              .ToListAsync(ct);

    /// <summary>Favourited games ordered by total playtime descending.</summary>
    public Task<List<GameEntry>> GetFavouritesAsync(CancellationToken ct = default)
        => db.Games.AsNoTracking()
              .Where(g => g.IsFavourite)
              .OrderByDescending(g => g.PlaytimeMinutes)
              .ToListAsync(ct);

}
