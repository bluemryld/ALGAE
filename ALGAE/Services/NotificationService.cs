using System.Threading.Tasks;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace ALGAE.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        public NotificationService(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue;
        }

        public void ShowSuccess(string message)
        {
            _snackbarMessageQueue.Enqueue(
                message,
                "OK",
                (obj) => { },
                null,
                false,
                true,
                TimeSpan.FromSeconds(4));
        }

        public void ShowInformation(string message)
        {
            _snackbarMessageQueue.Enqueue(
                message,
                "OK",
                (obj) => { },
                null,
                false,
                true,
                TimeSpan.FromSeconds(4));
        }

        public void ShowWarning(string message)
        {
            _snackbarMessageQueue.Enqueue(
                message,
                "DISMISS",
                (obj) => { },
                null,
                false,
                true,
                TimeSpan.FromSeconds(6));
        }

        public void ShowError(string message)
        {
            _snackbarMessageQueue.Enqueue(
                message,
                "DISMISS",
                (obj) => { },
                null,
                false,
                true,
                TimeSpan.FromSeconds(8));
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Yes", string cancelText = "No")
        {
            var dialog = new ConfirmationDialog
            {
                Title = title,
                Message = message,
                ConfirmText = confirmText,
                CancelText = cancelText
            };

            var result = await DialogHost.Show(dialog, "RootDialog");
            return result is bool boolResult && boolResult;
        }

        public async Task<bool> ShowWarningConfirmationAsync(string title, string message, string confirmText = "Yes", string cancelText = "No")
        {
            var dialog = new WarningConfirmationDialog
            {
                Title = title,
                Message = message,
                ConfirmText = confirmText,
                CancelText = cancelText
            };

            var result = await DialogHost.Show(dialog, "RootDialog");
            return result is bool boolResult && boolResult;
        }
    }
}
