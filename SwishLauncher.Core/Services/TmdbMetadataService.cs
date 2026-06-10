using Microsoft.Extensions.Logging;
using SwishLauncher.Core.Models;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SwishLauncher.Core.Services;

/// <summary>
/// Enriches a <see cref="MediaEntry"/> (Movie or TvEpisode) with metadata
/// from The Movie Database (TMDB) v3 API.
///
/// Key design decisions:
///   - TV detection happens HERE before any TMDB call, by inspecting the
///     filename for SxxExx patterns. LocalVideoSource always stamps Movie;
///     this service re-stamps TvEpisode when a pattern is found.
///   - EnrichAsync re-enriches entries whose TmdbId was set but whose Title
///     still matches the raw filename (i.e. a previous bad match). Pass
///     forceRefresh=true to unconditionally re-enrich.
/// </summary>
public partial class TmdbMetadataService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";

    private static readonly string PosterCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SwishLauncher", "Posters");

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<TmdbMetadataService> _logger;

    public TmdbMetadataService(
        HttpClient http,
        string apiKey,
        ILogger<TmdbMetadataService> logger)
    {
        _http = http;
        _apiKey = apiKey;
        _logger = logger;
        Directory.CreateDirectory(PosterCacheDir);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enriches a single entry in-place. Skips Music entries (handled by TagLib#).
    /// Re-detects TV episodes from filename before doing any TMDB call.
    /// </summary>
    public async Task<MediaEntry> EnrichAsync(
        MediaEntry entry,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        if (entry.Type == MediaType.Music)
            return entry;

        // ── Step 1: re-detect type from filename ──────────────────────────
        // LocalVideoSource always stamps Movie. Correct to TvEpisode here
        // if the filename contains an SxxExx pattern.
        if (SxxExxRegex().IsMatch(entry.Title))
            entry.Type = MediaType.TvEpisode;

        // ── Step 2: skip if already enriched (unless forced) ──────────────
        if (!forceRefresh && entry.TmdbId.HasValue)
            return entry;

        // ── Step 3: enrich ────────────────────────────────────────────────
        if (entry.Type == MediaType.TvEpisode)
            await EnrichTvAsync(entry, ct);
        else
            await EnrichMovieAsync(entry, ct);

        return entry;
    }

    /// <summary>
    /// Enriches a batch of entries with rate-limit throttling.
    /// Pass forceRefresh=true to re-enrich entries that were previously matched.
    /// </summary>
    public async Task EnrichBatchAsync(
        IEnumerable<MediaEntry> entries,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await EnrichAsync(entry, forceRefresh, ct);
                await Task.Delay(50, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "TMDB enrichment failed for '{Title}' ({Type}).",
                    entry.Title, entry.Type);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Movie enrichment
    // ─────────────────────────────────────────────────────────────────────

    private async Task EnrichMovieAsync(MediaEntry entry, CancellationToken ct)
    {
        var (cleanTitle, year) = ParseTitleAndYear(entry.Title);

        // Skip titles that are too short or purely numeric — these are
        // filenames like "0.10" or "0.25" (screen recordings, temp files)
        // that sanitise to meaningless tokens. TMDB will return random
        // popular results rather than a real match.
        if (!IsSearchableTitle(cleanTitle))
        {
            _logger.LogDebug("TMDB: skipping non-searchable title '{Title}'", cleanTitle);
            return;
        }

        _logger.LogDebug("TMDB movie search: query='{Title}' year={Year}", cleanTitle, year);

        var searchUrl = $"{BaseUrl}/search/movie?api_key={_apiKey}" +
                        $"&query={Uri.EscapeDataString(cleanTitle)}" +
                        (year.HasValue ? $"&year={year}" : "");

        var result = await _http.GetFromJsonAsync<TmdbSearchResult<TmdbMovie>>(searchUrl, ct);

        if (_logger.IsEnabled(LogLevel.Debug))
            foreach (var r in result?.Results ?? [])
                _logger.LogDebug("  candidate: '{Title}' ({Year})",
                    r.Title, ParseYear(r.ReleaseDate));

        var movie = BestMovieMatch(result?.Results, cleanTitle, year);
        if (movie is null) { _logger.LogDebug("TMDB: no match for '{Title}'", cleanTitle); return; }

        _logger.LogDebug("TMDB selected: '{Title}' (id={Id})", movie.Title, movie.Id);

        entry.TmdbId = movie.Id;
        entry.Title = movie.Title ?? entry.Title;
        entry.Description = movie.Overview;
        entry.Year = ParseYear(movie.ReleaseDate);
        entry.Rating = movie.VoteAverage;

        if (!string.IsNullOrWhiteSpace(movie.PosterPath))
        {
            var localPath = await DownloadPosterAsync(movie.PosterPath, ct);
            if (localPath is not null)
            {
                entry.PosterPath = localPath;
                entry.ThumbnailPath = localPath;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // TV enrichment
    // ─────────────────────────────────────────────────────────────────────

    private async Task EnrichTvAsync(MediaEntry entry, CancellationToken ct)
    {
        var (showTitle, season, episode) = ParseTvFilename(entry.Title);

        if (!IsSearchableTitle(showTitle))
        {
            _logger.LogDebug("TMDB: skipping non-searchable TV title '{Title}'", showTitle);
            return;
        }

        _logger.LogDebug("TMDB TV search: show='{Show}' S{S}E{E}", showTitle, season, episode);

        var searchUrl = $"{BaseUrl}/search/tv?api_key={_apiKey}" +
                        $"&query={Uri.EscapeDataString(showTitle)}";

        var result = await _http.GetFromJsonAsync<TmdbSearchResult<TmdbTvShow>>(searchUrl, ct);

        if (_logger.IsEnabled(LogLevel.Debug))
            foreach (var r in result?.Results ?? [])
                _logger.LogDebug("  candidate: '{Name}'", r.Name);

        var show = BestTvMatch(result?.Results, showTitle);
        if (show is null) { _logger.LogDebug("TMDB: no TV match for '{Show}'", showTitle); return; }

        _logger.LogDebug("TMDB TV selected: '{Name}' (id={Id})", show.Name, show.Id);

        entry.TmdbId = show.Id;
        entry.ShowTitle = show.Name ?? showTitle;
        entry.SeasonNumber = season;
        entry.EpisodeNumber = episode;
        entry.Description = show.Overview;
        entry.Year = ParseYear(show.FirstAirDate);
        entry.Rating = show.VoteAverage;

        if (season.HasValue && episode.HasValue)
            entry.Title = $"{entry.ShowTitle} S{season:D2}E{episode:D2}";

        if (!string.IsNullOrWhiteSpace(show.PosterPath))
        {
            var localPath = await DownloadPosterAsync(show.PosterPath, ct);
            if (localPath is not null)
            {
                entry.PosterPath = localPath;
                entry.ThumbnailPath = localPath;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Best-match selection
    // ─────────────────────────────────────────────────────────────────────

    private static TmdbMovie? BestMovieMatch(
        List<TmdbMovie>? candidates, string queryTitle, int? queryYear)
    {
        if (candidates is null || candidates.Count == 0) return null;

        var query = queryTitle.Trim().ToLowerInvariant();

        TmdbMovie? best = null;
        int bestScore = int.MinValue;

        foreach (var c in candidates)
        {
            if (c.Title is null) continue;
            var ct = c.Title.Trim().ToLowerInvariant();
            var score = 0;

            if (ct == query)
                score += 10;
            else if (ct.StartsWith(query + " ") || ct.StartsWith(query + ":"))
                score += 4;
            else if (ct.Contains(query))
                score += 2;

            var resultYear = ParseYear(c.ReleaseDate);
            if (queryYear.HasValue && resultYear.HasValue)
            {
                if (resultYear == queryYear) score += 4;
                else score -= 3;
            }

            if (score > bestScore) { bestScore = score; best = c; }
        }

        return best ?? candidates[0];
    }

    private static TmdbTvShow? BestTvMatch(
        List<TmdbTvShow>? candidates, string queryTitle)
    {
        if (candidates is null || candidates.Count == 0) return null;

        var query = queryTitle.Trim().ToLowerInvariant();

        TmdbTvShow? best = null;
        int bestScore = int.MinValue;

        foreach (var c in candidates)
        {
            if (c.Name is null) continue;
            var ct = c.Name.Trim().ToLowerInvariant();
            var score = 0;

            if (ct == query)
                score += 10;
            else if (ct.StartsWith(query + " ") || ct.StartsWith(query + ":"))
                score += 4;
            else if (ct.Contains(query))
                score += 2;

            if (score > bestScore) { bestScore = score; best = c; }
        }

        return best ?? candidates[0];
    }

    // ─────────────────────────────────────────────────────────────────────
    // Poster download
    // ─────────────────────────────────────────────────────────────────────

    private async Task<string?> DownloadPosterAsync(string tmdbPosterPath, CancellationToken ct)
    {
        var filename = tmdbPosterPath.TrimStart('/');
        var localPath = Path.Combine(PosterCacheDir, filename);

        if (System.IO.File.Exists(localPath))
            return localPath;

        try
        {
            var bytes = await _http.GetByteArrayAsync(ImageBaseUrl + tmdbPosterPath, ct);
            await System.IO.File.WriteAllBytesAsync(localPath, bytes, ct);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download poster '{Path}'.", tmdbPosterPath);
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Filename parsing helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns false for titles that would produce a meaningless TMDB search:
    ///   - Fewer than 2 words after sanitisation
    ///   - Every word is purely numeric (e.g. "0 10", "0 25")
    ///   - Total length under 3 characters
    /// </summary>
    private static bool IsSearchableTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            return false;

        var words = title.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // All words are numeric — e.g. "0 10", "2 5", "1080"
        if (words.All(w => double.TryParse(w, out _)))
            return false;

        return true;
    }

    private static (string Title, int? Year) ParseTitleAndYear(string raw)
    {
        var yearMatch = YearRegex().Match(raw);
        if (!yearMatch.Success)
            return (raw.Trim(), null);

        var year = int.Parse(yearMatch.Value.Trim('(', ')'));
        var title = raw[..yearMatch.Index].Trim();
        return (title, year);
    }

    private static (string ShowTitle, int? Season, int? Episode) ParseTvFilename(string raw)
    {
        var m = SxxExxRegex().Match(raw);
        if (!m.Success)
            return (raw.Trim(), null, null);

        var showTitle = raw[..m.Index].Trim(' ', '-', '_', '.');
        var season = int.Parse(m.Groups["s"].Value);
        var episode = int.Parse(m.Groups["e"].Value);
        return (showTitle, season, episode);
    }

    private static int? ParseYear(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        return dateStr.Length >= 4 && int.TryParse(dateStr[..4], out var y) ? y : null;
    }

    [GeneratedRegex(@"\(?(19|20)\d{2}\)?")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"[Ss](?<s>\d{1,2})[Ee](?<e>\d{1,2})")]
    private static partial Regex SxxExxRegex();

    // ─────────────────────────────────────────────────────────────────────
    // TMDB JSON models
    // ─────────────────────────────────────────────────────────────────────

    private sealed class TmdbSearchResult<T>
    {
        [JsonPropertyName("results")]
        public List<T>? Results { get; init; }
    }

    private sealed class TmdbMovie
    {
        [JsonPropertyName("id")] public int? Id { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("overview")] public string? Overview { get; init; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; init; }
        [JsonPropertyName("vote_average")] public double VoteAverage { get; init; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; init; }
    }

    private sealed class TmdbTvShow
    {
        [JsonPropertyName("id")] public int? Id { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("overview")] public string? Overview { get; init; }
        [JsonPropertyName("first_air_date")] public string? FirstAirDate { get; init; }
        [JsonPropertyName("vote_average")] public double VoteAverage { get; init; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; init; }
    }
}