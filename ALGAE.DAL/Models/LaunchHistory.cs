using System;

namespace Algae.DAL.Models
{
    /// <summary>
    /// Represents a record of a game launch attempt
    /// </summary>
    public class LaunchHistory
    {
        public int LaunchId { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public DateTime LaunchTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan? PlayTime => EndTime?.Subtract(LaunchTime);
        public string Status => EndTime.HasValue ? "Completed" : "Running";
        
        // Additional metadata
        public int? ProcessId { get; set; }
        public string? ExecutablePath { get; set; }
        public string? WorkingDirectory { get; set; }
        public string? LaunchArguments { get; set; }
        
        // Performance tracking
        public string? PeakMemoryUsage { get; set; }
        public double? AverageCpuUsage { get; set; }
        
        // Navigation property
        public virtual Game? Game { get; set; }
    }
}
