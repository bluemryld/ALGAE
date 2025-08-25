using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;

namespace ALGAE.ViewModels
{
    public partial class AddEditProfileViewModel : ObservableObject
    {
        private readonly ICompanionRepository _companionRepository;
        private readonly ICompanionProfileRepository _companionProfileRepository;

        [ObservableProperty]
        private int profileId;

        [ObservableProperty]
        private int gameId;

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string? commandLineArgs;

        [ObservableProperty]
        private ObservableCollection<ProfileCompanionViewModel> availableCompanions = new();

        [ObservableProperty]
        private bool hasNoCompanions;

        public AddEditProfileViewModel(ICompanionRepository companionRepository, ICompanionProfileRepository companionProfileRepository)
        {
            _companionRepository = companionRepository;
            _companionProfileRepository = companionProfileRepository;
        }

        public async Task LoadCompanionsForGameAsync(int gameId, int? profileId = null)
        {
            this.GameId = gameId;
            this.ProfileId = profileId ?? 0;

            // Get companions for this specific game (game-specific + global companions)
            var companions = await _companionRepository.GetForGameAsync(gameId);
            
            // Get existing companion associations for this profile if editing
            var existingAssociations = profileId.HasValue 
                ? await _companionProfileRepository.GetByProfileIdAsync(profileId.Value)
                : new List<CompanionProfile>();

            AvailableCompanions.Clear();
            foreach (var companion in companions)
            {
                var association = existingAssociations.FirstOrDefault(cp => cp.CompanionId == companion.CompanionId);
                var viewModel = new ProfileCompanionViewModel(companion, association?.IsEnabled ?? false);
                AvailableCompanions.Add(viewModel);
            }

            HasNoCompanions = !AvailableCompanions.Any();
        }

        public void LoadProfile(Profile profile)
        {
            ProfileId = profile.ProfileId;
            GameId = profile.GameId;
            ProfileName = profile.ProfileName;
            CommandLineArgs = profile.CommandLineArgs;
        }

        public Profile ToProfile()
        {
            return new Profile
            {
                ProfileId = this.ProfileId,
                GameId = this.GameId,
                ProfileName = this.ProfileName,
                CommandLineArgs = this.CommandLineArgs
            };
        }

        public async Task SaveCompanionAssociationsAsync(int savedProfileId)
        {
            // Delete existing associations for this profile
            await _companionProfileRepository.DeleteAllByProfileIdAsync(savedProfileId);

            // Add new associations
            foreach (var companionVM in AvailableCompanions)
            {
                if (companionVM.IsEnabled)
                {
                    await _companionProfileRepository.AddAsync(savedProfileId, companionVM.Companion.CompanionId, true);
                }
            }
        }
    }
}
