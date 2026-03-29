using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using SwishLauncher.App.ViewModels;
using SwishLauncher.Core.Data;
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
            // Scoped lifetime pairs well with Transient VMs that take the context
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        // ── ViewModels ─────────────────────────────────────────────────────
        // Transient: a fresh VM (and fresh DbContext) on every page navigation.
        // This avoids stale EF Core change-tracker state between visits.
        services.AddTransient<HomeViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<GameDetailViewModel>();
        services.AddTransient<MediaDetailViewModel>();

        // Singleton: settings state (chosen theme, toggles) must survive tab switches.
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
