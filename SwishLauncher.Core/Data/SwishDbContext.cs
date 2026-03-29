using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SwishLauncher.Core.Models;

namespace SwishLauncher.Core.Data;

public class SwishDbContext(DbContextOptions<SwishDbContext> options) : DbContext(options)
{
    public DbSet<GameEntry> Games => Set<GameEntry>();
    public DbSet<MediaEntry> Media => Set<MediaEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<GameEntry>().HasIndex(g => g.LaunchUri).IsUnique();
        b.Entity<MediaEntry>().HasIndex(m => m.FilePath).IsUnique();
    }
}
