using System.Windows;
using System.Windows.Threading;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.Core.Diagnostics;

namespace NetSpeedMonitor.App;

public partial class App : Application
{
    private const string MutexName = @"Local\NetSpeedMonitor.SingleInstance";
    private const string ShowEventName = @"Local\NetSpeedMonitor.ShowDashboard";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _registered;
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Latin digits everywhere (speeds are numbers + Latin units), even in the Arabic UI
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage("en-US")));

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error("Unhandled UI exception", args.Exception);
            args.Handled = true; // keep running; the monitor must survive a UI hiccup
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Logger.Error("Unhandled exception", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        // ---- single instance: a second launch just asks the first one to show its dashboard
        _mutex = new Mutex(true, MutexName, out bool first);
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        if (!first)
        {
            try
            {
                Native.AllowSetForegroundWindow(-1); // ASFW_ANY: let the running instance come to the front
                _showEvent.Set();
            }
            catch { /* ignore */ }
            Shutdown();
            return;
        }

        try
        {
            _controller = new AppController();
            _controller.Start();
            _registered = ThreadPool.RegisterWaitForSingleObject(_showEvent,
                (_, _) => _controller?.ShowDashboard(), null, Timeout.Infinite, false);
        }
        catch (Exception ex)
        {
            Logger.Error("Startup failed", ex);
            MessageBox.Show(ex.Message, "NetSpeed Monitor", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _registered?.Unregister(null);
        _controller?.Dispose();
        try { _mutex?.ReleaseMutex(); } catch { /* not owned */ }
        _mutex?.Dispose();
        _showEvent?.Dispose();
        base.OnExit(e);
    }
}
