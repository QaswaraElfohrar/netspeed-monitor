namespace NetSpeedMonitor.Core;

/// <summary>
/// Where settings and history live. Normal install: %AppData%\NetSpeedMonitor.
/// Portable mode: if a file named "portable.txt" sits next to the exe, everything is kept in a "data" folder beside it.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = Resolve();

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
    public static string HistoryFile => Path.Combine(DataDirectory, "history.json");

    private static string Resolve()
    {
        try
        {
            string exeDir = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(exeDir, "portable.txt")))
                return Path.Combine(exeDir, "data");
        }
        catch
        {
            // fall through to AppData
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetSpeedMonitor");
    }
}
