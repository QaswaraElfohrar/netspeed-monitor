namespace NetSpeedMonitor.Core.Settings;

/// <summary>All user configuration. Plain data, persisted as JSON (settings.json).</summary>
public sealed class AppSettings
{
    // ---- General
    public string Language { get; set; } = "system";          // system | en | ar | fr
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;          // don't open the dashboard at launch
    public bool MinimizeToTray { get; set; } = true;
    public bool DashboardOnDoubleClick { get; set; } = true;

    // ---- Appearance
    public string Theme { get; set; } = "system";             // system | dark | light
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 12;
    public string DownloadColor { get; set; } = "#2ECC71";
    public string UploadColor { get; set; } = "#3498DB";
    public string BackgroundColor { get; set; } = "";         // empty = follow theme
    public bool TransparentBackground { get; set; } = true;
    public double BackgroundOpacity { get; set; } = 0.85;
    public string Layout { get; set; } = "horizontal";        // horizontal | vertical
    public double WidgetWidth { get; set; }                   // DIPs, 0 = automatic

    // ---- Monitoring
    public int RefreshIntervalMs { get; set; } = 1000;        // 500 | 1000 | 2000
    public bool AutoAdapter { get; set; } = true;
    public string AdapterId { get; set; } = "";
    public string Unit { get; set; } = "bytes";               // bytes | bits
    public int Precision { get; set; } = 2;
    public bool ShowDownload { get; set; } = true;
    public bool ShowUpload { get; set; } = true;

    // ---- Widget position
    public bool WidgetVisible { get; set; } = true;
    public bool AutoPosition { get; set; } = true;
    public int ManualX { get; set; }
    public int ManualY { get; set; }
    public bool LockPosition { get; set; }
    public bool AlwaysOnTop { get; set; } = true;

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    /// <summary>Clamp hand-edited / corrupt values into valid ranges.</summary>
    public void Normalize()
    {
        if (Language is not ("system" or "en" or "ar" or "fr")) Language = "system";
        if (Theme is not ("system" or "dark" or "light")) Theme = "system";
        if (Layout is not ("horizontal" or "vertical")) Layout = "horizontal";
        if (Unit is not ("bytes" or "bits")) Unit = "bytes";
        if (RefreshIntervalMs is not (500 or 1000 or 2000)) RefreshIntervalMs = 1000;
        FontSize = Math.Clamp(FontSize, 8, 32);
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0, 1);
        Precision = Math.Clamp(Precision, 0, 3);
        WidgetWidth = Math.Clamp(WidgetWidth, 0, 600);
        if (string.IsNullOrWhiteSpace(FontFamily)) FontFamily = "Segoe UI";
        DownloadColor ??= "#2ECC71";
        UploadColor ??= "#3498DB";
        BackgroundColor ??= "";
        AdapterId ??= "";
    }
}
