using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    public partial class SignatureDownloadPromptDialog : Window
    {
        public SignatureDownloadPromptDialogResult Result { get; private set; }
        public bool RememberChoice { get; private set; }
        public bool AutoDownloadForNewDatabases { get; private set; }

        public SignatureDownloadPromptDialog(string databaseName)
        {
            InitializeComponent();
            
            DataContext = new SignatureDownloadPromptViewModel
            {
                DatabaseMessage = $"Database: {databaseName}",
                RememberChoice = false,
                AutoDownloadForNewDatabases = true
            };
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SignatureDownloadPromptDialogResult.Download;
            CaptureSettings();
            DialogResult = true;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SignatureDownloadPromptDialogResult.Skip;
            CaptureSettings();
            DialogResult = false;
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SignatureDownloadPromptDialogResult.GoToSettings;
            CaptureSettings();
            DialogResult = null;
            Close();
        }

        private void CaptureSettings()
        {
            var viewModel = DataContext as SignatureDownloadPromptViewModel;
            RememberChoice = viewModel?.RememberChoice ?? false;
            AutoDownloadForNewDatabases = viewModel?.AutoDownloadForNewDatabases ?? true;
        }
    }

    public enum SignatureDownloadPromptDialogResult
    {
        Download,
        Skip,
        GoToSettings
    }

    public class SignatureDownloadPromptViewModel
    {
        public string DatabaseMessage { get; set; } = string.Empty;
        public bool RememberChoice { get; set; }
        public bool AutoDownloadForNewDatabases { get; set; }
    }
}