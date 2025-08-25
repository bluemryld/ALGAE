using System.Windows.Controls;

namespace ALGAE.Services
{
    public partial class ConfirmationDialog : UserControl
    {
        public string Title { get; set; } = "Confirmation";
        public string Message { get; set; } = "";
        public string ConfirmText { get; set; } = "Yes";
        public string CancelText { get; set; } = "No";

        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
