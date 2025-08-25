using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using ALGAE.Services;
using ALGAE.Views;

namespace ALGAE.ViewModels
{
    public partial class GameDetailViewModel : ObservableObject
    {
        private readonly IGameRepository _gameRepository;
        private readonly IProfilesRepository _profilesRepository;
        private readonly ICompanionRepository _companionRepository;
        private readonly ICompanionProfileRepository _companionProfileRepository;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompanionLaunchService _companionLaunchService;
        private readonly IGameProcessMonitorService _processMonitor;

        [ObservableProperty]
        private Game? _game;

        [ObservableProperty]
        private ObservableCollection<Profile> _profiles = new();

        [ObservableProperty]
        private ObservableCollection<Companion> _companions = new();

        [ObservableProperty]
        private Profile? _selectedProfile;

        [ObservableProperty]
        private Companion? _selectedCompanion;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasProfiles;

        [ObservableProperty]
        private bool _hasCompanions;

        public GameDetailViewModel(
            IGameRepository gameRepository,
            IProfilesRepository profilesRepository,
            ICompanionRepository companionRepository,
            ICompanionProfileRepository companionProfileRepository,
            INotificationService notificationService,
            IServiceProvider serviceProvider,
            ICompanionLaunchService companionLaunchService,
            IGameProcessMonitorService processMonitor)
        {
            _gameRepository = gameRepository;
            _profilesRepository = profilesRepository;
            _companionRepository = companionRepository;
            _companionProfileRepository = companionProfileRepository;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;
            _companionLaunchService = companionLaunchService;
            _processMonitor = processMonitor;

            Profiles.CollectionChanged += (s, e) => HasProfiles = Profiles.Count > 0;
            Companions.CollectionChanged += (s, e) => HasCompanions = Companions.Count > 0;
        }

        public async Task LoadGameAsync(int gameId)
        {
            try
            {
                IsLoading = true;
                Game = await _gameRepository.GetByIdAsync(gameId);
                
                if (Game != null)
                {
                    await LoadProfilesAsync();
                    await LoadCompanionsAsync();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading game details: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProfilesAsync()
        {
            if (Game == null) return;

            try
            {
                var profiles = await _profilesRepository.GetAllByGameIdAsync(Game.GameId);
                Profiles.Clear();
                foreach (var profile in profiles)
                {
                    Profiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading profiles: {ex.Message}");
            }
        }

        private async Task LoadCompanionsAsync()
        {
            try
            {
                // Load companions for this specific game (game-specific + global)
                if (Game != null)
                {
                    var companions = await _companionRepository.GetForGameAsync(Game.GameId);
                    Companions.Clear();
                    foreach (var companion in companions)
                    {
                        Companions.Add(companion);
                    }
                }
                else
                {
                    // Fallback to all companions if no game is set
                    var companions = await _companionRepository.GetAllAsync();
                    Companions.Clear();
                    foreach (var companion in companions)
                    {
                        Companions.Add(companion);
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading companions: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddProfileAsync()
        {
            if (Game == null) return;

            var viewModel = new AddEditProfileViewModel(_companionRepository, _companionProfileRepository);
            await viewModel.LoadCompanionsForGameAsync(Game.GameId);

            var dialog = new AddEditProfileDialog();
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                try
                {
                    var profile = dialog.Result;
                    await _profilesRepository.AddAsync(profile);
                    
                    // Save companion associations
                    await viewModel.SaveCompanionAssociationsAsync(profile.ProfileId);
                    
                    Profiles.Add(profile);
                    _notificationService.ShowSuccess("Profile added successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error adding profile: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task EditProfileAsync(Profile? profile)
        {
            if (profile == null || Game == null) return;

            var viewModel = new AddEditProfileViewModel(_companionRepository, _companionProfileRepository);
            await viewModel.LoadCompanionsForGameAsync(Game.GameId, profile.ProfileId);
            viewModel.LoadProfile(profile);

            var dialog = new AddEditProfileDialog();
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                try
                {
                    var editProfile = dialog.Result;
                    await _profilesRepository.UpdateAsync(editProfile);
                    
                    // Save companion associations
                    await viewModel.SaveCompanionAssociationsAsync(editProfile.ProfileId);
                    
                    var index = Profiles.IndexOf(profile);
                    if (index >= 0)
                    {
                        Profiles[index] = editProfile;
                    }
                    _notificationService.ShowSuccess("Profile updated successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error updating profile: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteProfileAsync(Profile? profile)
        {
            if (profile == null) return;

            var confirmed = await _notificationService.ShowWarningConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{profile.ProfileName}'?",
                "Delete", "Cancel");

            if (confirmed)
            {
                try
                {
                    await _profilesRepository.DeleteAsync(profile.ProfileId);
                    Profiles.Remove(profile);
                    _notificationService.ShowSuccess("Profile deleted successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error deleting profile: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task AddCompanionAsync()
        {
            var dialog = new AddEditCompanionDialog();
            var companion = new Companion();
            
            // Set the GameId for this companion (null means applies to all games)
            if (Game != null)
            {
                companion.GameId = Game.GameId;  // Game-specific companion
            }
            // If GameId remains null, it applies to all games (global companion)
            
            dialog.DataContext = companion;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _companionRepository.AddAsync(companion);
                    Companions.Add(companion);
                    _notificationService.ShowSuccess("Companion added successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error adding companion: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task EditCompanionAsync(Companion? companion)
        {
            if (companion == null) return;

            var dialog = new AddEditCompanionDialog();
            var editCompanion = new Companion
            {
                CompanionId = companion.CompanionId,
                GameId = companion.GameId,
                Name = companion.Name,
                Type = companion.Type,
                PathOrURL = companion.PathOrURL,
                LaunchHelper = companion.LaunchHelper,
                Browser = companion.Browser,
                OpenInNewWindow = companion.OpenInNewWindow
            };
            dialog.DataContext = editCompanion;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _companionRepository.UpdateAsync(editCompanion);
                    var index = Companions.IndexOf(companion);
                    if (index >= 0)
                    {
                        Companions[index] = editCompanion;
                    }
                    _notificationService.ShowSuccess("Companion updated successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error updating companion: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteCompanionAsync(Companion? companion)
        {
            if (companion == null) return;

            var confirmed = await _notificationService.ShowWarningConfirmationAsync(
                "Delete Companion",
                $"Are you sure you want to delete the companion '{companion.Name}'?",
                "Delete", "Cancel");

            if (confirmed)
            {
                try
                {
                    await _companionRepository.DeleteAsync(companion.CompanionId);
                    Companions.Remove(companion);
                    _notificationService.ShowSuccess("Companion deleted successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error deleting companion: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task LaunchWithProfileAsync(Profile? profile)
        {
            if (Game == null) return;

            try
            {
                await LaunchGameWithProfileInternalAsync(Game, profile);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error launching game: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LaunchGameAsync()
        {
            if (Game == null) return;

            try
            {
                await LaunchGameWithProfileInternalAsync(Game, null);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error launching game: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal method to launch a game with a specific profile and its associated companions
        /// </summary>
        /// <param name="game">The game to launch</param>
        /// <param name="profile">The profile to use (null for default launch)</param>
        private async Task LaunchGameWithProfileInternalAsync(Game game, Profile? profile)
        {
            if (game == null) return;

            try
            {
                // Validate game executable exists
                var executablePath = Path.Combine(game.InstallPath, game.ExecutableName ?? "");
                
                if (string.IsNullOrEmpty(game.ExecutableName))
                {
                    _notificationService.ShowError($"No executable is specified for '{game.Name}'. Please edit the game settings.");
                    return;
                }

                if (!System.IO.File.Exists(executablePath))
                {
                    _notificationService.ShowError($"Game executable not found at: {executablePath}. Please check the game's installation path.");
                    return;
                }

                // Check if working directory exists
                var workingDir = game.GameWorkingPath ?? game.InstallPath;
                if (!System.IO.Directory.Exists(workingDir))
                {
                    _notificationService.ShowWarning($"Working directory not found: {workingDir}. Using executable directory instead.");
                    workingDir = System.IO.Path.GetDirectoryName(executablePath) ?? game.InstallPath;
                }

                // Determine arguments to use
                string gameArguments;
                if (profile != null && !string.IsNullOrEmpty(profile.CommandLineArgs))
                {
                    gameArguments = profile.CommandLineArgs;
                    System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Using profile '{profile.ProfileName}' arguments: {gameArguments}");
                }
                else
                {
                    gameArguments = game.GameArgs ?? "";
                    System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Using default game arguments: {gameArguments}");
                }

                // Launch companions first if using a profile
                if (profile != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Launching companions for profile {profile.ProfileId}");
                    try
                    {
                        var launchedCompanions = await _companionLaunchService.LaunchCompanionsForProfileAsync(profile.ProfileId);
                        System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Successfully launched {launchedCompanions.Count()} companions");
                    }
                    catch (Exception companionEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Error launching companions: {companionEx.Message}");
                        _notificationService.ShowWarning($"Some companions failed to start: {companionEx.Message}");
                    }
                }

                // Launch the game
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDir,
                    Arguments = gameArguments,
                    UseShellExecute = true
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                
                if (process != null)
                {
                    // Start monitoring the game process
                    _processMonitor.StartMonitoring(game, process);
                    
                    var profileInfo = profile != null ? $" with profile '{profile.ProfileName}'" : "";
                    _notificationService.ShowSuccess($"Successfully launched {game.Name}{profileInfo}");
                    System.Diagnostics.Debug.WriteLine($"Successfully launched {game.Name} (PID: {process.Id}){profileInfo}");
                }
                else
                {
                    _notificationService.ShowError($"Failed to start {game.Name}. The process could not be created.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LaunchGameWithProfileInternalAsync: Error launching {game.Name}: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (Game != null)
            {
                await LoadGameAsync(Game.GameId);
            }
        }
    }
}
