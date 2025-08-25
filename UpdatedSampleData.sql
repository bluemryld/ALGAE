-- Updated Sample Data for ALGAE Development Database with GameId for companions
-- This script clears and repopulates all tables with realistic game-specific companion data

-- Clear existing data (in dependency order)
DELETE FROM CompanionProfiles;
DELETE FROM CompanionApps;
DELETE FROM Profiles;
DELETE FROM Games;

-- Insert Sample Games (same as before)
INSERT INTO Games (Name, ShortName, Publisher, Version, Description, InstallPath, ExecutableName, GameArgs, GameWorkingPath) VALUES
('Counter-Strike 2', 'CS2', 'Valve Corporation', '1.0.0', 'The premier competitive FPS game with tactical gameplay and team strategy.', 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive', 'cs2.exe', '-console +fps_max 300', 'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive'),
('Cyberpunk 2077', 'CP2077', 'CD Projekt RED', '2.1.0', 'Open-world action-adventure story set in Night City, a megalopolis obsessed with power, glamour and body modification.', 'C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077', 'Cyberpunk2077.exe', '-windowed', 'C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077\bin\x64'),
('The Witcher 3: Wild Hunt', 'TW3', 'CD Projekt RED', '4.04', 'Story-driven open world RPG set in a visually stunning fantasy universe full of meaningful choices and impactful consequences.', 'C:\Program Files (x86)\Steam\steamapps\common\The Witcher 3', 'witcher3.exe', '', 'C:\Program Files (x86)\Steam\steamapps\common\The Witcher 3\bin\x64'),
('Red Dead Redemption 2', 'RDR2', 'Rockstar Games', '1.0.1491.50', 'Epic tale of life in Americas unforgiving heartland featuring a vast and atmospheric world.', 'C:\Program Files\Rockstar Games\Red Dead Redemption 2', 'RDR2.exe', '-fullscreen', 'C:\Program Files\Rockstar Games\Red Dead Redemption 2'),
('Minecraft', 'MC', 'Mojang Studios', '1.20.4', 'Build, explore and survive in randomly generated worlds with unlimited creative possibilities.', 'C:\Program Files (x86)\Minecraft Launcher', 'MinecraftLauncher.exe', '', 'C:\Users\alan\AppData\Roaming\.minecraft'),
('Hogwarts Legacy', 'HL', 'Warner Bros. Games', '1.0.0', 'Immersive open-world action RPG set in the 1800s wizarding world.', 'C:\Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy', 'HogwartsLegacy.exe', '-dx12', 'C:\Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy');

-- Insert Global Companion Applications (GameId = NULL, applies to all games)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(NULL, 'Discord', 'Application', 'C:\Users\alan\AppData\Local\Discord\Update.exe', '--processStart Discord.exe', NULL, 0),
(NULL, 'Spotify', 'Application', 'C:\Users\alan\AppData\Roaming\Spotify\Spotify.exe', NULL, NULL, 0),
(NULL, 'MSI Afterburner', 'Application', 'C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe', NULL, NULL, 0),
(NULL, 'NVIDIA GeForce Experience', 'Application', 'C:\Program Files\NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe', NULL, NULL, 0),
(NULL, 'YouTube Music', 'Website', 'https://music.youtube.com', NULL, NULL, 0);

-- Insert Counter-Strike 2 specific companions (GameId = 1)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(1, 'FACEIT', 'Website', 'https://www.faceit.com/en/csgo', NULL, 'chrome.exe', 1),
(1, 'ESEA Client', 'Application', 'C:\Program Files\ESEA\ESEA Client.exe', NULL, NULL, 0),
(1, 'CS2 Config Manager', 'Application', 'C:\Program Files\CS2ConfigManager\CS2Config.exe', NULL, NULL, 0),
(1, 'Twitch CS2 Streams', 'Website', 'https://www.twitch.tv/directory/game/Counter-Strike%3A%20Global%20Offensive', NULL, 'chrome.exe', 1);

-- Insert Cyberpunk 2077 specific companions (GameId = 2)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(2, 'Cyberpunk 2077 Mod Manager', 'Application', 'C:\Program Files\Cyberpunk2077ModManager\ModManager.exe', NULL, NULL, 0),
(2, 'REDmod', 'Application', 'C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077\tools\redmod\bin\redmod.exe', NULL, NULL, 0),
(2, 'Nexus Mods - CP2077', 'Website', 'https://www.nexusmods.com/cyberpunk2077', NULL, NULL, 0);

-- Insert The Witcher 3 specific companions (GameId = 3)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(3, 'Witcher 3 Script Merger', 'Application', 'C:\Program Files\WitcherScriptMerger\WitcherScriptMerger.exe', NULL, NULL, 0),
(3, 'Nexus Mods - TW3', 'Website', 'https://www.nexusmods.com/witcher3', NULL, NULL, 0);

-- Insert Red Dead Redemption 2 specific companions (GameId = 4)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(4, 'RDR2 Social Club', 'Application', 'C:\Program Files\Rockstar Games\Launcher\Launcher.exe', NULL, NULL, 0),
(4, 'RDR2 Companion App Info', 'Website', 'https://www.rockstargames.com/reddeadredemption2/companion', NULL, NULL, 0);

-- Insert Minecraft specific companions (GameId = 5)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(5, 'MultiMC', 'Application', 'C:\Program Files\MultiMC\MultiMC.exe', NULL, NULL, 0),
(5, 'CurseForge', 'Application', 'C:\Users\alan\AppData\Local\Programs\CurseForge\CurseForge.exe', NULL, NULL, 0),
(5, 'Minecraft Wiki', 'Website', 'https://minecraft.fandom.com/wiki/Minecraft_Wiki', NULL, NULL, 0);

-- Insert Hogwarts Legacy specific companions (GameId = 6)
INSERT INTO CompanionApps (GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow) VALUES
(6, 'Hogwarts Legacy Mod Manager', 'Application', 'C:\Program Files\HLModManager\HLModManager.exe', NULL, NULL, 0),
(6, 'Nexus Mods - HL', 'Website', 'https://www.nexusmods.com/hogwartslegacy', NULL, NULL, 0);

-- Insert Sample Profiles (same as before)
INSERT INTO Profiles (GameId, ProfileName, CommandLineArgs) VALUES
(1, 'Competitive Play', '-console +fps_max 300 +rate 128000 +cl_updaterate 128 +cl_cmdrate 128'),
(1, 'Casual Play', '-console +fps_max 144 -windowed'),
(1, 'Streaming Setup', '-console +fps_max 240 -novid +tv_enable 1'),
(2, 'Ultra Settings', '-windowed --launcher-skip'),
(2, 'Performance Mode', '-windowed --launcher-skip -lowmemory'),
(3, 'Modded Playthrough', '-scriptdebug'),
(3, 'Vanilla Experience', ''),
(4, 'Story Mode', '-windowed -vulkan'),
(4, 'Online Mode', '-windowed -vulkan -online'),
(5, 'Creative Mode', '--workDir %APPDATA%\.minecraft'),
(5, 'Modded (Forge)', '--workDir %APPDATA%\.minecraft --version 1.20.4-forge'),
(6, 'Ray Tracing On', '-dx12 -rtx'),
(6, 'Performance Mode', '-dx11 -nortx');

-- Insert Sample Companion Profile Associations with game-specific companions
-- Counter-Strike 2 profiles
INSERT INTO CompanionProfiles (ProfileId, CompanionId, IsEnabled) VALUES
-- Competitive Play profile (includes CS2-specific companions)
(1, 1, 1),  -- Discord (global)
(1, 3, 1),  -- MSI Afterburner (global)
(1, 4, 1),  -- NVIDIA GeForce Experience (global)
(1, 6, 1),  -- FACEIT (CS2-specific)
(1, 7, 0),  -- ESEA Client (CS2-specific, disabled)
-- Casual Play profile
(2, 1, 1),  -- Discord (global)
(2, 2, 1),  -- Spotify (global)
(2, 5, 1),  -- YouTube Music (global)
-- Streaming Setup profile
(3, 1, 1),  -- Discord (global)
(3, 3, 1),  -- MSI Afterburner (global)
(3, 4, 1),  -- NVIDIA GeForce Experience (global)
(3, 9, 1),  -- Twitch CS2 Streams (CS2-specific)

-- Cyberpunk 2077 profiles
-- Ultra Settings profile
(4, 1, 1),  -- Discord (global)
(4, 2, 1),  -- Spotify (global)
(4, 3, 1),  -- MSI Afterburner (global)
(4, 4, 1),  -- NVIDIA GeForce Experience (global)
(4, 10, 1), -- Cyberpunk 2077 Mod Manager (CP2077-specific)
-- Performance Mode profile
(5, 1, 1),  -- Discord (global)
(5, 3, 1),  -- MSI Afterburner (global)

-- The Witcher 3 profiles
-- Modded Playthrough profile
(6, 1, 1),  -- Discord (global)
(6, 2, 1),  -- Spotify (global)
(6, 5, 1),  -- YouTube Music (global)
(6, 13, 1), -- Witcher 3 Script Merger (TW3-specific)
-- Vanilla Experience profile
(7, 1, 1),  -- Discord (global)
(7, 2, 1),  -- Spotify (global)

-- Red Dead Redemption 2 profiles
-- Story Mode profile
(8, 1, 1),  -- Discord (global)
(8, 2, 1),  -- Spotify (global)
(8, 3, 1),  -- MSI Afterburner (global)
(8, 15, 1), -- RDR2 Social Club (RDR2-specific)
-- Online Mode profile
(9, 1, 1),  -- Discord (global)
(9, 15, 1), -- RDR2 Social Club (RDR2-specific)

-- Minecraft profiles
-- Creative Mode profile
(10, 1, 1), -- Discord (global)
(10, 2, 1), -- Spotify (global)
(10, 5, 1), -- YouTube Music (global)
-- Modded (Forge) profile
(11, 1, 1), -- Discord (global)
(11, 2, 1), -- Spotify (global)
(11, 17, 1), -- MultiMC (MC-specific)
(11, 18, 1), -- CurseForge (MC-specific)

-- Hogwarts Legacy profiles
-- Ray Tracing On profile
(12, 1, 1), -- Discord (global)
(12, 2, 1), -- Spotify (global)
(12, 3, 1), -- MSI Afterburner (global)
(12, 4, 1), -- NVIDIA GeForce Experience (global)
(12, 20, 1), -- Hogwarts Legacy Mod Manager (HL-specific)
-- Performance Mode profile
(13, 1, 1), -- Discord (global)
(13, 2, 1), -- Spotify (global)
(13, 3, 1); -- MSI Afterburner (global)

-- Display summary
SELECT 'Games' as TableName, COUNT(*) as RecordCount FROM Games
UNION ALL
SELECT 'CompanionApps' as TableName, COUNT(*) as RecordCount FROM CompanionApps
UNION ALL
SELECT 'Profiles' as TableName, COUNT(*) as RecordCount FROM Profiles
UNION ALL
SELECT 'CompanionProfiles' as TableName, COUNT(*) as RecordCount FROM CompanionProfiles;
