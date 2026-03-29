using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwishLauncher.Core.Models;

public enum MediaType { Movie, TvEpisode, Music, Other }

public class MediaEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}