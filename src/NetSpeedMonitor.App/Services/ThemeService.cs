using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using NetSpeedMonitor.Core.Settings;

namespace NetSpeedMonitor.App.Services;

public static class ThemeService
{
    private static bool? _currentLight;

    public static bool IsLight => _currentLight ?? false;
    public static event Action? Changed;

    public static void Apply(AppSettings s)
    {
        bool light = s.Theme switch
        {
            "light" => true,
            "dark" => false,
            _ => SystemIntegration.AppsUseLightTheme(),
        };

        if (_currentLight == light) return;
        _currentLight = light;

        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{(light ? "Light" : "Dark")}.xaml", UriKind.Absolute)
        };
        Application.Current.Resources.MergedDictionaries[0] = dict;
        Changed?.Invoke();
    }

    /// <summary>Makes the window's title bar follow the app theme (Windows 10 20H1+/11).</summary>
    public static void AttachTitleBar(Window window)
    {
        void Update()
        {
            var h = new WindowInteropHelper(window).Handle;
            if (h == IntPtr.Zero) return;
            int dark = IsLight ? 0 : 1;
            Native.DwmSetWindowAttribute(h, 20, ref dark, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
        }

        Update();
        Changed += Update;
        window.Closed += (_, _) => Changed -= Update;
    }
}

public static class MemoryTrimmer
{
    /// <summary>Returns unused pages to Windows (keeps the idle working set small).</summary>
    public static void Trim()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            Native.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }
        catch { /* best effort */ }
    }
}
