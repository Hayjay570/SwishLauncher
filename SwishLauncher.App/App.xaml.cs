using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Data;
using SwishLauncher.Core.Interfaces;
using SwishLauncher.Core.Services;
using SwishLauncher.Games.Sources;
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
        // EnsureCreated is quick for an empty DB; for migrations swap to Migrate()
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SwishDbContext>()
            .Database.EnsureCreated();

        _window = new MainWindow();
        ActiveWindow = _window;
        _window.Activate();
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

        // ── Domain services ────────────────────────────────────────────────
        services.AddTransient<GameLibraryService>();

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
