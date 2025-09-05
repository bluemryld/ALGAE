using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.DAL.Repositories;
using Algae.DAL.Models;
using System.Text.Json;
using System.Net.Http;
using System.IO;
using Microsoft.Win32;
using ALGAE.Services;

namespace ALGAE.ViewModels
{
    public partial class GameSignaturesViewModel : ObservableObject
    {
        private readonly IGameSignatureRepository _gameSignatureRepository;
        private readonly IGameSignatureService _gameSignatureService;
        
        [ObservableProperty]
        private ObservableCollection<GameSignature> _signatures = new();
        
        [ObservableProperty]
        private ICollectionView _signaturesView;
        
        [ObservableProperty]
        private GameSignature? _selectedSignature;
        
        [ObservableProperty]
        private GameSignature _editingSignature = new();
        
        [ObservableProperty]
        private bool _isAddingNew = false;
        
        [ObservableProperty]
        private bool _isEditing = false;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private bool _isDownloading = false;
        
        [ObservableProperty]
        private string _searchText = string.Empty;
        
        [ObservableProperty]
        private string _statusMessage = "Ready";
        
        [ObservableProperty]
        private int _totalSignatures;
        
        [ObservableProperty]
        private int _filteredSignatures;

        private const string GITHUB_API_BASE = "https://api.github.com/repos";
        private const string SIGNATURES_REPO = "your-username/algae-signatures"; // TODO: Update with actual repo
        private const string SIGNATURES_FILE_PATH = "signatures/game_signatures.json";
        
        public GameSignaturesViewModel(IGameSignatureRepository gameSignatureRepository, IGameSignatureService gameSignatureService)
        {
            _gameSignatureRepository = gameSignatureRepository;
            _gameSignatureService = gameSignatureService;
            
            // Initialize collection view for filtering and sorting
            _signaturesView = CollectionViewSource.GetDefaultView(Signatures);
            _signaturesView.Filter = FilterSignatures;
            _signaturesView.SortDescriptions.Add(new SortDescription(nameof(GameSignature.Name), ListSortDirection.Ascending));
            
            PropertyChanged += OnSearchTextChanged;
            
            // Load signatures on initialization
            _ = LoadSignaturesAsync();
        }
        
        private void OnSearchTextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                SignaturesView.Refresh();
                FilteredSignatures = SignaturesView.Cast<GameSignature>().Count();
            }
        }
        
        private bool FilterSignatures(object obj)
        {
            if (obj is not GameSignature signature)
                return false;
                
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;
                
            var searchTerm = SearchText.ToLower();
            return signature.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                   signature.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                   signature.ExecutableName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                   (signature.Publisher?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        
        [RelayCommand]
        private async Task LoadSignaturesAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading signatures...";
            
            try
            {
                var signatures = await _gameSignatureRepository.GetAllAsync();
                Signatures.Clear();
                
                foreach (var signature in signatures)
                {
                    Signatures.Add(signature);
                }
                
                TotalSignatures = Signatures.Count;
                FilteredSignatures = Signatures.Count;
                StatusMessage = $"Loaded {TotalSignatures} signatures";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading signatures: {ex.Message}";
                MessageBox.Show($"Error loading signatures: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private void StartAddNew()
        {
            EditingSignature = new GameSignature
            {
                MatchName = true,
                MatchVersion = false, // Default to false to prevent update issues
                MatchPublisher = true
            };
            IsAddingNew = true;
            IsEditing = true;
        }
        
        [RelayCommand]
        private void StartEdit()
        {
            if (SelectedSignature == null) return;
            
            // Create a copy for editing
            EditingSignature = new GameSignature
            {
                GameSignatureId = SelectedSignature.GameSignatureId,
                ShortName = SelectedSignature.ShortName,
                Name = SelectedSignature.Name,
                Description = SelectedSignature.Description,
                GameImage = SelectedSignature.GameImage,
                ThemeName = SelectedSignature.ThemeName,
                ExecutableName = SelectedSignature.ExecutableName,
                GameArgs = SelectedSignature.GameArgs,
                Version = SelectedSignature.Version,
                Publisher = SelectedSignature.Publisher,
                MetaName = SelectedSignature.MetaName,
                MatchName = SelectedSignature.MatchName,
                MatchVersion = SelectedSignature.MatchVersion,
                MatchPublisher = SelectedSignature.MatchPublisher
            };
            
            IsAddingNew = false;
            IsEditing = true;
        }
        
        [RelayCommand]
        private async Task SaveSignatureAsync()
        {
            if (string.IsNullOrWhiteSpace(EditingSignature.Name) ||
                string.IsNullOrWhiteSpace(EditingSignature.ExecutableName))
            {
                MessageBox.Show("Name and Executable Name are required fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                if (IsAddingNew)
                {
                    await _gameSignatureRepository.AddAsync(EditingSignature);
                    StatusMessage = $"Added signature: {EditingSignature.Name}";
                }
                else
                {
                    await _gameSignatureRepository.UpdateAsync(EditingSignature);
                    StatusMessage = $"Updated signature: {EditingSignature.Name}";
                }
                
                await LoadSignaturesAsync();
                CancelEdit();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving signature: {ex.Message}";
                MessageBox.Show($"Error saving signature: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            IsAddingNew = false;
            EditingSignature = new GameSignature();
        }
        
        [RelayCommand]
        private async Task DeleteSignatureAsync()
        {
            if (SelectedSignature == null) return;
            
            var result = MessageBox.Show($"Are you sure you want to delete the signature for '{SelectedSignature.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _gameSignatureRepository.DeleteAsync(SelectedSignature.GameSignatureId);
                    StatusMessage = $"Deleted signature: {SelectedSignature.Name}";
                    await LoadSignaturesAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting signature: {ex.Message}";
                    MessageBox.Show($"Error deleting signature: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        [RelayCommand]
        private async Task DownloadLatestSignaturesAsync()
        {
            IsDownloading = true;
            StatusMessage = "Downloading latest signatures from GitHub...";
            
            try
            {
                var downloadedSignatures = await _gameSignatureService.DownloadLatestSignaturesAsync();
                var signaturesList = downloadedSignatures.ToList();
                
                if (signaturesList.Count > 0)
                {
                    // Ask user about merge strategy
                    var result = MessageBox.Show($"Downloaded {signaturesList.Count} signatures. How would you like to proceed?\n\n" +
                        "Yes = Replace all existing signatures\n" +
                        "No = Merge with existing signatures\n" +
                        "Cancel = Cancel import",
                        "Import Strategy", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Cancel)
                        return;
                    
                    var mergeMode = result == MessageBoxResult.No;
                    await ImportSignaturesAsync(signaturesList, mergeMode);
                    
                    StatusMessage = $"Successfully imported {signaturesList.Count} signatures from GitHub";
                }
                else
                {
                    StatusMessage = "No signatures found in downloaded file";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error downloading signatures: {ex.Message}";
                MessageBox.Show($"Error downloading latest signatures: {ex.Message}", "Download Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsDownloading = false;
            }
        }
        
        [RelayCommand]
        private async Task ImportFromFileAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import Game Signatures",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(openFileDialog.FileName);
                    var signatures = JsonSerializer.Deserialize<List<GameSignature>>(jsonContent);
                    
                    if (signatures?.Count > 0)
                    {
                        var result = MessageBox.Show($"Found {signatures.Count} signatures in file. Merge with existing signatures?",
                            "Import Signatures", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Cancel)
                            return;
                        
                        var mergeMode = result == MessageBoxResult.Yes;
                        await ImportSignaturesAsync(signatures, mergeMode);
                        
                        StatusMessage = $"Imported {signatures.Count} signatures from file";
                    }
                    else
                    {
                        MessageBox.Show("No valid signatures found in file.", "Import Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error importing signatures: {ex.Message}";
                    MessageBox.Show($"Error importing signatures: {ex.Message}", "Import Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        [RelayCommand]
        private async Task ExportToFileAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Export Game Signatures",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"algae_signatures_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    
                    // Export all signatures or just filtered ones?
                    var signaturesToExport = string.IsNullOrWhiteSpace(SearchText) 
                        ? Signatures.ToList()
                        : SignaturesView.Cast<GameSignature>().ToList();
                    
                    var json = JsonSerializer.Serialize(signaturesToExport, options);
                    await File.WriteAllTextAsync(saveFileDialog.FileName, json);
                    
                    StatusMessage = $"Exported {signaturesToExport.Count} signatures to {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Successfully exported {signaturesToExport.Count} signatures.", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting signatures: {ex.Message}";
                    MessageBox.Show($"Error exporting signatures: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private async Task ImportSignaturesAsync(List<GameSignature> signatures, bool mergeMode)
        {
            StatusMessage = "Importing signatures...";
            
            try
            {
                var importCount = 0;
                var skipCount = 0;
                
                foreach (var signature in signatures)
                {
                    if (mergeMode)
                    {
                        // Check if signature already exists (by name + executable)
                        var existing = Signatures.FirstOrDefault(s => 
                            s.Name.Equals(signature.Name, StringComparison.OrdinalIgnoreCase) &&
                            s.ExecutableName.Equals(signature.ExecutableName, StringComparison.OrdinalIgnoreCase));
                        
                        if (existing != null)
                        {
                            skipCount++;
                            continue; // Skip existing signatures in merge mode
                        }
                    }
                    
                    // Reset ID for new import
                    signature.GameSignatureId = 0;
                    
                    await _gameSignatureRepository.AddAsync(signature);
                    importCount++;
                }
                
                await LoadSignaturesAsync();
                StatusMessage = $"Import complete: {importCount} added, {skipCount} skipped";
                
                if (skipCount > 0)
                {
                    MessageBox.Show($"Import complete!\n\nAdded: {importCount} signatures\nSkipped: {skipCount} duplicates",
                        "Import Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import failed: {ex.Message}";
                throw;
            }
        }
        
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }
        
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            // Update can execute states for commands
            if (e.PropertyName == nameof(SelectedSignature))
            {
                StartEditCommand.NotifyCanExecuteChanged();
                DeleteSignatureCommand.NotifyCanExecuteChanged();
            }
        }
        
        public bool CanStartEdit => SelectedSignature != null && !IsEditing;
        public bool CanDeleteSignature => SelectedSignature != null && !IsEditing;
        
        private class GitHubFileResponse
        {
            public string? Content { get; set; }
            public string? Encoding { get; set; }
        }
    }
}
