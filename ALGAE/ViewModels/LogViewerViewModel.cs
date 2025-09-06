using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ALGAE.Services;

namespace ALGAE.ViewModels
{
    public partial class LogViewerViewModel : ObservableObject, IRecipient<LogEntryMessage>
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        [ObservableProperty]
        private ICollectionView _filteredLogEntries;

        [ObservableProperty]
        private LogLevel _selectedLogLevel = LogLevel.Trace;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _autoScroll = true;

        [ObservableProperty]
        private int _totalEntries;

        [ObservableProperty]
        private int _filteredCount;

        [ObservableProperty]
        private DateTime _lastUpdate;

        public LogLevel[] LogLevels => Enum.GetValues<LogLevel>();

        public LogViewerViewModel(ILoggingService loggingService, IMessenger messenger)
        {
            _loggingService = loggingService;
            _messenger = messenger;

            // Set up collection view for filtering
            _filteredLogEntries = CollectionViewSource.GetDefaultView(LogEntries);
            _filteredLogEntries.Filter = FilterLogEntries;

            // Listen for new log entries
            _messenger.Register<LogEntryMessage>(this);

            // Load existing entries
            LoadExistingEntries();

            // Set up property change notifications for filtering
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedLogLevel) || e.PropertyName == nameof(SearchText))
            {
                FilteredLogEntries.Refresh();
                UpdateFilteredCount();
            }
        }

        private bool FilterLogEntries(object obj)
        {
            if (obj is not LogEntry entry)
                return false;

            // Filter by log level
            if (entry.Level < SelectedLogLevel)
                return false;

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower();
                return entry.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       entry.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       entry.Level.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private void LoadExistingEntries()
        {
            var existingEntries = _loggingService.GetRecentEntries().ToList();
            
            foreach (var entry in existingEntries)
            {
                LogEntries.Add(entry);
            }

            UpdateCounts();
        }

        private void UpdateCounts()
        {
            TotalEntries = LogEntries.Count;
            UpdateFilteredCount();
            LastUpdate = DateTime.Now;
        }

        private void UpdateFilteredCount()
        {
            FilteredCount = FilteredLogEntries.Cast<LogEntry>().Count();
        }

        [RelayCommand]
        private void ClearLogs()
        {
            _loggingService.ClearLogs();
            LogEntries.Clear();
            UpdateCounts();
        }

        [RelayCommand]
        private void Refresh()
        {
            LogEntries.Clear();
            LoadExistingEntries();
        }

        public void Receive(LogEntryMessage message)
        {
            // Add new log entry on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(message.Entry);
                UpdateCounts();

                // Auto-scroll to bottom if enabled
                if (AutoScroll)
                {
                    // This will be handled by the code-behind
                    OnAutoScrollRequested();
                }
            });
        }

        public event EventHandler? AutoScrollRequested;
        
        private void OnAutoScrollRequested()
        {
            AutoScrollRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}