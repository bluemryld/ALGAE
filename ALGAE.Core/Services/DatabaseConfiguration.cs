using System;
using System.Collections.Generic;
using System.IO;

namespace Algae.Core.Services
{
    /// <summary>
    /// Represents a database configuration entry
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ConnectionString => $"Data Source={FilePath}";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUsedDate { get; set; } = DateTime.Now;
        public bool IsDefault { get; set; }
        public bool IsDebugDatabase { get; set; }
        public long SizeInBytes { get; set; }
        
        public string DisplayName => IsDebugDatabase ? $"{Name} (Debug)" : Name;
        public string SizeFormatted => FormatFileSize(SizeInBytes);
        public bool Exists => File.Exists(FilePath);
        
        public void UpdateSize()
        {
            if (Exists)
            {
                var fileInfo = new FileInfo(FilePath);
                SizeInBytes = fileInfo.Length;
            }
            else
            {
                SizeInBytes = 0;
            }
        }
        
        private static string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            
            return $"{size:F1} {units[unitIndex]}";
        }
    }

    /// <summary>
    /// Manages database configurations and settings
    /// </summary>
    public class DatabaseSettings
    {
        public List<DatabaseConfiguration> Databases { get; set; } = new();
        public string? CurrentDatabaseId { get; set; }
        public bool AutoDownloadSignatures { get; set; } = true;
        public bool ShowDatabaseSwitchPrompt { get; set; } = true;
        
        public DatabaseConfiguration? GetCurrentDatabase()
        {
            if (string.IsNullOrEmpty(CurrentDatabaseId))
                return null;
                
            return Databases.FirstOrDefault(db => db.Id == CurrentDatabaseId);
        }
        
        public DatabaseConfiguration? GetDefaultDatabase()
        {
            return Databases.FirstOrDefault(db => db.IsDefault);
        }
        
        public DatabaseConfiguration? GetDebugDatabase()
        {
            return Databases.FirstOrDefault(db => db.IsDebugDatabase);
        }
        
        public DatabaseConfiguration? GetMostRecentDatabase()
        {
            return Databases
                .Where(db => db.Exists)
                .OrderByDescending(db => db.LastUsedDate)
                .FirstOrDefault();
        }
        
        public void AddDatabase(DatabaseConfiguration database)
        {
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(database.Id))
            {
                database.Id = Guid.NewGuid().ToString("N")[..8];
            }
            
            // Ensure no duplicate IDs
            while (Databases.Any(db => db.Id == database.Id))
            {
                database.Id = Guid.NewGuid().ToString("N")[..8];
            }
            
            database.UpdateSize();
            Databases.Add(database);
        }
        
        public void RemoveDatabase(string databaseId)
        {
            var database = Databases.FirstOrDefault(db => db.Id == databaseId);
            if (database != null)
            {
                Databases.Remove(database);
                
                // If we removed the current database, clear the current selection
                if (CurrentDatabaseId == databaseId)
                {
                    CurrentDatabaseId = null;
                }
                
                // If we removed the default database, clear default flag
                if (database.IsDefault)
                {
                    // Optionally set another database as default
                    var nextDefault = Databases.FirstOrDefault(db => db.Exists);
                    if (nextDefault != null)
                    {
                        nextDefault.IsDefault = true;
                    }
                }
            }
        }
        
        public void SetCurrentDatabase(string databaseId)
        {
            var database = Databases.FirstOrDefault(db => db.Id == databaseId);
            if (database != null)
            {
                CurrentDatabaseId = databaseId;
                database.LastUsedDate = DateTime.Now;
            }
        }
        
        public void SetDefaultDatabase(string databaseId)
        {
            // Clear existing default
            foreach (var db in Databases)
            {
                db.IsDefault = false;
            }
            
            // Set new default
            var database = Databases.FirstOrDefault(db => db.Id == databaseId);
            if (database != null)
            {
                database.IsDefault = true;
            }
        }
        
        public void RefreshDatabaseSizes()
        {
            foreach (var database in Databases)
            {
                database.UpdateSize();
            }
        }
    }
}