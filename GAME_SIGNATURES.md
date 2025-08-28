# 🎮 ALGAE Game Signatures Database

This document describes the game signatures system used by ALGAE for automatic game detection.

## 📋 Overview

Game signatures are predefined patterns that help ALGAE automatically identify installed games during directory scanning. Each signature contains metadata about a specific game, including its executable name, publisher, and other identifying information.

## 🗂️ Signature Structure

Each game signature contains the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `ShortName` | String | Abbreviated game name (e.g., "TW3" for The Witcher 3) |
| `Name` | String | Full game name |
| `Description` | String | Brief description of the game |
| `ExecutableName` | String | Main executable filename (e.g., "witcher3.exe") |
| `Publisher` | String | Game publisher/developer |
| `MetaName` | String | Product name from file metadata |
| `MatchName` | Boolean | Whether to match executable name (usually `true`) |
| `MatchVersion` | Boolean | Version matching (set to `false` to prevent update issues) |
| `MatchPublisher` | Boolean | Whether to match publisher (usually `true`) |

## 📊 Included Games (50+ Signatures)

### 🎯 AAA Single Player Games
- **The Witcher 3: Wild Hunt** - `witcher3.exe` (CD PROJEKT RED)
- **Cyberpunk 2077** - `Cyberpunk2077.exe` (CD PROJEKT RED)
- **Grand Theft Auto V** - `GTA5.exe` (Rockstar Games)
- **Red Dead Redemption 2** - `RDR2.exe` (Rockstar Games)
- **Assassin's Creed Valhalla** - `ACValhalla.exe` (Ubisoft)
- **Elden Ring** - `eldenring.exe` (FromSoftware)
- **Dark Souls III** - `DarkSoulsIII.exe` (FromSoftware)

### 🔥 Multiplayer/Competitive Games
- **Counter-Strike 2** - `cs2.exe` (Valve)
- **League of Legends** - `League of Legends.exe` (Riot Games)
- **VALORANT** - `VALORANT.exe` (Riot Games)
- **Overwatch 2** - `Overwatch.exe` (Blizzard)
- **Apex Legends** - `r5apex.exe` (Electronic Arts)

### 🎲 Steam Popular Games
- **Half-Life: Alyx** - `hlvr.exe` (Valve)
- **Portal 2** - `portal2.exe` (Valve)
- **Team Fortress 2** - `hl2.exe` (Valve)
- **Dota 2** - `dota2.exe` (Valve)

### 💎 Indie Games
- **Hades** - `Hades.exe` (Supergiant Games)
- **Hollow Knight** - `hollow_knight.exe` (Team Cherry)
- **Stardew Valley** - `Stardew Valley.exe` (ConcernedApe)

### ⚔️ Blizzard Games
- **World of Warcraft** - `Wow.exe` (Blizzard Entertainment)
- **Diablo IV** - `Diablo IV.exe` (Blizzard Entertainment)
- **Hearthstone** - `Hearthstone.exe` (Blizzard Entertainment)

### 🎮 Microsoft/Xbox Game Pass
- **Halo Infinite** - `HaloInfinite.exe` (Microsoft)
- **Forza Horizon 5** - `ForzaHorizon5.exe` (Microsoft)
- **Sea of Thieves** - `SoTGame.exe` (Microsoft)

### 🚀 Epic Games Store
- **Fortnite** - `FortniteClient-Win64-Shipping.exe` (Epic Games)
- **Rocket League** - `RocketLeague.exe` (Psyonix)

### 🏗️ Simulation Games
- **Cities: Skylines** - `Cities.exe` (Paradox Interactive)
- **The Sims 4** - `TS4_x64.exe` (Electronic Arts)
- **Euro Truck Simulator 2** - `eurotrucks2.exe` (SCS Software)

### 🧠 Strategy Games
- **Civilization VI** - `CivilizationVI.exe` (2K Games)
- **Age of Empires IV** - `RelicCardinal.exe` (Microsoft)
- **Total War: WARHAMMER III** - `Warhammer3.exe` (SEGA)

### 🕹️ Classic Games
- **Minecraft** - `Minecraft.exe` (Microsoft)
- **Terraria** - `Terraria.exe` (Re-Logic)

### 👻 Horror Games
- **Phasmophobia** - `Phasmophobia.exe` (Kinetic Games)
- **Dead by Daylight** - `DeadByDaylight-Win64-Shipping.exe` (Behaviour Interactive)

### 🏎️ Racing Games
- **F1 23** - `F1_23.exe` (Electronic Arts)
- **DiRT Rally 2.0** - `dirtrally2.exe` (Codemasters)

## 🚀 Installation

### Method 1: PowerShell Script (Recommended)
```powershell
# Run the import script
.\import_game_signatures.ps1

# Or with options
.\import_game_signatures.ps1 -DatabasePath "path\to\ALGAE.db" -ClearExisting
```

### Method 2: Manual SQL Import
```bash
# Using SQLite command line
sqlite3 ALGAE.db < game_signatures.sql
```

### Method 3: Through ALGAE Application
1. Build and run ALGAE application first to create the database
2. Use one of the above methods to import signatures
3. Restart ALGAE to use the new signatures

## ⚙️ Configuration Notes

### Version Matching Disabled
- `MatchVersion` is set to `false` for all signatures
- This prevents false negatives when games receive updates
- Games are identified by executable name and publisher instead

### Match Criteria Priority
1. **Executable Name** (70% weight) - Most reliable identifier
2. **Publisher** (30% weight) - Secondary confirmation  
3. **Product Metadata** (25% weight) - Additional validation

### Confidence Scoring
- **90-100%**: Excellent (multiple strong matches)
- **70-89%**: Very Good (strong primary match)
- **50-69%**: Good (basic signature match)
- **30-49%**: Fair (heuristic detection only)

## 🔧 Adding Custom Signatures

To add your own game signatures, insert into the `GameSignatures` table:

```sql
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'YourGame', 'Your Game Title', 'Description of your game',
    'yourgame.exe', 'Your Publisher', 'Your Game Title',
    1, 0, 1  -- Match name and publisher, but not version
);
```

## 🧪 Testing Signatures

After importing signatures, you can test them by:

1. **Running a scan** - Use "Scan for Games" in ALGAE
2. **Checking detection** - Look for high-confidence matches
3. **Verification dialog** - Review detected games before adding

## 📈 Signature Effectiveness

These signatures are designed to detect the most popular games across major platforms:
- **Steam** - Most comprehensive coverage
- **Epic Games Store** - Major exclusives and free games
- **Microsoft Store/Game Pass** - Xbox ecosystem games
- **Blizzard Battle.net** - All major Blizzard titles
- **GOG** - DRM-free versions of popular games
- **Indie Platforms** - Popular independent games

## 🔄 Maintenance

Game signatures may need updates when:
- Publishers change company names
- Games receive major rebrandings
- Executable names change (rare)
- New popular games are released

The database can be updated by running the import script again with new signature files.

## 📞 Support

If you encounter issues with game detection:
1. Check the game's executable name matches the signature
2. Verify the publisher name in file properties
3. Consider the confidence threshold (games below 30% won't be detected)
4. Add custom signatures for unsupported games

## 📝 License

These game signatures are provided for use with the ALGAE application. Game names and publisher information are trademarks of their respective owners.
