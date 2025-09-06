using System.IO;
using Algae.DAL;
using ALGAE.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Algae.Core.Services;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for managing database operations, switching, and initialization
    /// </summary>
    public interface IDatabaseManagementService
    {
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
        Task<string> GetActiveDatabasePathAsync();
        Task<DatabaseConfiguration?> GetCurrentDatabaseAsync();
        Task SwitchDatabaseAsync(string databaseId);
        Task<DatabaseConfiguration> CreateNewDatabaseAsync(string name, string filePath);
        Task<bool> ClearDatabaseAsync(string databaseId);
        Task<bool> DeleteDatabaseAsync(string databaseId);
        Task<bool> TestDatabaseConnectionAsync(string databasePath);
        Task<bool> HasSignaturesAsync(string databasePath);
        Task<int> GetGameCountAsync(string databasePath);
        Task<int> GetCompanionCountAsync(string databasePath);
        event EventHandler<DatabaseSwitchedEventArgs>? DatabaseSwitched;
    }
    
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private AppSettings _currentSettings;
        
        public event EventHandler<DatabaseSwitchedEventArgs>? DatabaseSwitched;
        
        public DatabaseManagementService()
        {
            _currentSettings = AppSettings.Load();
            _currentSettings.InitializeDefaultDatabases();
        }
        
        public async Task<AppSettings> LoadSettingsAsync()
        {
            return await Task.FromResult(AppSettings.Load());
        }
        
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            _currentSettings = settings;
            await Task.Run(() => settings.Save());
        }
        
        public async Task<string> GetActiveDatabasePathAsync()
        {
            var currentDb = await GetCurrentDatabaseAsync();
            if (currentDb != null)
            {
                return currentDb.ConnectionString;
            }
            
            // Fall back to application logic
            return GetDefaultDatabasePath();
        }
        
        public async Task<DatabaseConfiguration?> GetCurrentDatabaseAsync()
        {
            var settings = await LoadSettingsAsync();
            
            // Try to get current database
            var current = settings.Database.GetCurrentDatabase();
            if (current != null && current.Exists)
            {
                return current;
            }
            
            // Check for debug vs production mode
            if (IsDebugMode())
            {
                var debugDb = settings.Database.GetDebugDatabase();
                if (debugDb != null)
                {
                    await SetCurrentDatabaseAsync(debugDb.Id);
                    return debugDb;
                }
            }
            else
            {
                // Production mode - try default first, then most recent
                var defaultDb = settings.Database.GetDefaultDatabase();
                if (defaultDb != null && defaultDb.Exists)
                {
                    await SetCurrentDatabaseAsync(defaultDb.Id);
                    return defaultDb;
                }
                
                var recentDb = settings.Database.GetMostRecentDatabase();
                if (recentDb != null)
                {
                    await SetCurrentDatabaseAsync(recentDb.Id);
                    return recentDb;
                }
            }
            
            // If no database found, create default
            return await CreateDefaultDatabaseAsync();
        }
        
        public async Task SwitchDatabaseAsync(string databaseId)
        {
            var settings = await LoadSettingsAsync();
            var database = settings.Database.Databases.FirstOrDefault(db => db.Id == databaseId);
            
            if (database == null)
            {
                throw new ArgumentException($"Database with ID '{databaseId}' not found.");
            }
            
            if (!database.Exists)
            {
                throw new FileNotFoundException($"Database file not found: {database.FilePath}");
            }
            
            var oldDatabase = settings.Database.GetCurrentDatabase();
            
            // Update current database
            await SetCurrentDatabaseAsync(databaseId);
            
            // Raise event for UI updates
            DatabaseSwitched?.Invoke(this, new DatabaseSwitchedEventArgs(oldDatabase, database));
        }
        
        public async Task<DatabaseConfiguration> CreateNewDatabaseAsync(string name, string filePath)
        {
            var settings = await LoadSettingsAsync();
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create database file if it doesn't exist
            if (!File.Exists(filePath))
            {
                // Create empty database file and initialize schema
                var connectionString = $"Data Source={filePath}";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                // Initialize database schema using DatabaseInitializer logic
                var initializer = new DatabaseInitializer(new DatabaseContext(connectionString));
                initializer.Initialize();
            }
            
            // Create database configuration
            var newDb = new DatabaseConfiguration
            {
                Name = name,
                FilePath = filePath,
                IsDefault = false,
                IsDebugDatabase = false
            };
            
            settings.Database.AddDatabase(newDb);
            await SaveSettingsAsync(settings);
            
            return newDb;
        }
        
        public async Task<bool> ClearDatabaseAsync(string databaseId)
        {
            var settings = await LoadSettingsAsync();
            var database = settings.Database.Databases.FirstOrDefault(db => db.Id == databaseId);
            
            if (database == null || !database.Exists)
            {
                return false;
            }
            
            try
            {
                var connectionString = database.ConnectionString;
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                // Clear all data from tables (keeping schema)
                var clearCommands = new[]
                {
                    "DELETE FROM CompanionProfiles;",
                    "DELETE FROM LaunchHistory;", 
                    "DELETE FROM Companions;",
                    "DELETE FROM Profiles;",
                    "DELETE FROM Games;",
                    "DELETE FROM GameSignatures;",
                    "DELETE FROM CompanionSignatures;",
                    "DELETE FROM SearchPaths;",
                    "VACUUM;" // Reclaim space
                };
                
                foreach (var sql in clearCommands)
                {
                    using var command = new SqliteCommand(sql, connection);
                    await command.ExecuteNonQueryAsync();
                }
                
                // Update database size
                database.UpdateSize();
                await SaveSettingsAsync(settings);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing database: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> DeleteDatabaseAsync(string databaseId)
        {
            var settings = await LoadSettingsAsync();
            var database = settings.Database.Databases.FirstOrDefault(db => db.Id == databaseId);
            
            if (database == null)
            {
                return false;
            }
            
            try
            {
                // Delete file if it exists
                if (database.Exists)
                {
                    File.Delete(database.FilePath);
                }
                
                // Remove from settings
                settings.Database.RemoveDatabase(databaseId);
                await SaveSettingsAsync(settings);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting database: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> TestDatabaseConnectionAsync(string databasePath)
        {
            try
            {
                var connectionString = $"Data Source={databasePath}";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                // Test basic query
                using var command = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' LIMIT 1;", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<bool> HasSignaturesAsync(string databasePath)
        {
            try
            {
                var connectionString = $"Data Source={databasePath}";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = new SqliteCommand("SELECT COUNT(*) FROM GameSignatures;", connection);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<int> GetGameCountAsync(string databasePath)
        {
            try
            {
                var connectionString = $"Data Source={databasePath}";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = new SqliteCommand("SELECT COUNT(*) FROM Games;", connection);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch
            {
                return 0;
            }
        }
        
        public async Task<int> GetCompanionCountAsync(string databasePath)
        {
            try
            {
                var connectionString = $"Data Source={databasePath}";
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = new SqliteCommand("SELECT COUNT(*) FROM Companions;", connection);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch
            {
                return 0;
            }
        }
        
        private async Task<DatabaseConfiguration> CreateDefaultDatabaseAsync()
        {
            var settings = await LoadSettingsAsync();
            
            if (IsDebugMode())
            {
                // Create debug database
                return await CreateNewDatabaseAsync("Debug", "ALGAE-dev.db");
            }
            else
            {
                // Create production database
                var prodPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AlgaeApp",
                    "Database",
                    "ALGAE.db"
                );
                
                var prodDb = await CreateNewDatabaseAsync("Production", prodPath);
                
                // Set as default
                settings.Database.SetDefaultDatabase(prodDb.Id);
                await SaveSettingsAsync(settings);
                
                return prodDb;
            }
        }
        
        private async Task SetCurrentDatabaseAsync(string databaseId)
        {
            var settings = await LoadSettingsAsync();
            settings.Database.SetCurrentDatabase(databaseId);
            await SaveSettingsAsync(settings);
        }
        
        private static bool IsDebugMode()
        {
#if DEBUG
            return true;
#else
            // Check for environment variable override
            string? forceProduction = Environment.GetEnvironmentVariable("ALGAE_FORCE_PRODUCTION");
            bool isForceProduction = !string.IsNullOrEmpty(forceProduction) && 
                                     (forceProduction.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                                      forceProduction.Equals("1"));
            
            if (isForceProduction)
            {
                return false;
            }
            
            // Check for common development indicators
            string currentDirectory = Environment.CurrentDirectory;
            string executablePath = Environment.ProcessPath ?? "";
            
            bool hasSourceStructure = currentDirectory.Contains("\\source\\repos", StringComparison.OrdinalIgnoreCase) ||
                                     currentDirectory.Contains("/source/repos", StringComparison.OrdinalIgnoreCase) ||
                                     currentDirectory.Contains("\\src\\", StringComparison.OrdinalIgnoreCase) ||
                                     currentDirectory.Contains("/src/", StringComparison.OrdinalIgnoreCase);
            
            bool hasBinDebugPath = executablePath.Contains("\\bin\\Debug\\", StringComparison.OrdinalIgnoreCase) ||
                                  executablePath.Contains("/bin/Debug/", StringComparison.OrdinalIgnoreCase);
            
            bool hasDevDbFile = File.Exists(Path.Combine(currentDirectory, "ALGAE-dev.db"));
            
            return hasSourceStructure || hasBinDebugPath || hasDevDbFile;
#endif
        }
        
        private static string GetDefaultDatabasePath()
        {
            // Check for environment variable override first
            string? envDbPath = Environment.GetEnvironmentVariable("ALGAE_DB_PATH");
            if (!string.IsNullOrEmpty(envDbPath))
            {
                return $"Data Source={envDbPath}";
            }
            
            if (IsDebugMode())
            {
                return "Data Source=ALGAE-dev.db";
            }
            else
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbFolder = Path.Combine(appDataPath, "AlgaeApp", "Database");
                Directory.CreateDirectory(dbFolder);
                return $"Data Source={Path.Combine(dbFolder, "ALGAE.db")}";
            }
        }
    }
    
    /// <summary>
    /// Event args for database switching
    /// </summary>
    public class DatabaseSwitchedEventArgs : EventArgs
    {
        public DatabaseConfiguration? OldDatabase { get; }
        public DatabaseConfiguration NewDatabase { get; }
        
        public DatabaseSwitchedEventArgs(DatabaseConfiguration? oldDatabase, DatabaseConfiguration newDatabase)
        {
            OldDatabase = oldDatabase;
            NewDatabase = newDatabase;
        }
    }
}