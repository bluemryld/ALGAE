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
    }

    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }
}
