using System.Windows.Controls;

namespace ALGAE.Services
{
    public partial class WarningConfirmationDialog : UserControl
    {
        public string Title { get; set; } = "Warning";
        public string Message { get; set; } = "";
        public string ConfirmText { get; set; } = "Yes";
        public string CancelText { get; set; } = "No";

        public WarningConfirmationDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
