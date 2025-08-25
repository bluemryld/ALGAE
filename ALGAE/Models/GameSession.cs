using System;

namespace ALGAE.Models
{
    /// <summary>
    /// Represents a game playing session
    /// </summary>
    public class GameSession
    {
        public string GameName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.Now.Subtract(StartTime);
        public string Status => EndTime.HasValue ? "Completed" : "Running";
        public int ProcessId { get; set; }
    }
}
