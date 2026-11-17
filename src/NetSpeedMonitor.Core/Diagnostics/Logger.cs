using System.Text;

namespace NetSpeedMonitor.Core.Diagnostics;

/// <summary>Tiny append-only error log (rotates at 512 KB). Never throws.</summary>
public static class Logger
{
    private static readonly object Gate = new();
    private const long MaxBytes = 512 * 1024;

    public static string LogFile => Path.Combine(AppPaths.DataDirectory, "error.log");

    public static void Error(string message, Exception? ex = null)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);
                var fi = new FileInfo(LogFile);
                if (fi.Exists && fi.Length > MaxBytes)
                {
                    string old = LogFile + ".old";
                    File.Delete(old);
                    File.Move(LogFile, old);
                }

                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ").AppendLine(message);
                if (ex != null) sb.AppendLine(ex.ToString());
                File.AppendAllText(LogFile, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // logging must never take the app down
        }
    }
}
