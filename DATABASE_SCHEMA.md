# Database Schema Documentation

This document describes the database structure and relationships in ALGAE.

## Overview

ALGAE uses SQLite with Entity Framework Core for data persistence. The database consists of several main areas:
- **Game Library Management** - Core game and profile data
- **Companion System** - Application companions and associations  
- **Signature Database** - Game and companion detection signatures
- **Application Settings** - User preferences and configuration

## Database Tables

### Core Game Library

#### Games
Primary table storing game information.

```sql
CREATE TABLE Games (
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
```

**Key Fields:**
- `GameId` - Primary key, auto-increment
- `ShortName` - Abbreviated name for UI display
- `Name` - Full game name
- `InstallPath` - Game installation directory
- `ExecutableName` - Main executable file
- `GameArgs` - Default command line arguments

#### Profiles  
Game launch profiles with custom settings.

```sql
CREATE TABLE Profiles (
    ProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
    GameId INTEGER NOT NULL,
    ProfileName TEXT NOT NULL,
    CommandLineArgs TEXT,
    FOREIGN KEY (GameId) REFERENCES Games (GameId)
);
```

**Relationships:**
- `GameId` → `Games.GameId` (Many profiles per game)

### Companion System

#### CompanionApps
Applications that can be launched alongside games.

```sql
CREATE TABLE CompanionApps (
    CompanionId INTEGER PRIMARY KEY AUTOINCREMENT,
    GameId INTEGER,                    -- NULL = global, specific = game-specific
    Name TEXT NOT NULL,
    Type TEXT NOT NULL,               -- 'Application', 'Website', etc.
    PathOrURL TEXT NOT NULL,
    LaunchHelper TEXT,
    Browser TEXT,
    OpenInNewWindow INTEGER,
    FOREIGN KEY (GameId) REFERENCES Games (GameId)
);
```

**Key Features:**
- `GameId` NULL = Global companions (available for all games)
- `GameId` specific = Game-specific companions
- `Type` determines launch behavior (Application/Website/etc.)

#### CompanionProfiles
Junction table linking profiles to their enabled companions.

```sql
CREATE TABLE CompanionProfiles (
    CompanionProfileId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProfileId INTEGER NOT NULL,
    CompanionId INTEGER NOT NULL,
    IsEnabled INTEGER DEFAULT 1,
    FOREIGN KEY (ProfileId) REFERENCES Profiles (ProfileId),
    FOREIGN KEY (CompanionId) REFERENCES CompanionApps (CompanionId)
);
```

**Relationships:**
- Many-to-Many between `Profiles` and `CompanionApps`
- `IsEnabled` allows toggling companions per profile

### Signature Database System

#### GameSignatures
Signature database for automatic game detection.

```sql
CREATE TABLE GameSignatures (
    GameSignatureId INTEGER PRIMARY KEY AUTOINCREMENT,
    ShortName TEXT NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT,
    GameImage TEXT,
    ThemeName TEXT,
    ExecutableName TEXT NOT NULL,     -- Key for detection
    GameArgs TEXT,
    Version TEXT,
    Publisher TEXT,
    MetaName TEXT,                    -- Alternative matching name
    MatchName BOOLEAN,                -- Enable name matching
    MatchVersion BOOLEAN,             -- Enable version matching  
    MatchPublisher BOOLEAN            -- Enable publisher matching
);
```

**Detection Logic:**
- `ExecutableName` - Primary detection key
- `Match*` booleans control which fields are used for matching
- `MetaName` provides alternative name matching

#### CompanionSignatures  
Signature database for automatic companion detection.

```sql
CREATE TABLE CompanionSignatures (
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
```

**Relationships:**
- `GameSignatureId` → `GameSignatures.GameSignatureId` (Companions belong to games)
- Cascade delete removes companions when game signature is deleted

### Configuration & Settings

#### SearchPaths
Directories to scan for games and applications.

```sql
CREATE TABLE SearchPaths (
    SearchPathId INTEGER PRIMARY KEY AUTOINCREMENT,
    Path TEXT NOT NULL
);
```

#### AppSettings
Application configuration and user preferences.

```sql
CREATE TABLE AppSettings (
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
```

## Database Relationships

### Entity Relationship Overview

```
Games (1) ──→ (N) Profiles
   │              │
   │              └─→ (N) CompanionProfiles ←─(N) CompanionApps
   │                                              │
   └──→ (N) CompanionApps (game-specific)        │
                                                 │
GameSignatures (1) ──→ (N) CompanionSignatures  │
                                                 │
SearchPaths (independent)                        │
AppSettings (singleton)                          │
                                                 │
CompanionApps.GameId = NULL (global companions) ─┘
```

### Key Relationships

1. **Games → Profiles**: One-to-Many
   - Each game can have multiple launch profiles
   - Profiles store custom launch arguments

2. **Profiles ↔ CompanionApps**: Many-to-Many (via CompanionProfiles)
   - Each profile can enable/disable specific companions
   - Companions can be associated with multiple profiles

3. **Games → CompanionApps**: One-to-Many (optional)
   - Game-specific companions (`GameId` set)
   - Global companions (`GameId` is NULL)

4. **GameSignatures → CompanionSignatures**: One-to-Many
   - Signature companions are tied to signature games
   - Used for automatic detection and suggestions

## Migration Strategy

The database includes automatic migration logic in `DatabaseInitializer.cs`:

### Current Migrations

1. **Add IsEnabled Column** (CompanionProfiles)
   ```sql
   ALTER TABLE CompanionProfiles ADD COLUMN IsEnabled INTEGER DEFAULT 1
   ```

2. **Add GameId Column** (CompanionApps)  
   ```sql
   ALTER TABLE CompanionApps ADD COLUMN GameId INTEGER
   ```

3. **Migrate Signature Tables** (KnownGames → GameSignatures)
   - Automatically migrates old `KnownGames`/`KnownCompanions` to new signature tables
   - Preserves existing data while updating schema

### Future Migration Pattern

When adding new columns:
```csharp
// In DatabaseInitializer.cs
try
{
    command.CommandText = "SELECT NewColumn FROM TableName LIMIT 1";
    command.ExecuteScalar();
}
catch
{
    command.CommandText = "ALTER TABLE TableName ADD COLUMN NewColumn DataType DEFAULT DefaultValue";
    command.ExecuteNonQuery();
}
```

## Development Notes

### Repository Pattern Implementation

Each main entity has a corresponding repository:
- `IGameRepository` / `GameRepository`
- `IProfilesRepository` / `ProfilesRepository`  
- `ICompanionRepository` / `CompanionRepository`
- `ICompanionProfileRepository` / `CompanionProfileRepository`
- `IGameSignatureRepository` / `GameSignatureRepository`
- `ICompanionSignatureRepository` / `CompanionSignatureRepository`

### Connection Management

- Database context creates connections using `DatabaseContext.GetConnection()`
- All repositories use `using var connection` pattern for proper disposal
- Connection strings automatically switch between development and production databases

### Environment-Specific Databases

- **Development**: `ALGAE-dev.db` in project root
- **Production**: `%AppData%/AlgaeApp/Database/ALGAE.db`

### Query Patterns

**Basic CRUD:**
```csharp
// Create
await connection.ExecuteAsync(sql, entity);

// Read
await connection.QueryAsync<Entity>(sql, parameters);
await connection.QuerySingleOrDefaultAsync<Entity>(sql, parameters);

// Update  
await connection.ExecuteAsync(sql, entity);

// Delete
await connection.ExecuteAsync("DELETE FROM Table WHERE Id = @Id", new { Id = id });
```

**Complex Queries:**
```csharp
// Get companions for a specific game (including global ones)
public async Task<IEnumerable<Companion>> GetForGameAsync(int gameId)
{
    const string sql = @"
        SELECT * FROM CompanionApps 
        WHERE GameId IS NULL OR GameId = @GameId
        ORDER BY Name";
    return await connection.QueryAsync<Companion>(sql, new { GameId = gameId });
}
```

This schema supports ALGAE's core functionality while remaining flexible for future enhancements.
