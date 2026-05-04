using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Services;
using SwishLauncher.Games.Sources;
using SwishLauncher.Media.Sources;
using System;
using System.IO;
using Application = Microsoft.UI.Xaml.Application;

namespace SwishLauncher.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The active top-level Window. Set in OnLaunched.</summary>
    public Window? ActiveWindow { get; private set; }
    private Window? _window;

    public App()
    {
        InitializeComponent();
        Services = BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SwishDbContext>();

        // Create schema if the DB is brand-new.
        db.Database.EnsureCreated();

        // ── Manual column migrations ───────────────────────────────────────
        // EnsureCreated is idempotent: it won't add new columns to existing
        // tables. Use TryAddColumn for every column added after the initial
        // schema — it silently swallows the "duplicate column" error from SQLite.
        TryAddColumn(db, "Games", "IsFavourite", "INTEGER NOT NULL DEFAULT 0");

        _window = new MainWindow();
        ActiveWindow = _window;
        _window.Activate();
    }

    /// <summary>
    /// Runs ALTER TABLE … ADD COLUMN and silently ignores the error SQLite
    /// raises when the column already exists. Safe to call on every launch.
    /// </summary>
    private static void TryAddColumn(SwishDbContext db, string table,
        string column, string columnDef)
    {
        try
        {
            db.Database.ExecuteSqlRaw(
                $"ALTER TABLE {table} ADD COLUMN {column} {columnDef}");
        }
        catch
        {
            // Column already exists — nothing to do.
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // ── Logging ────────────────────────────────────────────────────────
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // ── SQLite / EF Core ───────────────────────────────────────────────
        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwishLauncher");
        Directory.CreateDirectory(dbFolder);

        services.AddDbContext<SwishDbContext>(o =>
            o.UseSqlite($"Data Source={Path.Combine(dbFolder, "swish.db")}"),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        // ── Game sources ───────────────────────────────────────────────────
        services.AddTransient<IGameSource, SteamGameSource>();
        services.AddTransient<IGameSource, XboxGameSource>();
        services.AddTransient<IGameSource, ManualGameSource>();

        // ── Media sources ──────────────────────────────────────────────────
        services.AddTransient<IMediaSource, LocalVideoSource>();
        services.AddTransient<IMediaSource, LocalAudioSource>();

        // ── Domain services ────────────────────────────────────────────────
        services.AddTransient<GameLibraryService>();
        services.AddTransient<MediaLibraryService>();

        // ── ViewModels ─────────────────────────────────────────────────────
        services.AddTransient<HomeViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<GameDetailViewModel>();
        services.AddTransient<MediaDetailViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
