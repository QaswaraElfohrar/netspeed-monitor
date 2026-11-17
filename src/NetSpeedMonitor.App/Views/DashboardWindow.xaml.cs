using System.ComponentModel;
using System.Windows;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.App.ViewModels;

namespace NetSpeedMonitor.App.Views;

public partial class DashboardWindow : Window
{
    private readonly AppController _app;
    private readonly DashboardViewModel _vm;

    public DashboardWindow(AppController app)
    {
        _app = app;
        InitializeComponent();
        _vm = new DashboardViewModel(app);
        DataContext = _vm;
        _vm.Sample += (d, u) => Graph.AddSample(d, u);
        SourceInitialized += (_, _) => ThemeService.AttachTitleBar(this);

        IsVisibleChanged += (_, _) =>
        {
            // only work while somebody can see the window
            if (IsVisible) _vm.Attach(); else _vm.Detach();
        };

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && _app.Settings.MinimizeToTray)
            {
                Hide();
                WindowState = WindowState.Normal;
            }
        };
    }

    public void RefreshUsage() => _vm.RefreshUsage();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_app.IsExiting)
        {
            e.Cancel = true; // closing the dashboard just hides it; the app lives in the tray
            Hide();
            return;
        }
        _vm.Detach();
        base.OnClosing(e);
    }
}
