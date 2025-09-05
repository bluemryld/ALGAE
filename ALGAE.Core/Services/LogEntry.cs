using Serilog.Events;

namespace Algae.Core.Services
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogEventLevel Severity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}