using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Serilog;

namespace Algae.Core.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger _logger;
        private readonly List<ILogSubscriber> _subscribers = new();

        public LogService(LogSettings settings)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(settings.LoggingLevel)
                .WriteTo.File(settings.LogFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void Log(string message, LogEventLevel level, string type)
        {
            _logger.Write(level, "[{Type}] {Message}", type, message);

            NotifySubscribers(new LogEntry
            {
                Severity = level,
                Type = type,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        public void Subscribe(ILogSubscriber subscriber)
        {
            _subscribers.Add(subscriber);
        }

        private void NotifySubscribers(LogEntry entry)
        {
            foreach (var subscriber in _subscribers)
            {
                if (subscriber.SeverityFilter.Contains(entry.Severity) &&
                    subscriber.TypeFilter.Contains(entry.Type))
                {
                    subscriber.OnLogEntryAdded(entry);
                }
            }
        }
    }
}
