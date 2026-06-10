namespace SwishLauncher.Core.Models;

public enum MediaType { Movie, TvEpisode, Music, Other }

public class MediaEntry
{
    public int       Id            { get; set; }
    public string    Title         { get; set; } = string.Empty;
    public MediaType Type          { get; set; }
    public string    FilePath      { get; set; } = string.Empty;

    // ── Artwork ───────────────────────────────────────────────────────────
    // ThumbnailPath : small card image (poster for video, album art for music)
    // PosterPath    : full-res poster cached locally from TMDB/TagLib
    public string? ThumbnailPath  { get; set; }
    public string? PosterPath     { get; set; }

    // ── Common metadata ───────────────────────────────────────────────────
    public string? Description    { get; set; }
    public int?    Year           { get; set; }
    public string? Genre          { get; set; }
    public double? Rating         { get; set; }   // TMDB vote average, 0–10

    // ── Music-specific (populated by TagLib#) ─────────────────────────────
    public string? Artist         { get; set; }
    public string? Album          { get; set; }
    public int?    TrackNumber    { get; set; }

    // ── TV-specific (populated by TMDB) ───────────────────────────────────
    public string? ShowTitle      { get; set; }   // parent series name
    public int?    SeasonNumber   { get; set; }
    public int?    EpisodeNumber  { get; set; }

    // ── TMDB reference ────────────────────────────────────────────────────
    public int?    TmdbId         { get; set; }

    public DateTime DateAdded     { get; set; } = DateTime.UtcNow;
}
