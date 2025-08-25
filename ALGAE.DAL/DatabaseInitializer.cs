using Algae.DAL;

public class DatabaseInitializer
{
    private readonly DatabaseContext _dbContext;

    public DatabaseInitializer(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Initialize()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Starting database initialization");
            using var connection = _dbContext.CreateConnection();
            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Connection created, state: {connection.State}");
            connection.Open();
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Connection opened successfully");
            using var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Games (
                GameId INTEGER PRIMARY KEY AUTOINCREMENT,
                ShortName TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                GameImage TEXT,
                ThemeName TEXT,
                InstallPath TEXT NOT NULL,
                GameWorkingPath TEXT,
                ExecutableName TEXT,
                GameArgs TEXT,
                Version TEXT,
                Publisher TEXT
            );
            CREATE TABLE IF NOT EXISTS Profiles (
                ProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                ProfileName TEXT NOT NULL,
                CommandLineArgs TEXT,
                FOREIGN KEY (GameId) REFERENCES Games (GameId)
            );
            CREATE TABLE IF NOT EXISTS CompanionApps (
                CompanionId INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER,
                Name TEXT NOT NULL,
                Type TEXT NOT NULL,
                PathOrURL TEXT NOT NULL,
                LaunchHelper TEXT,
                Browser TEXT,
                OpenInNewWindow INTEGER,
                FOREIGN KEY (GameId) REFERENCES Games (GameId)
            );
            CREATE TABLE IF NOT EXISTS CompanionProfiles (
                CompanionProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
                ProfileId INTEGER NOT NULL,
                CompanionId INTEGER NOT NULL,
                IsEnabled INTEGER DEFAULT 1,
                FOREIGN KEY (ProfileId) REFERENCES Profiles (ProfileId),
                FOREIGN KEY (CompanionId) REFERENCES CompanionApps (CompanionId)
            );
            CREATE TABLE IF NOT EXISTS GameSignatures (
                GameSignatureId INTEGER PRIMARY KEY AUTOINCREMENT,
                ShortName TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                GameImage TEXT,
                ThemeName TEXT,
                ExecutableName TEXT NOT NULL,
                GameArgs TEXT,
                Version TEXT,
                Publisher TEXT,
                MetaName TEXT,
                MatchName BOOLEAN,
                MatchVersion BOOLEAN,
                MatchPublisher BOOLEAN
            );
            CREATE TABLE IF NOT EXISTS CompanionSignatures (
                CompanionSignatureId INTEGER PRIMARY KEY AUTOINCREMENT,
                GameSignatureId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT,
                ExecutableName TEXT NOT NULL,
                CompanionArgs TEXT,
                Version TEXT,
                Publisher TEXT,
                MetaName TEXT,
                MatchName BOOLEAN,
                MatchVersion BOOLEAN,
                MatchPublisher BOOLEAN,
                FOREIGN KEY (GameSignatureId) REFERENCES GameSignatures (GameSignatureId) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS SearchPaths (
                SearchPathId INTEGER PRIMARY KEY AUTOINCREMENT,
                Path TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS AppSettings (
                SettingsId INTEGER PRIMARY KEY DEFAULT 1,
                LoggingEnabled INTEGER DEFAULT 0,
                LogLevel INTEGER DEFAULT 2,
                LogToFile INTEGER DEFAULT 1,
                LogToConsole INTEGER DEFAULT 0,
                MaxLogFileSizeMB INTEGER DEFAULT 50,
                MaxLogFileCount INTEGER DEFAULT 10,
                Theme TEXT DEFAULT 'Dark',
                StartMinimized INTEGER DEFAULT 0,
                MinimizeToTray INTEGER DEFAULT 0,
                AutoScanOnStartup INTEGER DEFAULT 0,
                ShowNotifications INTEGER DEFAULT 1,
                ConfirmGameDeletion INTEGER DEFAULT 1,
                EnableGameProcessMonitoring INTEGER DEFAULT 1,
                ProcessMonitoringIntervalSeconds INTEGER DEFAULT 5,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            );
        ";
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Executing table creation commands");
            command.ExecuteNonQuery();
            
            // Migration: Add IsEnabled column to CompanionProfiles if it doesn't exist
            try
            {
                command.CommandText = "SELECT IsEnabled FROM CompanionProfiles LIMIT 1";
                command.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: IsEnabled column already exists");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Adding IsEnabled column to CompanionProfiles");
                command.CommandText = "ALTER TABLE CompanionProfiles ADD COLUMN IsEnabled INTEGER DEFAULT 1";
                command.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: IsEnabled column added successfully");
            }
            
            // Migration: Add GameId column to CompanionApps if it doesn't exist
            try
            {
                command.CommandText = "SELECT GameId FROM CompanionApps LIMIT 1";
                command.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: GameId column already exists in CompanionApps");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Adding GameId column to CompanionApps");
                command.CommandText = "ALTER TABLE CompanionApps ADD COLUMN GameId INTEGER";
                command.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: GameId column added successfully to CompanionApps");
            }
            
            // Migration: Migrate KnownGames to GameSignatures if needed
            try
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='KnownGames'";
                var knownGamesExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                
                if (knownGamesExists)
                {
                    System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Migrating KnownGames to GameSignatures");
                    command.CommandText = @"
                        INSERT INTO GameSignatures (
                            ShortName, Name, Description, GameImage, ThemeName, ExecutableName,
                            GameArgs, Version, Publisher, MetaName, MatchName, MatchVersion, MatchPublisher
                        )
                        SELECT 
                            ShortName, Name, Description, GameImage, ThemeName, ExecutableName,
                            GameArgs, Version, Publisher, MetaName, MatchName, MatchVersion, MatchPublisher
                        FROM KnownGames
                        WHERE NOT EXISTS (
                            SELECT 1 FROM GameSignatures gs 
                            WHERE gs.Name = KnownGames.Name AND gs.ExecutableName = KnownGames.ExecutableName
                        )";
                    var migratedGames = command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Migrated {migratedGames} games from KnownGames to GameSignatures");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Error migrating KnownGames: {ex.Message}");
            }
            
            // Migration: Migrate KnownCompanions to CompanionSignatures if needed
            try
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='KnownCompanions'";
                var knownCompanionsExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                
                if (knownCompanionsExists)
                {
                    System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Migrating KnownCompanions to CompanionSignatures");
                    command.CommandText = @"
                        INSERT INTO CompanionSignatures (
                            GameSignatureId, Name, Description, ExecutableName, CompanionArgs,
                            Version, Publisher, MetaName, MatchName, MatchVersion, MatchPublisher
                        )
                        SELECT 
                            kc.KnownGameId, kc.Name, kc.Description, kc.ExecutableName, kc.CompanionArgs,
                            kc.Version, kc.Publisher, kc.MetaName, kc.MatchName, kc.MatchVersion, kc.MatchPublisher
                        FROM KnownCompanions kc
                        WHERE NOT EXISTS (
                            SELECT 1 FROM CompanionSignatures cs 
                            WHERE cs.Name = kc.Name AND cs.ExecutableName = kc.ExecutableName
                        )";
                    var migratedCompanions = command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Migrated {migratedCompanions} companions from KnownCompanions to CompanionSignatures");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseInitializer: Error migrating KnownCompanions: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"DatabaseInitializer ERROR Stack: {ex.StackTrace}");
            throw; // Re-throw to ensure the error is visible
        }
    }
}
