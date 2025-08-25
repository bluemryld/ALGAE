using Serilog.Events;

namespace Algae.Core.Services
{
    public class LogSettings
    {
        public LogEventLevel LoggingLevel { get; set; }
        public string LogFilePath { get; set; }
    }
}