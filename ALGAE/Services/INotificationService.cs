using System.Threading.Tasks;

namespace ALGAE.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Show a success message to the user
        /// </summary>
        void ShowSuccess(string message);

        /// <summary>
        /// Show an information message to the user
        /// </summary>
        void ShowInformation(string message);

        /// <summary>
        /// Show a warning message to the user
        /// </summary>
        void ShowWarning(string message);

        /// <summary>
        /// Show an error message to the user
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// Show a confirmation dialog and return the user's choice
        /// </summary>
        Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Yes", string cancelText = "No");

        /// <summary>
        /// Show a warning confirmation dialog and return the user's choice
        /// </summary>
        Task<bool> ShowWarningConfirmationAsync(string title, string message, string confirmText = "Yes", string cancelText = "No");
    }
}
