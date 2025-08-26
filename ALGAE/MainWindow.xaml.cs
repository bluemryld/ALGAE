using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using ALGAE.ViewModels;

namespace ALGAE;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public ISnackbarMessageQueue MessageQueue => MainSnackbar.MessageQueue;
    
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
        
        // Add proper cleanup on window closing
        Closing += MainWindow_Closing;
    }
    
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Ensure application shuts down completely when main window closes
        System.Diagnostics.Debug.WriteLine("MainWindow closing - requesting application shutdown");
        Application.Current.Shutdown();
    }

    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }
}
