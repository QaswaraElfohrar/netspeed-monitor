using System.Windows.Media;
using Microsoft.Win32;

namespace NetSpeedMonitor.App.Services;

/// <summary>Start-with-Windows (HKCU Run key) and light/dark detection. No admin rights needed.</summary>
public static class SystemIntegration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "NetSpeedMonitor";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(ValueName) is string;
        }
        catch { return false; }
    }

    public static void SetStartup(bool enabled)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                string exe = Environment.ProcessPath ?? "";
                if (exe.Length > 0) k.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                k.DeleteValue(ValueName, false);
            }
        }
        catch (Exception ex)
        {
            Core.Diagnostics.Logger.Error("Could not update the startup entry", ex);
        }
    }

    /// <summary>True when Windows apps use the light theme.</summary>
    public static bool AppsUseLightTheme() => ReadPersonalize("AppsUseLightTheme");

    /// <summary>True when the taskbar uses the light theme (the widget sits on it).</summary>
    public static bool TaskbarUsesLightTheme() => ReadPersonalize("SystemUsesLightTheme");

    private static bool ReadPersonalize(string name)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return k?.GetValue(name) is int v && v != 0;
        }
        catch { return false; }
    }
}

public static class ColorUtil
{
    public static Color Parse(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return fallback; }
    }

    public static SolidColorBrush Brush(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
