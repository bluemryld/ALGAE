-- Popular Game Signatures for ALGAE Game Detection System
-- This script populates the GameSignatures table with known game signatures
-- Note: MatchVersion is set to 0 since we removed version matching to prevent false negatives

-- Clear existing signatures (optional - remove this line if you want to keep existing data)
-- DELETE FROM GameSignatures;

-- ====================
-- AAA SINGLE PLAYER GAMES
-- ====================

-- The Witcher 3: Wild Hunt
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName, 
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'TW3', 'The Witcher 3: Wild Hunt', 'Open-world RPG by CD PROJEKT RED', 
    'witcher3.exe', 'CD PROJEKT RED', 'The Witcher 3: Wild Hunt',
    1, 0, 1
);

-- Cyberpunk 2077
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'CP2077', 'Cyberpunk 2077', 'Futuristic RPG by CD PROJEKT RED',
    'Cyberpunk2077.exe', 'CD PROJEKT RED', 'Cyberpunk 2077',
    1, 0, 1
);

-- Grand Theft Auto V
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'GTAV', 'Grand Theft Auto V', 'Open-world action game by Rockstar Games',
    'GTA5.exe', 'Rockstar Games', 'Grand Theft Auto V',
    1, 0, 1
);

-- Red Dead Redemption 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'RDR2', 'Red Dead Redemption 2', 'Western action-adventure by Rockstar Games',
    'RDR2.exe', 'Rockstar Games', 'Red Dead Redemption 2',
    1, 0, 1
);

-- Assassin's Creed Valhalla
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'ACV', 'Assassin''s Creed Valhalla', 'Viking adventure by Ubisoft',
    'ACValhalla.exe', 'Ubisoft Entertainment', 'Assassin''s Creed Valhalla',
    1, 0, 1
);

-- Elden Ring
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'ER', 'Elden Ring', 'Dark fantasy action RPG by FromSoftware',
    'eldenring.exe', 'FromSoftware', 'ELDEN RING',
    1, 0, 1
);

-- Dark Souls III
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'DS3', 'Dark Souls III', 'Action RPG by FromSoftware',
    'DarkSoulsIII.exe', 'FromSoftware', 'DARK SOULS III',
    1, 0, 1
);

-- ====================
-- MULTIPLAYER/COMPETITIVE GAMES
-- ====================

-- Counter-Strike 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'CS2', 'Counter-Strike 2', 'Tactical FPS by Valve',
    'cs2.exe', 'Valve Corporation', 'Counter-Strike 2',
    1, 0, 1
);

-- League of Legends
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'LoL', 'League of Legends', 'MOBA by Riot Games',
    'League of Legends.exe', 'Riot Games', 'League of Legends',
    1, 0, 1
);

-- VALORANT
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'VALORANT', 'VALORANT', 'Tactical shooter by Riot Games',
    'VALORANT.exe', 'Riot Games', 'VALORANT',
    1, 0, 1
);

-- Overwatch 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'OW2', 'Overwatch 2', 'Team-based shooter by Blizzard',
    'Overwatch.exe', 'Blizzard Entertainment', 'Overwatch',
    1, 0, 1
);

-- Apex Legends
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'APEX', 'Apex Legends', 'Battle royale by Respawn Entertainment',
    'r5apex.exe', 'Electronic Arts', 'Apex Legends',
    1, 0, 1
);

-- ====================
-- STEAM POPULAR GAMES
-- ====================

-- Half-Life: Alyx
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'HLA', 'Half-Life: Alyx', 'VR game by Valve',
    'hlvr.exe', 'Valve Corporation', 'Half-Life: Alyx',
    1, 0, 1
);

-- Portal 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Portal2', 'Portal 2', 'Puzzle game by Valve',
    'portal2.exe', 'Valve Corporation', 'Portal 2',
    1, 0, 1
);

-- Team Fortress 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'TF2', 'Team Fortress 2', 'Class-based shooter by Valve',
    'hl2.exe', 'Valve Corporation', 'Team Fortress 2',
    1, 0, 1
);

-- Dota 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Dota2', 'Dota 2', 'MOBA by Valve',
    'dota2.exe', 'Valve Corporation', 'Dota 2',
    1, 0, 1
);

-- ====================
-- INDIE GAMES
-- ====================

-- Hades
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Hades', 'Hades', 'Rogue-like by Supergiant Games',
    'Hades.exe', 'Supergiant Games', 'Hades',
    1, 0, 1
);

-- Hollow Knight
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'HK', 'Hollow Knight', 'Metroidvania by Team Cherry',
    'hollow_knight.exe', 'Team Cherry', 'Hollow Knight',
    1, 0, 1
);

-- Stardew Valley
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'SDV', 'Stardew Valley', 'Farming simulation by ConcernedApe',
    'Stardew Valley.exe', 'ConcernedApe', 'Stardew Valley',
    1, 0, 1
);

-- ====================
-- BLIZZARD GAMES
-- ====================

-- World of Warcraft
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'WoW', 'World of Warcraft', 'MMORPG by Blizzard Entertainment',
    'Wow.exe', 'Blizzard Entertainment', 'World of Warcraft',
    1, 0, 1
);

-- Diablo IV
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'D4', 'Diablo IV', 'Action RPG by Blizzard Entertainment',
    'Diablo IV.exe', 'Blizzard Entertainment', 'Diablo IV',
    1, 0, 1
);

-- Hearthstone
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'HS', 'Hearthstone', 'Digital card game by Blizzard Entertainment',
    'Hearthstone.exe', 'Blizzard Entertainment', 'Hearthstone',
    1, 0, 1
);

-- ====================
-- MICROSOFT/XBOX GAME PASS
-- ====================

-- Halo Infinite
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'HaloInf', 'Halo Infinite', 'Sci-fi shooter by 343 Industries',
    'HaloInfinite.exe', 'Microsoft Corporation', 'Halo Infinite',
    1, 0, 1
);

-- Forza Horizon 5
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'FH5', 'Forza Horizon 5', 'Racing game by Playground Games',
    'ForzaHorizon5.exe', 'Microsoft Corporation', 'Forza Horizon 5',
    1, 0, 1
);

-- Sea of Thieves
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'SoT', 'Sea of Thieves', 'Pirate adventure by Rare',
    'SoTGame.exe', 'Microsoft Corporation', 'Sea of Thieves',
    1, 0, 1
);

-- ====================
-- EPIC GAMES STORE
-- ====================

-- Fortnite
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Fortnite', 'Fortnite', 'Battle royale by Epic Games',
    'FortniteClient-Win64-Shipping.exe', 'Epic Games', 'Fortnite',
    1, 0, 1
);

-- Rocket League
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'RL', 'Rocket League', 'Car soccer by Psyonix',
    'RocketLeague.exe', 'Psyonix LLC', 'Rocket League',
    1, 0, 1
);

-- ====================
-- SIMULATION GAMES
-- ====================

-- Cities: Skylines
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'CitiesXL', 'Cities: Skylines', 'City builder by Colossal Order',
    'Cities.exe', 'Paradox Interactive', 'Cities: Skylines',
    1, 0, 1
);

-- The Sims 4
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Sims4', 'The Sims 4', 'Life simulation by Maxis',
    'TS4_x64.exe', 'Electronic Arts', 'The Sims 4',
    1, 0, 1
);

-- Euro Truck Simulator 2
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'ETS2', 'Euro Truck Simulator 2', 'Truck simulation by SCS Software',
    'eurotrucks2.exe', 'SCS Software', 'Euro Truck Simulator 2',
    1, 0, 1
);

-- ====================
-- STRATEGY GAMES
-- ====================

-- Civilization VI
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Civ6', 'Sid Meier''s Civilization VI', 'Turn-based strategy by Firaxis Games',
    'CivilizationVI.exe', '2K Games', 'Sid Meier''s Civilization VI',
    1, 0, 1
);

-- Age of Empires IV
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'AoE4', 'Age of Empires IV', 'RTS by Relic Entertainment',
    'RelicCardinal.exe', 'Microsoft Corporation', 'Age of Empires IV',
    1, 0, 1
);

-- Total War: Warhammer III
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'TWWH3', 'Total War: WARHAMMER III', 'Strategy by Creative Assembly',
    'Warhammer3.exe', 'SEGA', 'Total War: WARHAMMER III',
    1, 0, 1
);

-- ====================
-- RETRO/CLASSIC GAMES
-- ====================

-- Minecraft
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'MC', 'Minecraft', 'Sandbox game by Mojang Studios',
    'Minecraft.exe', 'Microsoft Corporation', 'Minecraft',
    1, 0, 1
);

-- Terraria
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Terraria', 'Terraria', '2D sandbox by Re-Logic',
    'Terraria.exe', 'Re-Logic', 'Terraria',
    1, 0, 1
);

-- ====================
-- HORROR GAMES
-- ====================

-- Phasmophobia
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'Phasmo', 'Phasmophobia', 'Horror co-op by Kinetic Games',
    'Phasmophobia.exe', 'Kinetic Games', 'Phasmophobia',
    1, 0, 1
);

-- Dead by Daylight
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'DbD', 'Dead by Daylight', 'Asymmetric horror by Behaviour Interactive',
    'DeadByDaylight-Win64-Shipping.exe', 'Behaviour Interactive', 'Dead by Daylight',
    1, 0, 1
);

-- ====================
-- RACING GAMES
-- ====================

-- F1 23
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'F123', 'F1 23', 'Formula 1 racing by Codemasters',
    'F1_23.exe', 'Electronic Arts', 'F1 23',
    1, 0, 1
);

-- Dirt Rally 2.0
INSERT INTO GameSignatures (
    ShortName, Name, Description, ExecutableName, Publisher, MetaName,
    MatchName, MatchVersion, MatchPublisher
) VALUES (
    'DR2', 'DiRT Rally 2.0', 'Rally racing by Codemasters',
    'dirtrally2.exe', 'Codemasters', 'DiRT Rally 2.0',
    1, 0, 1
);

-- Print completion message
SELECT 'Game signatures inserted successfully!' AS Result;
