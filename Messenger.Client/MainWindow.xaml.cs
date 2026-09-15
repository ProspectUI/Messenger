using System.Windows;
using Messenger.Client.ViewModels;

namespace Messenger.Client;

public partial class MainWindow : Window
{
    private MainViewModel Vm { get; }

    public MainWindow(MainViewModel vm)
    {
        Vm = vm;
        DataContext = vm;
        InitializeComponent();

        vm.ScrollToBottomRequested += () =>
            Dispatcher.BeginInvoke(() => MessagesScroll.ScrollToBottom());

        Loaded += async (_, _) => await Vm.InitializeAsync();
    }
}
