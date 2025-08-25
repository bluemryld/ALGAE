using Serilog.Events;

namespace Algae.Core.Services
{
    public interface ILogService
    {
        void Log(string message, LogEventLevel level, string type);
        void Subscribe(ILogSubscriber subscriber);
    }
}
