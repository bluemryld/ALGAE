using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Algae.DAL.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for AddEditGameDialog.xaml
    /// </summary>
    public partial class AddEditGameDialog : Window
    {
        public Game? Game { get; private set; }

        public AddEditGameDialog(Game? existingGame = null)
        {
            InitializeComponent();
            var viewModel = new AddEditGameDialogViewModel(existingGame);
            viewModel.DialogResult += (result, game) => 
            {
                if (result)
                {
                    Game = game;
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
            };
            DataContext = viewModel;
        }
    }

    public partial class AddEditGameDialogViewModel : ObservableObject
    {
        private readonly Game? _existingGame;
        public event Action<bool, Game?>? DialogResult;

        [ObservableProperty]
        private string _gameName = string.Empty;

        [ObservableProperty]
        private string _shortName = string.Empty;

        [ObservableProperty]
        private string? _publisher;

        [ObservableProperty]
        private string? _version;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string _installPath = string.Empty;

        [ObservableProperty]
        private string? _gameWorkingPath;

        [ObservableProperty]
        private string _executableName = string.Empty;
        
        [ObservableProperty]
        private string _executableFullPath = string.Empty;

        [ObservableProperty]
        private string? _gameArgs;

        [ObservableProperty]
        private string? _themeName;

        [ObservableProperty]
        private ObservableCollection<string> _validationErrors = new();

        [ObservableProperty]
        private bool _hasValidationErrors;
        
        [ObservableProperty]
        private bool _hasWorkingPathWarning;
        
        private bool _isPopulatingFromFile = false;

        public string Title => _existingGame == null ? "Add Game" : "Edit Game";
        public string SaveButtonText => _existingGame == null ? "Add" : "Save";

        public AddEditGameDialogViewModel(Game? existingGame = null)
        {
            _existingGame = existingGame;
            
            if (_existingGame != null)
            {
                // Populate fields with existing game data
                GameName = _existingGame.Name;
                ShortName = _existingGame.ShortName;
                Publisher = _existingGame.Publisher;
                Version = _existingGame.Version;
                Description = _existingGame.Description;
                InstallPath = _existingGame.InstallPath;
                GameWorkingPath = _existingGame.GameWorkingPath;
                ExecutableName = _existingGame.ExecutableName ?? string.Empty;
                ExecutableFullPath = !string.IsNullOrEmpty(InstallPath) && !string.IsNullOrEmpty(ExecutableName) 
                    ? Path.Combine(InstallPath, ExecutableName) 
                    : string.Empty;
                GameArgs = _existingGame.GameArgs;
                ThemeName = _existingGame.ThemeName;
            }
            
            // Auto-generate ShortName when GameName changes if ShortName is empty
            PropertyChanged += OnPropertyChanged;
        }
        
        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameName) && string.IsNullOrWhiteSpace(ShortName))
            {
                // Auto-generate a short name from the game name
                ShortName = GenerateShortName(GameName);
            }
            
            // Auto-extract metadata when ExecutableFullPath changes (but not during programmatic updates)
            if (e.PropertyName == nameof(ExecutableFullPath) && !string.IsNullOrWhiteSpace(ExecutableFullPath) && !_isPopulatingFromFile)
            {
                // Check if the path is valid and different from what we had before
                if (File.Exists(ExecutableFullPath))
                {
                    PopulateFromExecutableFile(ExecutableFullPath);
                }
            }
            
            // Clear validation errors when user starts typing in relevant fields
            if (HasValidationErrors && (e.PropertyName == nameof(GameName) || 
                                      e.PropertyName == nameof(InstallPath) || 
                                      e.PropertyName == nameof(ExecutableName) ||
                                      e.PropertyName == nameof(ExecutableFullPath)))
            {
                ValidationErrors.Clear();
                HasValidationErrors = false;
                HasWorkingPathWarning = false;
            }
        }
        
        private static string GenerateShortName(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
                return string.Empty;
                
            // Remove common words and create a short name
            var words = gameName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var filteredWords = words.Where(word => 
                !string.Equals(word, "the", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(word, "of", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(word, "and", StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            if (filteredWords.Count == 1)
                return filteredWords[0];
            else if (filteredWords.Count > 1)
                return string.Join("", filteredWords.Take(2).Select(w => w[0])).ToUpper() + 
                       string.Join("", filteredWords.Skip(2).Select(w => w.ToLower()));
            else
                return gameName.Length > 10 ? gameName.Substring(0, 10) : gameName;
        }

        [RelayCommand]
        private void BrowseInstallPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Game Install Folder"
            };

            if (!string.IsNullOrEmpty(InstallPath))
            {
                dialog.InitialDirectory = InstallPath;
            }

            if (dialog.ShowDialog() == true)
            {
                InstallPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseWorkingPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Working Directory"
            };

            if (!string.IsNullOrEmpty(GameWorkingPath))
            {
                dialog.InitialDirectory = GameWorkingPath;
            }
            else if (!string.IsNullOrEmpty(InstallPath))
            {
                dialog.InitialDirectory = InstallPath;
            }

            if (dialog.ShowDialog() == true)
            {
                GameWorkingPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseExecutable()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Game Executable",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (!string.IsNullOrEmpty(InstallPath))
            {
                dialog.InitialDirectory = InstallPath;
            }

            if (dialog.ShowDialog() == true)
            {
                var selectedFile = dialog.FileName;
                PopulateFromExecutableFile(selectedFile);
            }
        }
        
        private void PopulateFromExecutableFile(string executablePath)
        {
            try
            {
                if (!File.Exists(executablePath))
                    return;
                    
                var fileName = Path.GetFileName(executablePath);
                var directory = Path.GetDirectoryName(executablePath);
                
                // Set flag to prevent triggering property change events during population
                _isPopulatingFromFile = true;
                
                // Set the full path and filename
                ExecutableFullPath = executablePath;
                ExecutableName = fileName;
                
                // Set install path if empty
                if (string.IsNullOrEmpty(InstallPath) && !string.IsNullOrEmpty(directory))
                {
                    InstallPath = directory;
                }
                
                // Extract file metadata
                var fileInfo = new FileInfo(executablePath);
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                
                // Auto-populate Game Name if empty
                if (string.IsNullOrEmpty(GameName))
                {
                    // Try to get a clean game name from various sources
                    string gameName = GetCleanGameName(versionInfo, fileName);
                    if (!string.IsNullOrEmpty(gameName))
                    {
                        GameName = gameName;
                    }
                }
                
                // Auto-populate Publisher if empty
                if (string.IsNullOrEmpty(Publisher) && !string.IsNullOrEmpty(versionInfo.CompanyName))
                {
                    Publisher = versionInfo.CompanyName;
                }
                
                // Auto-populate Version if empty
                if (string.IsNullOrEmpty(Version))
                {
                    string version = GetCleanVersion(versionInfo);
                    if (!string.IsNullOrEmpty(version))
                    {
                        Version = version;
                    }
                }
                
                // Auto-populate Description if empty
                if (string.IsNullOrEmpty(Description) && !string.IsNullOrEmpty(versionInfo.FileDescription))
                {
                    Description = versionInfo.FileDescription;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting metadata from {executablePath}: {ex.Message}");
            }
            finally
            {
                // Clear flag
                _isPopulatingFromFile = false;
            }
        }
        
        private static string GetCleanGameName(FileVersionInfo versionInfo, string fileName)
        {
            // Priority order for game name extraction
            var candidates = new[]
            {
                versionInfo.ProductName,
                versionInfo.FileDescription,
                versionInfo.OriginalFilename,
                Path.GetFileNameWithoutExtension(fileName)
            };
            
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                    
                // Clean up common suffixes and prefixes
                string cleaned = candidate
                    .Replace(".exe", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Game", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Launcher", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Setup", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                
                if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length > 2)
                {
                    return cleaned;
                }
            }
            
            return string.Empty;
        }
        
        private static string GetCleanVersion(FileVersionInfo versionInfo)
        {
            // Try different version properties
            var candidates = new[]
            {
                versionInfo.ProductVersion,
                versionInfo.FileVersion,
                $"{versionInfo.ProductMajorPart}.{versionInfo.ProductMinorPart}.{versionInfo.ProductBuildPart}"
            };
            
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                    
                // Clean version string
                string cleaned = candidate.Trim();
                if (!string.IsNullOrEmpty(cleaned) && cleaned != "0.0.0" && !cleaned.StartsWith("0.0.0"))
                {
                    return cleaned;
                }
            }
            
            return string.Empty;
        }

        [RelayCommand]
        private void Save()
        {
            // Clear previous validation errors
            ValidationErrors.Clear();
            
            // Comprehensive validation with inline display
            if (string.IsNullOrWhiteSpace(GameName))
                ValidationErrors.Add("• Game Name is required");
                
            if (string.IsNullOrWhiteSpace(ExecutableFullPath))
                ValidationErrors.Add("• Game Executable is required");
            else if (!File.Exists(ExecutableFullPath))
                ValidationErrors.Add("• Game Executable file does not exist");
                
            if (string.IsNullOrWhiteSpace(InstallPath))
                ValidationErrors.Add("• Install Path is required");
            else if (!Directory.Exists(InstallPath))
                ValidationErrors.Add("• Install Path does not exist");
            
            // Update the HasValidationErrors property
            HasValidationErrors = ValidationErrors.Any();
            
            // If there are validation errors, stop here
            if (HasValidationErrors)
            {
                return;
            }

            // Additional validation for working path if specified
            if (!string.IsNullOrWhiteSpace(GameWorkingPath) && !Directory.Exists(GameWorkingPath))
            {
                // If this is the first time we encounter this warning, show it
                if (!HasWorkingPathWarning)
                {
                    ValidationErrors.Add($"• Warning: Working directory '{GameWorkingPath}' does not exist");
                    ValidationErrors.Add("• Click Save again to continue anyway");
                    HasValidationErrors = true;
                    HasWorkingPathWarning = true;
                    return;
                }
                // If user clicked Save again, proceed despite the warning
            }

            // Create or update game object
            var game = _existingGame ?? new Game();
            game.Name = GameName;
            game.ShortName = string.IsNullOrWhiteSpace(ShortName) ? GameName : ShortName;
            game.Publisher = Publisher;
            game.Version = Version;
            game.Description = Description;
            game.InstallPath = InstallPath;
            game.GameWorkingPath = GameWorkingPath;
            game.ExecutableName = ExecutableName;
            game.GameArgs = GameArgs;
            game.ThemeName = ThemeName;

            DialogResult?.Invoke(true, game);
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult?.Invoke(false, null);
        }
    }
}
