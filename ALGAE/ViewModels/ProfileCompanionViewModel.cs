using CommunityToolkit.Mvvm.ComponentModel;
using Algae.DAL.Models;

namespace ALGAE.ViewModels
{
    public partial class ProfileCompanionViewModel : ObservableObject
    {
        [ObservableProperty]
        private Companion companion;

        [ObservableProperty]
        private bool isEnabled;

        public ProfileCompanionViewModel(Companion companion, bool isEnabled = false)
        {
            this.companion = companion;
            this.isEnabled = isEnabled;
        }
    }
}
