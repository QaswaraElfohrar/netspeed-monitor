using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using NetSpeedMonitor.App.Views;
using NetSpeedMonitor.Core.Diagnostics;
using NetSpeedMonitor.Core.Formatting;
using NetSpeedMonitor.Core.Localization;
using NetSpeedMonitor.Core.Monitoring;
using NetSpeedMonitor.Core.Settings;
using NetSpeedMonitor.Core.Storage;

namespace NetSpeedMonitor.App.Services;

public sealed record MenuEntry(Func<string> Text, Action Action, Func<bool>? IsChecked = null, bool SeparatorBefore = false);

/// <summary>Owns every long-lived piece: settings, monitor, history, tray, widget and the windows.</summary>
public sealed class AppController : IDisposable
{
    private readonly Dispatcher _ui = Application.Current.Dispatcher;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private readonly DispatcherTimer _repositionTimer = new();
    private long _lastWidgetTick;
    private bool _taskbarVertical;
    private bool _exiting;
    private bool _disposed;

    public AppSettings Settings { get; private set; }
    public HistoryStore History { get; }
    public NetworkMonitor Monitor { get; }
    public MonitorSnapshot? Last { get; private set; }

    public WidgetWindow? Widget { get; private set; }
    public DashboardWindow? Dashboard { get; private set; }
    private SettingsWindow? _settingsWindow;
    private TrayService? _tray;

    public IReadOnlyList<MenuEntry> MenuEntries { get; }

    /// <summary>Raised on the UI thread for every monitor sample.</summary>
    public event Action<MonitorSnapshot>? SnapshotReceived;

    public AppController()
    {
        Settings = SettingsStore.Load();
        Settings.StartWithWindows = SystemIntegration.IsStartupEnabled();
        Loc.Instance.SetLanguage(Settings.Language);
        History = new HistoryStore();
        Monitor = new NetworkMonitor(History);
        Monitor.Configure(Settings.AutoAdapter, Settings.AdapterId, Settings.RefreshIntervalMs);

        MenuEntries = new List<MenuEntry>
        {
            new(() => Loc.Instance["menu.toggle"], ToggleWidget),
            new(() => Loc.Instance["menu.stats"], ShowDashboard),
            new(() => Loc.Instance["menu.settings"], ShowSettings),
            new(() => Loc.Instance["menu.reset"], ResetStatistics),
            new(() => Loc.Instance["menu.resetpos"], ResetWidgetPosition),
            new(() => Loc.Instance["menu.startup"], ToggleStartup, () => Settings.StartWithWindows, true),
            new(() => Loc.Instance["menu.about"], ShowAbout, null, true),
            new(() => Loc.Instance["menu.exit"], Exit),
        };
    }

    public void Start()
    {
        ThemeService.Apply(Settings);

        _tray = new TrayService(this);

        Widget = new WidgetWindow(this);
        ApplyWidgetSettings();
        Widget.Show();
        Widget.Visibility = Settings.WidgetVisible ? Visibility.Visible : Visibility.Collapsed;
        RepositionWidget();

        Monitor.SnapshotReady += OnSnapshotFromThread;
        Monitor.Start();

        _saveTimer.Tick += (_, _) =>
        {
            Task.Run(History.SaveIfDirty);
            if (Dashboard is not { IsVisible: true }) MemoryTrimmer.Trim();
        };
        _saveTimer.Start();

        var warmup = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        warmup.Tick += (_, _) =>
        {
            warmup.Stop();
            MemoryTrimmer.Trim(); // drop startup/JIT garbage once the app has settled
        };
        warmup.Start();

        _repositionTimer.Tick += (_, _) =>
        {
            _repositionTimer.Stop();
            ApplyWidgetSettings();
            RepositionWidget();
        };

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        if (!Settings.StartMinimized) ShowDashboard();
        if (Environment.GetCommandLineArgs().Contains("--about")) _ui.BeginInvoke(ShowAbout);
    }

    // ---------------------------------------------------------------- monitoring

    private void OnSnapshotFromThread(MonitorSnapshot s)
        => _ui.BeginInvoke(DispatcherPriority.Normal, () => OnSnapshot(s));

    private void OnSnapshot(MonitorSnapshot s)
    {
        if (_disposed) return;
        Last = s;
        Widget?.ViewModel.Update(s.DownBytesPerSec, s.UpBytesPerSec);

        bool bits = Settings.Unit == "bits";
        _tray?.SetTooltip(
            SpeedFormatter.FormatSpeed(s.DownBytesPerSec, bits, Settings.Precision),
            SpeedFormatter.FormatSpeed(s.UpBytesPerSec, bits, Settings.Precision));

        // keep the widget glued to the taskbar (re-assert top-most, follow layout changes)
        long now = Stopwatch.GetTimestamp();
        if ((now - _lastWidgetTick) / (double)Stopwatch.Frequency >= 1)
        {
            _lastWidgetTick = now;
            RepositionWidget();
        }

        SnapshotReceived?.Invoke(s);
    }

    // ---------------------------------------------------------------- widget

    public void ApplyWidgetSettings()
    {
        if (Widget == null) return;
        Widget.ViewModel.Apply(Settings, SystemIntegration.TaskbarUsesLightTheme(), _taskbarVertical);
        Widget.UpdateMetrics();
    }

    public void RepositionWidget()
    {
        var w = Widget;
        if (w == null || w.Handle == IntPtr.Zero) return;

        bool wantVisible = Settings.WidgetVisible;
        int x, y;
        bool hide = false;

        if (!Native.GetWindowRect(w.Handle, out var wr)) return;
        int ww = wr.Width, wh = wr.Height;

        if (!Settings.AutoPosition)
        {
            var rect = new RECT { Left = Settings.ManualX, Top = Settings.ManualY, Right = Settings.ManualX + ww, Bottom = Settings.ManualY + wh };
            var mon = Native.MonitorFromRect(ref rect, Native.MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            x = Settings.ManualX;
            y = Settings.ManualY;
            if (Native.GetMonitorInfo(mon, ref mi))
            {
                x = Math.Clamp(x, mi.rcMonitor.Left, Math.Max(mi.rcMonitor.Left, mi.rcMonitor.Right - ww));
                y = Math.Clamp(y, mi.rcMonitor.Top, Math.Max(mi.rcMonitor.Top, mi.rcMonitor.Bottom - wh));
            }
        }
        else
        {
            IntPtr tb = Native.FindWindow("Shell_TrayWnd", null);
            if (tb == IntPtr.Zero || !Native.GetWindowRect(tb, out var tr))
            {
                // Explorer is not running (yet): wait, keep hidden.
                hide = true;
                x = wr.Left;
                y = wr.Top;
            }
            else
            {
                var mon = Native.MonitorFromWindow(tb, Native.MONITOR_DEFAULTTONEAREST);
                var mi = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
                bool haveMon = Native.GetMonitorInfo(mon, ref mi);

                bool horizontal = tr.Width >= tr.Height;
                if (_taskbarVertical == horizontal)
                {
                    _taskbarVertical = !horizontal;
                    _ui.BeginInvoke(() => { ApplyWidgetSettings(); RepositionWidget(); });
                }

                if (haveMon)
                {
                    int visible = horizontal
                        ? Math.Min(tr.Bottom, mi.rcMonitor.Bottom) - Math.Max(tr.Top, mi.rcMonitor.Top)
                        : Math.Min(tr.Right, mi.rcMonitor.Right) - Math.Max(tr.Left, mi.rcMonitor.Left);
                    if (visible < 8) hide = true; // taskbar is auto-hidden right now
                }

                IntPtr notify = Native.FindWindowEx(tb, IntPtr.Zero, "TrayNotifyWnd", null);
                bool haveNotify = notify != IntPtr.Zero && Native.GetWindowRect(notify, out var nr) && nr.Width > 0 && nr.Height > 0;
                RECT notifyRect = default;
                if (haveNotify) Native.GetWindowRect(notify, out notifyRect);

                const int gap = 4;
                if (horizontal)
                {
                    x = (haveNotify ? notifyRect.Left : tr.Right - 190) - ww - gap;
                    x = Math.Max(x, tr.Left);
                    y = tr.Top + (tr.Height - wh) / 2;
                }
                else
                {
                    x = tr.Left + (tr.Width - ww) / 2;
                    y = (haveNotify ? notifyRect.Top : tr.Bottom - 140) - wh - gap;
                    y = Math.Max(y, tr.Top);
                }
            }
        }

        // do not float above full-screen games / presentations
        if (Native.SHQueryUserNotificationState(out int state) == 0 && state is 2 or 3 or 4)
            hide = true;

        var wantState = wantVisible && !hide ? Visibility.Visible : Visibility.Collapsed;
        if (w.Visibility != wantState) w.Visibility = wantState;
        if (wantState != Visibility.Visible) return;

        bool topmost = Settings.AutoPosition || Settings.AlwaysOnTop;
        Native.SetWindowPos(w.Handle, topmost ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
            x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
    }

    public void ScheduleReposition(int delayMs)
    {
        _ui.BeginInvoke(() =>
        {
            _repositionTimer.Stop();
            _repositionTimer.Interval = TimeSpan.FromMilliseconds(delayMs);
            _repositionTimer.Start();
        });
    }

    public void OnExplorerRestarted()
    {
        Logger.Error("Explorer restarted - re-attaching the widget");
        ScheduleReposition(1500);
    }

    public void OnWidgetDragged(int x, int y)
    {
        var s = Settings.Clone();
        s.AutoPosition = false;
        s.ManualX = x;
        s.ManualY = y;
        ApplySettings(s);
    }

    public void ResetWidgetPosition()
    {
        var s = Settings.Clone();
        s.AutoPosition = true;
        ApplySettings(s);
    }

    public void ToggleWidget()
    {
        var s = Settings.Clone();
        s.WidgetVisible = !s.WidgetVisible;
        ApplySettings(s);
    }

    // ---------------------------------------------------------------- settings

    public void ApplySettings(AppSettings newSettings)
    {
        newSettings.Normalize();
        Settings = newSettings;
        SettingsStore.Save(Settings);

        Loc.Instance.SetLanguage(Settings.Language);
        ThemeService.Apply(Settings);

        if (SystemIntegration.IsStartupEnabled() != Settings.StartWithWindows)
            SystemIntegration.SetStartup(Settings.StartWithWindows);

        Monitor.Configure(Settings.AutoAdapter, Settings.AdapterId, Settings.RefreshIntervalMs);
        ApplyWidgetSettings();
        RepositionWidget();
    }

    private void ToggleStartup()
    {
        var s = Settings.Clone();
        s.StartWithWindows = !s.StartWithWindows;
        ApplySettings(s);
    }

    // ---------------------------------------------------------------- windows & dialogs

    public void ShowDashboard()
    {
        _ui.BeginInvoke(() =>
        {
            Dashboard ??= new DashboardWindow(this);
            if (!Dashboard.IsVisible) Dashboard.Show();
            if (Dashboard.WindowState == WindowState.Minimized) Dashboard.WindowState = WindowState.Normal;
            Dashboard.Topmost = true;   // brief topmost toggle reliably raises the window
            Dashboard.Topmost = false;
            Dashboard.Activate();
        });
    }

    public void ShowSettings()
    {
        _ui.BeginInvoke(() =>
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    public void ShowAbout()
    {
        new AboutWindow().ShowDialog();
    }

    public void ResetStatistics()
    {
        var options = Loc.Instance.IsRtl ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None;
        var r = MessageBox.Show(Loc.Instance["reset.confirm"], Loc.Instance["menu.reset"], MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No, options);
        if (r != MessageBoxResult.Yes) return;
        Monitor.ResetSession();
        History.Clear();
        Dashboard?.RefreshUsage();
    }

    public System.Windows.Controls.ContextMenu BuildWpfMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu
        {
            FlowDirection = Loc.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
        };
        foreach (var e in MenuEntries)
        {
            if (e.SeparatorBefore) menu.Items.Add(new Separator());
            var action = e.Action;
            var item = new MenuItem { Header = e.Text(), IsChecked = e.IsChecked?.Invoke() ?? false };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }
        return menu;
    }

    // ---------------------------------------------------------------- system events

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Monitor.ResetBaselines();
            ScheduleReposition(3000);
        }
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => ScheduleReposition(800);

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Window)
        {
            _ui.BeginInvoke(() =>
            {
                ThemeService.Apply(Settings);
                ApplyWidgetSettings();
                RepositionWidget();
            });
        }
    }

    public bool IsExiting => _exiting;

    public void Exit()
    {
        if (_exiting) return;
        _exiting = true;
        Dispose();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _saveTimer.Stop();
        Monitor.Dispose();
        History.SaveIfDirty();
        SettingsStore.Save(Settings);
        _tray?.Dispose();
        Widget?.Close();
        _settingsWindow?.Close();
        Dashboard?.Close();
    }
}
