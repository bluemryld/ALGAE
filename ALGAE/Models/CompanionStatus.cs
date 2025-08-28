using System.Diagnostics;
using Algae.DAL.Models;

namespace ALGAE.Models
{
    /// <summary>
    /// Represents the current status of a companion application
    /// </summary>
    public class CompanionStatus
    {
        public Companion Companion { get; set; } = null!;
        public bool IsRunning { get; set; }
        public Process? Process { get; set; }
        public DateTime? StartTime { get; set; }
        public string Status { get; set; } = "Stopped";
        public string StatusIcon { get; set; } = "Stop";
        public System.Windows.Media.Brush StatusColor { get; set; } = System.Windows.Media.Brushes.Gray;
        public TimeSpan RunningTime => StartTime.HasValue ? DateTime.Now - StartTime.Value : TimeSpan.Zero;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool CanStart => !IsRunning && !HasError;
        public bool CanStop => IsRunning;
    }
}
