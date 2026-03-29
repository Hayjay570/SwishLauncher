using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwishLauncher.Core.Models;

public class GameEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string LaunchUri { get; set; } = string.Empty;
    public string? CoverArtPath { get; set; }
    public string? Description { get; set; }
    public double? Rating { get; set; }
    public DateTime? LastPlayed { get; set; }
    public int PlaytimeMinutes { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
