using Algae.Core.Services;
using Algae.DAL;
using ALGAE.DAL.Repositories;
using ALGAE.Views;

using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog.Events;

using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ALGAE;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;
    [STAThread]
    private static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    private static async Task MainAsync(string[] args)
    {
        try
        {
            using IHost host = CreateHostBuilder(args).Build();
            await host.StartAsync().ConfigureAwait(true);

            // Initialize database management and get the active database
            var databaseService = host.Services.GetRequiredService<ALGAE.Services.IDatabaseManagementService>();
            var activeDatabasePath = await databaseService.GetActiveDatabasePathAsync();
            
            // Initialize the database with the selected path
            var context = new DatabaseContext(activeDatabasePath);
            var initializer = new DatabaseInitializer(context);
            initializer.Initialize();

        App app = new();
        app.Services = host.Services; // Initialize the Services property
        app.InitializeComponent();
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        
        // After the main window is shown, check if signatures are needed
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Give UI time to load
            var currentDb = await databaseService.GetCurrentDatabaseAsync();
            if (currentDb != null && currentDb.Exists)
            {
                var hasSignatures = await databaseService.HasSignaturesAsync(currentDb.FilePath);
                var settings = await databaseService.LoadSettingsAsync();
                
                if (!hasSignatures && settings.General.AutoDownloadSignatures)
                {
                    // Show signature download prompt on UI thread
                    app.Dispatcher.Invoke(async () =>
                    {
                        await ShowSignatureDownloadPromptAsync(currentDb.Name, host.Services);
                    });
                }
            }
        });
        
        // Set up proper shutdown handling
        app.Exit += (sender, e) => {
            try
            {
                // Dispose of singleton services that might have background timers
                var gameMonitorService = host.Services.GetService<ALGAE.Services.IGameProcessMonitorService>();
                if (gameMonitorService is IDisposable disposableMonitor)
                {
                    disposableMonitor.Dispose();
                }
                
                System.Diagnostics.Debug.WriteLine("Application shutdown: Services disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during application shutdown: {ex.Message}");
            }
        };
        
        app.Run();

        await host.StopAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Application startup error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // For critical startup errors, we still need to use MessageBox as the UI isn't initialized
            System.Windows.MessageBox.Show(
                $"ALGAE failed to start.\n\nError: {ex.Message}\n\nPlease check the log files for more details.", 
                "ALGAE Startup Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder)
            => configurationBuilder.AddUserSecrets(typeof(App).Assembly))
        .ConfigureServices((hostContext, services) =>
        {
            // Register LogService
            services.AddSingleton(_ =>
            {
                var logSettings = new LogSettings
                {
                    LoggingLevel = Serilog.Events.LogEventLevel.Information,
                    LogFilePath = GetLogFilePath() // Dynamically determine the log file path
                };
                return logSettings;
            });

            services.AddSingleton<MainWindow>();
            services.AddSingleton<LauncherWindow>(provider => 
                new LauncherWindow(provider.GetRequiredService<ALGAE.ViewModels.LauncherViewModel>()));
            services.AddSingleton<ALGAE.ViewModels.MainViewModel>();
            services.AddTransient<ALGAE.ViewModels.LogViewerViewModel>();
            services.AddTransient<ALGAE.Views.LogViewerWindow>();
            services.AddTransient<ALGAE.ViewModels.GamesViewModel>(provider => 
                new ALGAE.ViewModels.GamesViewModel(
                    provider.GetRequiredService<IGameRepository>(),
                    provider.GetRequiredService<ALGAE.Services.INotificationService>(),
                    provider.GetRequiredService<ALGAE.Services.IGameProcessMonitorService>(),
                    provider,
                    provider.GetRequiredService<IProfilesRepository>(),
                    provider.GetRequiredService<ALGAE.Services.ICompanionLaunchService>(),
                    provider.GetRequiredService<ALGAE.Services.IGameLaunchService>()
                ));
            services.AddTransient<ALGAE.ViewModels.HomeViewModel>();
            services.AddTransient<ALGAE.ViewModels.GameDetailViewModel>(provider =>
                new ALGAE.ViewModels.GameDetailViewModel(
                    provider.GetRequiredService<IGameRepository>(),
                    provider.GetRequiredService<IProfilesRepository>(),
                    provider.GetRequiredService<ICompanionRepository>(),
                    provider.GetRequiredService<ICompanionProfileRepository>(),
                    provider.GetRequiredService<ALGAE.Services.INotificationService>(),
                    provider,
                    provider.GetRequiredService<ALGAE.Services.ICompanionLaunchService>(),
                    provider.GetRequiredService<ALGAE.Services.IGameProcessMonitorService>(),
                    provider.GetRequiredService<ALGAE.Services.IGameLaunchService>()
                ));
            services.AddTransient<ALGAE.ViewModels.LauncherViewModel>(provider =>
                new ALGAE.ViewModels.LauncherViewModel(
                    provider.GetRequiredService<ALGAE.Services.IGameProcessMonitorService>(),
                    provider.GetRequiredService<ALGAE.Services.INotificationService>(),
                    provider.GetRequiredService<ALGAE.Services.IGameLaunchService>(),
                    provider.GetRequiredService<IProfilesRepository>(),
                    provider.GetRequiredService<ICompanionRepository>(),
                    provider.GetRequiredService<ICompanionProfileRepository>(),
                    provider.GetRequiredService<ALGAE.Services.ICompanionLaunchService>(),
                    provider.GetRequiredService<Dispatcher>()
                ));
            
            // Register companion management ViewModels
            services.AddTransient<ALGAE.ViewModels.CompanionsViewModel>();
            services.AddTransient<ALGAE.ViewModels.AddEditCompanionViewModel>();
            
            // Register settings ViewModels
            services.AddTransient<ALGAE.ViewModels.SettingsViewModel>();
            
            // Register signature management ViewModels
            services.AddTransient<ALGAE.ViewModels.GameSignaturesViewModel>(provider =>
                new ALGAE.ViewModels.GameSignaturesViewModel(
                    provider.GetRequiredService<ALGAE.DAL.Repositories.IGameSignatureRepository>(),
                    provider.GetRequiredService<ALGAE.Services.IGameSignatureService>()
                ));
            
            // Register notification service
            services.AddSingleton<ALGAE.Services.INotificationService, ALGAE.Services.NotificationService>();
            
            // Register logging service
            services.AddSingleton<ALGAE.Services.ILoggingService, ALGAE.Services.LoggingService>();
            
            // Register game process monitoring service
            services.AddSingleton<ALGAE.Services.IGameProcessMonitorService, ALGAE.Services.GameProcessMonitorService>();

            // Register companion launch service
            services.AddSingleton<ALGAE.Services.ICompanionLaunchService, ALGAE.Services.CompanionLaunchService>();

            // Register game launch service
            services.AddSingleton<ALGAE.Services.IGameLaunchService, ALGAE.Services.GameLaunchService>();

            // Register game detection service
            services.AddTransient<ALGAE.Services.IGameDetectionService, ALGAE.Services.GameDetectionService>();
            
            // Register database management service
            services.AddSingleton<ALGAE.Services.IDatabaseManagementService, ALGAE.Services.DatabaseManagementService>();
            
            // Register game signature service
            services.AddTransient<ALGAE.Services.IGameSignatureService, ALGAE.Services.GameSignatureService>();
            
            // Register repositories
            services.AddTransient<IGameRepository, GameRepository>();
            services.AddTransient<ILaunchHistoryRepository, LaunchHistoryRepository>();
            services.AddTransient<IProfilesRepository, ProfilesRepository>();
            services.AddTransient<ICompanionRepository, CompanionRepository>();
            services.AddTransient<ICompanionProfileRepository, CompanionProfileRepository>();
            services.AddTransient<ICompanionSignatureRepository, CompanionSignatureRepository>();
            services.AddTransient<ISearchPathRepository, SearchPathRepository>();
            services.AddTransient<IGameSignatureRepository, GameSignatureRepository>();

            services.AddSingleton<WeakReferenceMessenger>();
            services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>());

            services.AddSingleton(_ => Current.Dispatcher);

            services.AddTransient<ISnackbarMessageQueue>(provider =>
            {
                Dispatcher dispatcher = provider.GetRequiredService<Dispatcher>();
                return new SnackbarMessageQueue(TimeSpan.FromSeconds(3.0), dispatcher);
            });
            services.AddSingleton<DatabaseContext>(provider =>
            {
                // This will be updated during startup to use the proper database
                return new DatabaseContext(GetDatabasePath());
            });
            services.AddTransient<DatabaseInitializer>();
        });

    public static string GetDatabasePath()
    {
        // Check for environment variable override first
        string? envDbPath = Environment.GetEnvironmentVariable("ALGAE_DB_PATH");
        if (!string.IsNullOrEmpty(envDbPath))
        {
            string customDbPath = $"Data Source={envDbPath}";
            System.Diagnostics.Debug.WriteLine($"Using custom database from environment variable: {customDbPath}");
            return customDbPath;
        }
        
        // Check for force production mode
        string? forceProduction = Environment.GetEnvironmentVariable("ALGAE_FORCE_PRODUCTION");
        bool isForceProduction = !string.IsNullOrEmpty(forceProduction) && 
                                 (forceProduction.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                                  forceProduction.Equals("1"));
        
        if (!isForceProduction && IsDevelopmentEnvironment())
        {
            // Development database in the local project folder
            string devDbPath = "Data Source=ALGAE-dev.db";
            System.Diagnostics.Debug.WriteLine($"Using development database: {devDbPath}");
            return devDbPath;
        }
        else
        {
            // Production database in AppData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dbFolder = Path.Combine(appDataPath, "AlgaeApp", "Database");
            Directory.CreateDirectory(dbFolder); // Ensure directory exists
            string prodDbPath = $"Data Source={Path.Combine(dbFolder, "ALGAE.db")}";
            string reason = isForceProduction ? "(forced by environment variable)" : "(detected as production)";
            System.Diagnostics.Debug.WriteLine($"Using production database {reason}: {prodDbPath}");
            return prodDbPath;
        }
    }
    public static string GetLogFilePath()
    {
        if (IsDevelopmentEnvironment())
        {
            // Use a local path for debugging
            string devLogPath = "logs/development-log.txt";
            System.Diagnostics.Debug.WriteLine($"Using development log path: {devLogPath}");
            return devLogPath;
        }
        else
        {
            // Use the AppData folder for production
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logFolder = Path.Combine(appDataPath, "AlgaeApp", "Logs");
            Directory.CreateDirectory(logFolder); // Ensure the directory exists
            string prodLogPath = Path.Combine(logFolder, "ALGAE-Log.txt");
            System.Diagnostics.Debug.WriteLine($"Using production log path: {prodLogPath}");
            return prodLogPath;
        }
    }

    /// <summary>
    /// Improved detection for development environment
    /// </summary>
    public static bool IsDevelopmentEnvironment()
    {
        // First check if we're in DEBUG mode
#if DEBUG
        return true;
#endif
        
        // Check if we're running from a typical development directory structure
        string currentDirectory = Environment.CurrentDirectory;
        string executablePath = Environment.ProcessPath ?? "";
        
        // Check for common development indicators
        bool hasSourceStructure = currentDirectory.Contains("\\source\\repos", StringComparison.OrdinalIgnoreCase) ||
                                 currentDirectory.Contains("/source/repos", StringComparison.OrdinalIgnoreCase) ||
                                 currentDirectory.Contains("\\src\\", StringComparison.OrdinalIgnoreCase) ||
                                 currentDirectory.Contains("/src/", StringComparison.OrdinalIgnoreCase);
        
        bool hasBinDebugPath = executablePath.Contains("\\bin\\Debug\\", StringComparison.OrdinalIgnoreCase) ||
                              executablePath.Contains("/bin/Debug/", StringComparison.OrdinalIgnoreCase);
        
        bool hasDevDbFile = File.Exists(Path.Combine(currentDirectory, "ALGAE-dev.db"));
        
        // Check for development tools in process list (optional)
        bool hasDevTools = false;
        try
        {
            var processes = System.Diagnostics.Process.GetProcesses();
            hasDevTools = processes.Any(p => 
                p.ProcessName.Contains("devenv", StringComparison.OrdinalIgnoreCase) || // Visual Studio
                p.ProcessName.Contains("Code", StringComparison.OrdinalIgnoreCase) ||    // VS Code
                p.ProcessName.Contains("rider", StringComparison.OrdinalIgnoreCase));    // JetBrains Rider
        }
        catch
        {
            // Ignore process enumeration errors
        }
        
        bool isDevelopment = hasSourceStructure || hasBinDebugPath || hasDevDbFile || hasDevTools;
        
        System.Diagnostics.Debug.WriteLine($"Development Environment Detection:");
        System.Diagnostics.Debug.WriteLine($"  Current Directory: {currentDirectory}");
        System.Diagnostics.Debug.WriteLine($"  Executable Path: {executablePath}");
        System.Diagnostics.Debug.WriteLine($"  Has Source Structure: {hasSourceStructure}");
        System.Diagnostics.Debug.WriteLine($"  Has Bin/Debug Path: {hasBinDebugPath}");
        System.Diagnostics.Debug.WriteLine($"  Has Dev DB File: {hasDevDbFile}");
        System.Diagnostics.Debug.WriteLine($"  Has Dev Tools: {hasDevTools}");
        System.Diagnostics.Debug.WriteLine($"  Final Decision: {(isDevelopment ? "DEVELOPMENT" : "PRODUCTION")}");
        
        return isDevelopment;
    }
    
    private static async Task ShowSignatureDownloadPromptAsync(string databaseName, IServiceProvider services)
    {
        try
        {
            var dialog = new ALGAE.Views.SignatureDownloadPromptDialog(databaseName);
            var result = dialog.ShowDialog();
            
            if (result == true && dialog.Result == ALGAE.Views.SignatureDownloadPromptDialogResult.Download)
            {
                // Download signatures
                var gameSignatureService = services.GetService<ALGAE.Services.IGameSignatureService>();
                if (gameSignatureService != null)
                {
                    try
                    {
                        var signatures = await gameSignatureService.DownloadLatestSignaturesAsync();
                        var gameSignatureRepository = services.GetRequiredService<ALGAE.DAL.Repositories.IGameSignatureRepository>();
                        
                        foreach (var signature in signatures)
                        {
                            signature.GameSignatureId = 0; // Reset ID for new import
                            await gameSignatureRepository.AddAsync(signature);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Successfully downloaded and imported {signatures.Count()} signatures");
                        MessageBox.Show($"Successfully downloaded and imported {signatures.Count()} game signatures!", 
                            "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error downloading signatures: {ex.Message}");
                        MessageBox.Show($"Error downloading signatures: {ex.Message}", "Download Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            
            // Update settings if needed
            if (dialog.RememberChoice || dialog.AutoDownloadForNewDatabases)
            {
                var databaseService = services.GetRequiredService<ALGAE.Services.IDatabaseManagementService>();
                var settings = await databaseService.LoadSettingsAsync();
                
                if (dialog.RememberChoice)
                {
                    settings.General.AutoDownloadSignatures = dialog.Result == ALGAE.Views.SignatureDownloadPromptDialogResult.Download;
                }
                
                await databaseService.SaveSettingsAsync(settings);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing signature download prompt: {ex.Message}");
        }
    }
    
    [Obsolete("Use IsDevelopmentEnvironment() instead for better detection")]
    public static bool IsRunningInVisualStudio()
    {
#if DEBUG
        return true; // Debug mode
#else
        return false; // Release mode
#endif
    }
}
