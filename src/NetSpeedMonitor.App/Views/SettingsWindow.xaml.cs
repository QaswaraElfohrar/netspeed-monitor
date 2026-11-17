using System.Windows;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.App.ViewModels;

namespace NetSpeedMonitor.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppController app)
    {
        InitializeComponent();
        var vm = new SettingsViewModel(app);
        vm.CloseRequested += Close;
        SourceInitialized += (_, _) => ThemeService.AttachTitleBar(this);
        DataContext = vm;
    }
}
