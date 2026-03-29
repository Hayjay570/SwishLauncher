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
    private Window? _window;

    public App()
    {
        InitializeComponent();
        Services = BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SwishDbContext>()
            .Database.EnsureCreated();

        _window = new MainWindow();
        _window.Activate();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwishLauncher");
        Directory.CreateDirectory(dbFolder);

        services.AddDbContext<SwishDbContext>(o =>
            o.UseSqlite($"Data Source={Path.Combine(dbFolder, "swish.db")}"));

        services.AddTransient<HomeViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}