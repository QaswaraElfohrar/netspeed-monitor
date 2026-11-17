using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.App.ViewModels;
using NetSpeedMonitor.Core.Formatting;

namespace NetSpeedMonitor.App.Views;

/// <summary>
/// The borderless, non-activating overlay that sits next to the clock.
/// It is a normal top-most window aligned to the taskbar – not a native taskbar component.
/// </summary>
public partial class WidgetWindow : Window
{
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_SETTINGCHANGE = 0x001A;
    private static readonly uint WM_TASKBARCREATED = Native.RegisterWindowMessage("TaskbarCreated");

    private readonly AppController _app;
    public WidgetViewModel ViewModel { get; } = new();
    public IntPtr Handle { get; private set; }

    public WidgetWindow(AppController app)
    {
        _app = app;
        InitializeComponent();
        DataContext = ViewModel;
        SourceInitialized += OnSourceInitialized;
        SizeChanged += (_, _) => _app.RepositionWidget();
        MouseLeftButtonDown += OnMouseDown;
        MouseRightButtonUp += OnRightClick;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        Handle = new WindowInteropHelper(this).Handle;
        int ex = Native.GetWindowLong(Handle, Native.GWL_EXSTYLE);
        ex = (ex | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE) & ~Native.WS_EX_APPWINDOW;
        Native.SetWindowLong(Handle, Native.GWL_EXSTYLE, ex);
        HwndSource.FromHwnd(Handle)?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(3); // MA_NOACTIVATE – never steal focus
        }

        if (msg == WM_TASKBARCREATED)
            _app.OnExplorerRestarted();
        else if (msg == WM_DISPLAYCHANGE || msg == WM_SETTINGCHANGE)
            _app.ScheduleReposition(600);

        return IntPtr.Zero;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            _app.ShowDashboard();
            return;
        }

        if (_app.Settings.LockPosition) return;
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (Native.GetWindowRect(Handle, out var r))
            _app.OnWidgetDragged(r.Left, r.Top);
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = _app.BuildWpfMenu();
        menu.PlacementTarget = this;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>Apply width option and a stable minimum text width so the widget doesn't jitter.</summary>
    public void UpdateMetrics()
    {
        var s = _app.Settings;
        bool vertical = ViewModel.WidgetOrientation == Orientation.Vertical;
        UpBlock.Margin = vertical ? new Thickness(0) : new Thickness(10, 0, 0, 0);

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        string number = s.Precision > 0 ? "8." + new string('8', s.Precision) : "888";
        string unit = s.Unit == "bits" ? "Mbps" : "MB/s";
        double w = MeasureText($"↓ {number} {unit}", dpi);
        DownBlock.MinWidth = w;
        UpBlock.MinWidth = w;

        if (s.WidgetWidth > 0)
        {
            SizeToContent = SizeToContent.Height;
            Width = s.WidgetWidth;
        }
        else
        {
            SizeToContent = SizeToContent.WidthAndHeight;
        }
    }

    private double MeasureText(string text, double pixelsPerDip)
    {
        var typeface = new Typeface(ViewModel.WidgetFont, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, ViewModel.WidgetFontSize, Brushes.Black, pixelsPerDip);
        return Math.Ceiling(ft.WidthIncludingTrailingWhitespace);
    }
}
