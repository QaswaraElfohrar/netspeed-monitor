using System.Text.Json;
using NetSpeedMonitor.Core.Diagnostics;

namespace NetSpeedMonitor.Core.Settings;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load(string? path = null)
    {
        path ??= AppPaths.SettingsFile;
        try
        {
            if (File.Exists(path))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
                if (s != null)
                {
                    s.Normalize();
                    return s;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Could not read settings, using defaults", ex);
            try { File.Move(path, path + ".bad", true); } catch { /* ignore */ }
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings, string? path = null)
    {
        path ??= AppPaths.SettingsFile;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Options));
            File.Move(tmp, path, true);
        }
        catch (Exception ex)
        {
            Logger.Error("Could not save settings", ex);
        }
    }
}
