using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow(LogViewerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Handle auto-scroll
            viewModel.AutoScrollRequested += (sender, e) =>
            {
                if (LogScrollViewer.VerticalOffset == LogScrollViewer.ScrollableHeight || 
                    LogScrollViewer.ScrollableHeight == 0)
                {
                    LogScrollViewer.ScrollToEnd();
                }
            };
        }
    }
}