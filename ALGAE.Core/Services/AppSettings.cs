using System.Text.Json;
using Algae.Core.Services;

namespace Algae.Core.Services
{
    /// <summary>
    /// Main application settings container
    /// </summary>
    public class AppSettings
    {
        public DatabaseSettings Database { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        public GeneralSettings General { get; set; } = new();
        public LoggingSettings AppLogging { get; set; } = new();
        
        // Settings file path
        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AlgaeApp",
            "Settings",
            "appsettings.json"
        );
        
        /// <summary>
        /// Load settings from file or return defaults
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    });
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new AppSettings();
        }
        
        /// <summary>
        /// Save settings to file
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize default database configurations
        /// </summary>
        public void InitializeDefaultDatabases()
        {
            if (!Database.Databases.Any())
            {
                // Add production database
                var prodDbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AlgaeApp",
                    "Database",
                    "ALGAE.db"
                );
                
                var prodDb = new DatabaseConfiguration
                {
                    Name = "Production",
                    FilePath = prodDbPath,
                    IsDefault = true,
                    IsDebugDatabase = false
                };
                Database.AddDatabase(prodDb);
                
                // Add debug database
                var debugDb = new DatabaseConfiguration
                {
                    Name = "Debug",
                    FilePath = "ALGAE-dev.db",
                    IsDefault = false,
                    IsDebugDatabase = true
                };
                Database.AddDatabase(debugDb);
                
                Save();
            }
        }
    }
    
    /// <summary>
    /// General application settings
    /// </summary>
    public class GeneralSettings
    {
        public bool ShowDatabaseSwitchPrompt { get; set; } = true;
        public bool AutoDownloadSignatures { get; set; } = true;
        public bool CheckForUpdatesOnStartup { get; set; } = true;
        public string Theme { get; set; } = "Auto";
        public string Language { get; set; } = "en-US";
    }
    
    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableLogRotation { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 5;
        public bool ShowDebugMessages { get; set; } = false;
    }
}