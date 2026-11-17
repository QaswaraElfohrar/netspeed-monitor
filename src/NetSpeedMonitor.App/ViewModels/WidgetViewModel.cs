using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.Core.Formatting;
using NetSpeedMonitor.Core.Settings;

namespace NetSpeedMonitor.App.ViewModels;

public sealed class WidgetViewModel : ViewModelBase
{
    private AppSettings _s = new();
    private string _downText = "↓ 0 B/s";
    private string _upText = "↑ 0 B/s";
    private Brush _downBrush = Brushes.LightGreen;
    private Brush _upBrush = Brushes.LightSkyBlue;
    private Brush _background = Brushes.Transparent;
    private FontFamily _fontFamily = new("Segoe UI");
    private double _fontSize = 12;
    private Visibility _downVisibility = Visibility.Visible;
    private Visibility _upVisibility = Visibility.Visible;
    private Orientation _orientation = Orientation.Horizontal;
    private double _lastDown, _lastUp;

    public string DownText { get => _downText; private set => Set(ref _downText, value); }
    public string UpText { get => _upText; private set => Set(ref _upText, value); }
    public Brush DownBrush { get => _downBrush; private set => Set(ref _downBrush, value); }
    public Brush UpBrush { get => _upBrush; private set => Set(ref _upBrush, value); }
    public Brush WidgetBackground { get => _background; private set => Set(ref _background, value); }
    public FontFamily WidgetFont { get => _fontFamily; private set => Set(ref _fontFamily, value); }
    public double WidgetFontSize { get => _fontSize; private set => Set(ref _fontSize, value); }
    public Visibility DownVisibility { get => _downVisibility; private set => Set(ref _downVisibility, value); }
    public Visibility UpVisibility { get => _upVisibility; private set => Set(ref _upVisibility, value); }
    public Orientation WidgetOrientation { get => _orientation; private set => Set(ref _orientation, value); }

    /// <param name="forceStacked">true when the taskbar is vertical (left/right edge).</param>
    public void Apply(AppSettings s, bool lightTaskbar, bool forceStacked)
    {
        _s = s;
        DownBrush = ColorUtil.Brush(ColorUtil.Parse(s.DownloadColor, Color.FromRgb(0x2E, 0xCC, 0x71)));
        UpBrush = ColorUtil.Brush(ColorUtil.Parse(s.UploadColor, Color.FromRgb(0x34, 0x98, 0xDB)));

        bool light = s.Theme == "light" || (s.Theme == "system" && lightTaskbar);
        Color baseColor = ColorUtil.Parse(s.BackgroundColor, light ? Color.FromRgb(0xF3, 0xF3, 0xF3) : Color.FromRgb(0x20, 0x20, 0x20));
        // alpha 1 (not 0) keeps the transparent widget clickable/draggable
        byte alpha = s.TransparentBackground ? (byte)1 : (byte)Math.Round(s.BackgroundOpacity * 255);
        WidgetBackground = ColorUtil.Brush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));

        try { WidgetFont = new FontFamily(s.FontFamily); } catch { WidgetFont = new FontFamily("Segoe UI"); }
        WidgetFontSize = s.FontSize;
        DownVisibility = s.ShowDownload ? Visibility.Visible : Visibility.Collapsed;
        UpVisibility = s.ShowUpload ? Visibility.Visible : Visibility.Collapsed;
        WidgetOrientation = forceStacked || s.Layout == "vertical" ? Orientation.Vertical : Orientation.Horizontal;
        Update(_lastDown, _lastUp);
    }

    public void Update(double downBytesPerSec, double upBytesPerSec)
    {
        _lastDown = downBytesPerSec;
        _lastUp = upBytesPerSec;
        bool bits = _s.Unit == "bits";
        DownText = "↓ " + SpeedFormatter.FormatSpeed(downBytesPerSec, bits, _s.Precision);
        UpText = "↑ " + SpeedFormatter.FormatSpeed(upBytesPerSec, bits, _s.Precision);
    }
}
