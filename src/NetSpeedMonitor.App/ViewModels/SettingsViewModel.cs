using System.Runtime.CompilerServices;
using System.Windows.Media;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.Core.Settings;

namespace NetSpeedMonitor.App.ViewModels;

public sealed record AdapterOption(string Id, string Display);

/// <summary>Edits a copy of the settings; OK / Apply push it to the controller.</summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppController _app;
    private readonly AppSettings _d;
    private int _tab;

    public SettingsViewModel(AppController app)
    {
        _app = app;
        _d = app.Settings.Clone();
        Fonts = Fonts_().ToList();
        Adapters = app.Monitor.GetAdapters()
            .Select(a => new AdapterOption(a.Id, a.DisplayName))
            .ToList();
        OkCommand = new RelayCommand(() => { Apply(); CloseRequested?.Invoke(); });
        ApplyCommand = new RelayCommand(Apply);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
        PickColorCommand = new RelayCommand(PickColor);
        ClearBackgroundCommand = new RelayCommand(() => BackgroundColor = "");
    }

    public event Action? CloseRequested;

    public IReadOnlyList<string> Fonts { get; }
    public IReadOnlyList<AdapterOption> Adapters { get; }

    public RelayCommand OkCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PickColorCommand { get; }
    public RelayCommand ClearBackgroundCommand { get; }

    private static IEnumerable<string> Fonts_() =>
        System.Windows.Media.Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);

    // ---- navigation
    public int Tab { get => _tab; set { if (Set(ref _tab, value)) { Raise(nameof(IsGeneral)); Raise(nameof(IsAppearance)); Raise(nameof(IsMonitoring)); Raise(nameof(IsPosition)); } } }
    public bool IsGeneral { get => _tab == 0; set { if (value) Tab = 0; } }
    public bool IsAppearance { get => _tab == 1; set { if (value) Tab = 1; } }
    public bool IsMonitoring { get => _tab == 2; set { if (value) Tab = 2; } }
    public bool IsPosition { get => _tab == 3; set { if (value) Tab = 3; } }

    private bool P<T>(T current, T value, Action<T> assign, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return false;
        assign(value);
        Raise(name);
        return true;
    }

    // ---- general
    public string Language { get => _d.Language; set => P(_d.Language, value, v => _d.Language = v); }
    public bool StartWithWindows { get => _d.StartWithWindows; set => P(_d.StartWithWindows, value, v => _d.StartWithWindows = v); }
    public bool StartMinimized { get => _d.StartMinimized; set => P(_d.StartMinimized, value, v => _d.StartMinimized = v); }
    public bool MinimizeToTray { get => _d.MinimizeToTray; set => P(_d.MinimizeToTray, value, v => _d.MinimizeToTray = v); }
    public bool DashboardOnDoubleClick { get => _d.DashboardOnDoubleClick; set => P(_d.DashboardOnDoubleClick, value, v => _d.DashboardOnDoubleClick = v); }

    // ---- appearance
    public string Theme { get => _d.Theme; set => P(_d.Theme, value, v => _d.Theme = v); }
    public string FontFamily { get => _d.FontFamily; set { if (!string.IsNullOrEmpty(value)) P(_d.FontFamily, value, v => _d.FontFamily = v); } }
    public double FontSize { get => _d.FontSize; set => P(_d.FontSize, value, v => _d.FontSize = v); }
    public double BackgroundOpacity { get => _d.BackgroundOpacity; set { if (P(_d.BackgroundOpacity, value, v => _d.BackgroundOpacity = v)) Raise(nameof(OpacityText)); } }
    public string OpacityText => $"{Math.Round(_d.BackgroundOpacity * 100)}%";
    public bool TransparentBackground { get => _d.TransparentBackground; set { if (P(_d.TransparentBackground, value, v => _d.TransparentBackground = v)) Raise(nameof(OpacityEnabled)); } }
    public bool OpacityEnabled => !_d.TransparentBackground;
    public string Layout { get => _d.Layout; set => P(_d.Layout, value, v => _d.Layout = v); }
    public double WidgetWidth { get => _d.WidgetWidth; set => P(_d.WidgetWidth, value, v => _d.WidgetWidth = v); }

    public string DownloadColor { get => _d.DownloadColor; set { if (P(_d.DownloadColor, value, v => _d.DownloadColor = v)) Raise(nameof(DownloadSwatch)); } }
    public string UploadColor { get => _d.UploadColor; set { if (P(_d.UploadColor, value, v => _d.UploadColor = v)) Raise(nameof(UploadSwatch)); } }
    public string BackgroundColor { get => _d.BackgroundColor; set { if (P(_d.BackgroundColor, value, v => _d.BackgroundColor = v)) Raise(nameof(BackgroundSwatch)); } }
    public Brush DownloadSwatch => ColorUtil.Brush(ColorUtil.Parse(_d.DownloadColor, Colors.LimeGreen));
    public Brush UploadSwatch => ColorUtil.Brush(ColorUtil.Parse(_d.UploadColor, Colors.DodgerBlue));
    public Brush BackgroundSwatch => string.IsNullOrEmpty(_d.BackgroundColor)
        ? Brushes.Transparent
        : ColorUtil.Brush(ColorUtil.Parse(_d.BackgroundColor, Colors.Transparent));

    // ---- monitoring
    public string RefreshText
    {
        get => _d.RefreshIntervalMs.ToString();
        set { if (int.TryParse(value, out int ms)) P(_d.RefreshIntervalMs, ms, v => _d.RefreshIntervalMs = v); }
    }
    public bool AutoAdapter { get => _d.AutoAdapter; set { if (P(_d.AutoAdapter, value, v => _d.AutoAdapter = v)) Raise(nameof(ManualAdapterEnabled)); } }
    public bool ManualAdapterEnabled => !_d.AutoAdapter;
    public string AdapterId { get => _d.AdapterId; set { if (value != null) P(_d.AdapterId, value, v => _d.AdapterId = v); } }
    public string Unit { get => _d.Unit; set => P(_d.Unit, value, v => _d.Unit = v); }
    public double Precision { get => _d.Precision; set => P((double)_d.Precision, value, v => _d.Precision = (int)Math.Round(v)); }
    public bool ShowDownload { get => _d.ShowDownload; set => P(_d.ShowDownload, value, v => _d.ShowDownload = v); }
    public bool ShowUpload { get => _d.ShowUpload; set => P(_d.ShowUpload, value, v => _d.ShowUpload = v); }

    // ---- position
    public bool AutoPosition { get => _d.AutoPosition; set { if (P(_d.AutoPosition, value, v => _d.AutoPosition = v)) Raise(nameof(ManualPositionEnabled)); } }
    public bool ManualPositionEnabled => !_d.AutoPosition;
    public bool LockPosition { get => _d.LockPosition; set => P(_d.LockPosition, value, v => _d.LockPosition = v); }
    public bool AlwaysOnTop { get => _d.AlwaysOnTop; set => P(_d.AlwaysOnTop, value, v => _d.AlwaysOnTop = v); }
    public int ManualX { get => _d.ManualX; set => P(_d.ManualX, value, v => _d.ManualX = v); }
    public int ManualY { get => _d.ManualY; set => P(_d.ManualY, value, v => _d.ManualY = v); }

    private void Apply()
    {
        // the tray "show/hide" toggle may have changed visibility while this window was open
        _d.WidgetVisible = _app.Settings.WidgetVisible;
        _app.ApplySettings(_d.Clone());
    }

    private void PickColor(object? which)
    {
        string key = which as string ?? "down";
        string current = key switch { "up" => _d.UploadColor, "bg" => _d.BackgroundColor, _ => _d.DownloadColor };
        var c = ColorUtil.Parse(current, Colors.Gray);
        using var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B),
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        string hex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
        switch (key)
        {
            case "up": UploadColor = hex; break;
            case "bg": BackgroundColor = hex; break;
            default: DownloadColor = hex; break;
        }
    }
}
