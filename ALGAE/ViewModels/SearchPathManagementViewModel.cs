using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ALGAE.ViewModels
{
    public class SearchPathManagementViewModel : INotifyPropertyChanged
    {
        private readonly ISearchPathRepository _searchPathRepository;
        private SearchPath? _selectedSearchPath;

        public SearchPathManagementViewModel(ISearchPathRepository searchPathRepository)
        {
            _searchPathRepository = searchPathRepository;
            SearchPaths = new ObservableCollection<SearchPath>();
            
            AddPathCommand = new RelayCommand(AddPath);
            RemoveSelectedPathCommand = new RelayCommand(RemoveSelectedPath, () => SelectedSearchPath != null);
            AddCommonPathsCommand = new RelayCommand(AddCommonPaths);
            
            _ = LoadSearchPathsAsync();
        }

        public ObservableCollection<SearchPath> SearchPaths { get; }

        public SearchPath? SelectedSearchPath
        {
            get => _selectedSearchPath;
            set
            {
                _selectedSearchPath = value;
                OnPropertyChanged(nameof(SelectedSearchPath));
                ((RelayCommand)RemoveSelectedPathCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand AddPathCommand { get; }
        public ICommand RemoveSelectedPathCommand { get; }
        public ICommand AddCommonPathsCommand { get; }

        private void AddPath()
        {
            // TODO: This is a temporary solution - we should implement a proper folder browser dialog
            // For now, suggest users to add common paths using the "Add Common Paths" button
            var result = MessageBox.Show(
                "To add a custom search path, please use the 'Add Common Paths' button which will automatically add standard game installation directories.\n\n" +
                "Would you like to add common game paths now?",
                "Add Search Path",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                AddCommonPaths();
            }
        }

        private async void RemoveSelectedPath()
        {
            if (SelectedSearchPath == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove this search path?\n\n{SelectedSearchPath.Path}",
                "Remove Search Path",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _searchPathRepository.DeleteAsync(SelectedSearchPath.SearchPathId);
                    SearchPaths.Remove(SelectedSearchPath);
                    SelectedSearchPath = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing search path: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AddCommonPaths()
        {
            var commonPaths = GetCommonGamePaths();
            var addedCount = 0;

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path) && !SearchPaths.Any(sp => string.Equals(sp.Path, path, StringComparison.OrdinalIgnoreCase)))
                {
                    var searchPath = new SearchPath
                    {
                        Path = path,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastScanned = null,
                        GamesFound = 0
                    };

                    try
                    {
                        await _searchPathRepository.AddAsync(searchPath);
                        SearchPaths.Add(searchPath);
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding search path '{path}': {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            if (addedCount > 0)
            {
                MessageBox.Show($"Added {addedCount} common game path(s).", "Paths Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No new common game paths were found or they were already in the list.", 
                    "No Paths Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task LoadSearchPathsAsync()
        {
            try
            {
                var searchPaths = await _searchPathRepository.GetAllAsync();
                SearchPaths.Clear();
                
                foreach (var searchPath in searchPaths)
                {
                    // Validate path exists and update status
                    searchPath.IsValid = Directory.Exists(searchPath.Path);
                    SearchPaths.Add(searchPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading search paths: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string[] GetCommonGamePaths()
        {
            return new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Steam\steamapps\common",
                @"C:\Program Files (x86)\Epic Games",
                @"C:\Program Files\Epic Games",
                @"C:\Program Files (x86)\GOG Galaxy\Games",
                @"C:\Program Files\GOG Galaxy\Games",
                @"C:\Program Files (x86)\Origin Games",
                @"C:\Program Files\Origin Games",
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
                @"C:\Program Files\Ubisoft\Ubisoft Game Launcher\games",
                @"C:\Program Files (x86)\Battle.net",
                @"C:\Program Files\Battle.net",
                @"C:\Program Files (x86)\Electronic Arts",
                @"C:\Program Files\Electronic Arts",
                @"C:\Program Files (x86)\Microsoft Games",
                @"C:\Program Files\Microsoft Games",
                @"C:\Games",
                @"D:\Games",
                @"E:\Games"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
