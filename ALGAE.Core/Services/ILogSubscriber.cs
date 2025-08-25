using Serilog.Events;

namespace Algae.Core.Services
{
    public interface ILogSubscriber
    {
        List<LogEventLevel> SeverityFilter { get; }
        List<string> TypeFilter { get; }
        void OnLogEntryAdded(LogEntry entry);
    }
}