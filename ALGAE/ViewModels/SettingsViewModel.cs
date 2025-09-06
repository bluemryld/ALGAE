using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algae.Core.Services;
using ALGAE.Services;
using Microsoft.Win32;

namespace ALGAE.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDatabaseManagementService _databaseService;
        private AppSettings _settings;
        
        [ObservableProperty]
        private ObservableCollection<DatabaseConfigurationViewModel> databases;
        
        [ObservableProperty]
        private DatabaseConfigurationViewModel? selectedDatabase;
        
        [ObservableProperty]
        private bool autoDownloadSignatures;
        
        [ObservableProperty]
        private bool showDatabaseSwitchPrompt;
        
        [ObservableProperty]
        private string theme;
        
        [ObservableProperty]
        private bool isLoading;

        public SettingsViewModel(IDatabaseManagementService databaseService)
        {
            _databaseService = databaseService;
            _settings = new AppSettings();
            databases = new ObservableCollection<DatabaseConfigurationViewModel>();
            theme = "Auto";
            
            _ = LoadSettingsAsync();
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                IsLoading = true;
                _settings = await _databaseService.LoadSettingsAsync();
                
                // Load general settings
                AutoDownloadSignatures = _settings.General.AutoDownloadSignatures;
                ShowDatabaseSwitchPrompt = _settings.General.ShowDatabaseSwitchPrompt;
                Theme = _settings.General.Theme;
                
                // Load databases
                Databases.Clear();
                _settings.Database.RefreshDatabaseSizes();
                
                foreach (var db in _settings.Database.Databases.OrderByDescending(d => d.LastUsedDate))
                {
                    var dbVm = new DatabaseConfigurationViewModel(db, _databaseService);
                    Databases.Add(dbVm);
                    
                    // Set selected database to current one
                    if (db.Id == _settings.Database.CurrentDatabaseId)
                    {
                        SelectedDatabase = dbVm;
                    }
                }
                
                // If no current database is set, select the default or first one
                if (SelectedDatabase == null && Databases.Any())
                {
                    var defaultDb = Databases.FirstOrDefault(d => d.IsDefault);
                    SelectedDatabase = defaultDb ?? Databases.First();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SwitchToSelectedDatabaseAsync()
        {
            if (SelectedDatabase == null) return;
            
            try
            {
                IsLoading = true;
                await _databaseService.SwitchDatabaseAsync(SelectedDatabase.Id);
                
                // Update UI to reflect the change
                await LoadSettingsAsync();
                
                MessageBox.Show($"Successfully switched to database: {SelectedDatabase.Name}", 
                    "Database Switched", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching database: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateNewDatabaseAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Create New Database",
                    Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                    DefaultExt = "db",
                    AddExtension = true,
                    InitialDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "AlgaeApp", "Database")
                };

                if (dialog.ShowDialog() == true)
                {
                    var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    var newDb = await _databaseService.CreateNewDatabaseAsync(fileName, dialog.FileName);
                    
                    // Reload settings to show the new database
                    await LoadSettingsAsync();
                    
                    // Select the new database
                    var newDbVm = Databases.FirstOrDefault(d => d.Id == newDb.Id);
                    if (newDbVm != null)
                    {
                        SelectedDatabase = newDbVm;
                    }
                    
                    MessageBox.Show($"Database '{fileName}' created successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating database: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SetAsDefaultDatabaseAsync(DatabaseConfigurationViewModel database)
        {
            try
            {
                _settings.Database.SetDefaultDatabase(database.Id);
                await _databaseService.SaveSettingsAsync(_settings);
                
                // Update all database view models
                foreach (var db in Databases)
                {
                    db.IsDefault = db.Id == database.Id;
                }
                
                MessageBox.Show($"'{database.Name}' set as default database.", "Default Set", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting default database: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ClearDatabaseAsync(DatabaseConfigurationViewModel database)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to clear all data from '{database.Name}'?\n\nThis will delete all games, companions, profiles, and signatures, but keep the database structure.",
                "Clear Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _databaseService.ClearDatabaseAsync(database.Id);
                    
                    if (success)
                    {
                        await LoadSettingsAsync();
                        MessageBox.Show($"Database '{database.Name}' cleared successfully!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to clear database '{database.Name}'.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing database: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        private async Task DeleteDatabaseAsync(DatabaseConfigurationViewModel database)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to permanently delete '{database.Name}'?\n\nThis will delete the database file and cannot be undone.",
                "Delete Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _databaseService.DeleteDatabaseAsync(database.Id);
                    
                    if (success)
                    {
                        await LoadSettingsAsync();
                        MessageBox.Show($"Database '{database.Name}' deleted successfully!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete database '{database.Name}'.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting database: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                // Update settings from UI
                _settings.General.AutoDownloadSignatures = AutoDownloadSignatures;
                _settings.General.ShowDatabaseSwitchPrompt = ShowDatabaseSwitchPrompt;
                _settings.General.Theme = Theme;
                
                await _databaseService.SaveSettingsAsync(_settings);
                
                MessageBox.Show("Settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshDatabasesAsync()
        {
            await LoadSettingsAsync();
        }
    }

    public class DatabaseConfigurationViewModel : ObservableObject
    {
        private readonly DatabaseConfiguration _config;
        private readonly IDatabaseManagementService _databaseService;
        
        public string Id => _config.Id;
        public string Name => _config.Name;
        public string FilePath => _config.FilePath;
        public string DisplayName => _config.DisplayName;
        public string SizeFormatted => _config.SizeFormatted;
        public bool Exists => _config.Exists;
        public DateTime LastUsedDate => _config.LastUsedDate;
        public bool IsDebugDatabase => _config.IsDebugDatabase;

        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        private int _gameCount;
        public int GameCount
        {
            get => _gameCount;
            set => SetProperty(ref _gameCount, value);
        }

        private int _companionCount;
        public int CompanionCount
        {
            get => _companionCount;
            set => SetProperty(ref _companionCount, value);
        }

        private bool _hasSignatures;
        public bool HasSignatures
        {
            get => _hasSignatures;
            set => SetProperty(ref _hasSignatures, value);
        }

        public DatabaseConfigurationViewModel(DatabaseConfiguration config, IDatabaseManagementService databaseService)
        {
            _config = config;
            _databaseService = databaseService;
            _isDefault = config.IsDefault;
            
            _ = LoadStatsAsync();
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                if (_config.Exists)
                {
                    GameCount = await _databaseService.GetGameCountAsync(_config.FilePath);
                    CompanionCount = await _databaseService.GetCompanionCountAsync(_config.FilePath);
                    HasSignatures = await _databaseService.HasSignaturesAsync(_config.FilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading database stats: {ex.Message}");
            }
        }
    }
}