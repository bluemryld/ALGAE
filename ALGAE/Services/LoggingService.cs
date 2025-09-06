using System.Collections.Concurrent;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;

namespace ALGAE.Services
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string FormattedMessage => $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Category}: {Message}{(Exception != null ? $"\n{Exception}" : "")}";
    }

    public class LogEntryMessage
    {
        public LogEntry Entry { get; }
        public LogEntryMessage(LogEntry entry) => Entry = entry;
    }

    public interface ILoggingService
    {
        LogLevel CurrentLevel { get; set; }
        void Log(LogLevel level, string category, string message, Exception? exception = null);
        void LogTrace(string category, string message);
        void LogDebug(string category, string message);
        void LogInformation(string category, string message);
        void LogWarning(string category, string message);
        void LogError(string category, string message, Exception? exception = null);
        void LogCritical(string category, string message, Exception? exception = null);
        IEnumerable<LogEntry> GetRecentEntries(int count = 1000);
        void ClearLogs();
    }

    public class LoggingService : ILoggingService, IDisposable
    {
        private readonly IMessenger _messenger;
        private readonly ConcurrentQueue<LogEntry> _logEntries = new();
        private readonly string _logFilePath;
        private readonly Timer _rotationTimer;
        private readonly object _fileLock = new();
        private LogLevel _currentLevel = LogLevel.Information;
        private const int MaxLogEntries = 10000;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private const int MaxLogFiles = 5;

        public LogLevel CurrentLevel 
        { 
            get => _currentLevel; 
            set => _currentLevel = value; 
        }

        public LoggingService(IMessenger messenger)
        {
            _messenger = messenger;
            _logFilePath = GetLogFilePath();
            
            // Set default log level based on build mode
            SetDefaultLogLevel();
            
            // Ensure log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
            
            // Clean up old log files
            CleanupOldLogFiles();
            
            // Set up rotation timer (check every hour)
            _rotationTimer = new Timer(CheckLogRotation, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            
            LogInformation("LoggingService", $"Logging service initialized (Level: {_currentLevel})");
        }

        public void Log(LogLevel level, string category, string message, Exception? exception = null)
        {
            if (level < _currentLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception?.ToString()
            };

            // Add to memory queue
            _logEntries.Enqueue(entry);
            
            // Maintain max entries
            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.TryDequeue(out _);
            }

            // Write to file
            WriteToFile(entry);

            // Send to UI
            _messenger.Send(new LogEntryMessage(entry));
        }

        public void LogTrace(string category, string message) => Log(LogLevel.Trace, category, message);
        public void LogDebug(string category, string message) => Log(LogLevel.Debug, category, message);
        public void LogInformation(string category, string message) => Log(LogLevel.Information, category, message);
        public void LogWarning(string category, string message) => Log(LogLevel.Warning, category, message);
        public void LogError(string category, string message, Exception? exception = null) => Log(LogLevel.Error, category, message, exception);
        public void LogCritical(string category, string message, Exception? exception = null) => Log(LogLevel.Critical, category, message, exception);

        public IEnumerable<LogEntry> GetRecentEntries(int count = 1000)
        {
            return _logEntries.TakeLast(count).ToList();
        }

        public void ClearLogs()
        {
            while (_logEntries.TryDequeue(out _)) { }
            
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_logFilePath))
                    {
                        File.WriteAllText(_logFilePath, "");
                    }
                }
                catch (Exception ex)
                {
                    // Can't log this error since we're clearing logs
                    System.Diagnostics.Debug.WriteLine($"Error clearing log file: {ex.Message}");
                }
            }
            
            LogInformation("LoggingService", "Logs cleared");
        }

        private void WriteToFile(LogEntry entry)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, entry.FormattedMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // Fallback to Debug.WriteLine if file logging fails
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(entry.FormattedMessage);
            }
        }

        private void CheckLogRotation(object? state)
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_logFilePath))
                    {
                        var fileInfo = new FileInfo(_logFilePath);
                        if (fileInfo.Length > MaxFileSizeBytes)
                        {
                            RotateLogFile();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during log rotation: {ex.Message}");
            }
        }

        private void RotateLogFile()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath)!;
                var fileName = Path.GetFileNameWithoutExtension(_logFilePath);
                var extension = Path.GetExtension(_logFilePath);

                // Move existing numbered logs
                for (int i = MaxLogFiles - 1; i >= 1; i--)
                {
                    var oldPath = Path.Combine(directory, $"{fileName}.{i}{extension}");
                    var newPath = Path.Combine(directory, $"{fileName}.{i + 1}{extension}");
                    
                    if (File.Exists(oldPath))
                    {
                        if (i == MaxLogFiles - 1)
                        {
                            File.Delete(oldPath); // Delete oldest log
                        }
                        else
                        {
                            if (File.Exists(newPath)) File.Delete(newPath);
                            File.Move(oldPath, newPath);
                        }
                    }
                }

                // Move current log to .1
                var rotatedPath = Path.Combine(directory, $"{fileName}.1{extension}");
                if (File.Exists(rotatedPath)) File.Delete(rotatedPath);
                File.Move(_logFilePath, rotatedPath);

                LogInformation("LoggingService", $"Log file rotated. New file started.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rotating log file: {ex.Message}");
            }
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath)!;
                var fileName = Path.GetFileNameWithoutExtension(_logFilePath);
                var extension = Path.GetExtension(_logFilePath);

                // Delete logs older than MaxLogFiles
                for (int i = MaxLogFiles + 1; i <= 20; i++) // Check up to 20 to clean up any old files
                {
                    var oldPath = Path.Combine(directory, $"{fileName}.{i}{extension}");
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up old log files: {ex.Message}");
            }
        }

        private static string GetLogFilePath()
        {
            if (App.IsDevelopmentEnvironment())
            {
                return Path.Combine("logs", "algae-dev.log");
            }
            else
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logFolder = Path.Combine(appDataPath, "AlgaeApp", "Logs");
                return Path.Combine(logFolder, "algae.log");
            }
        }

        public void Dispose()
        {
            _rotationTimer?.Dispose();
            LogInformation("LoggingService", "Logging service disposed");
        }

        private void SetDefaultLogLevel()
        {
#if DEBUG
            _currentLevel = LogLevel.Debug;
#else
            // In release mode, try to load from settings, otherwise default to Information
            try
            {
                var settings = Algae.Core.Services.AppSettings.Load();
                if (Enum.TryParse<LogLevel>(settings.AppLogging.LogLevel, out var logLevel))
                {
                    _currentLevel = logLevel;
                }
                else
                {
                    _currentLevel = LogLevel.Information;
                }
            }
            catch
            {
                _currentLevel = LogLevel.Information;
            }
#endif
        }
    }
}