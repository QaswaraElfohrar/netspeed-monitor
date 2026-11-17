using System.Globalization;

namespace NetSpeedMonitor.Core.Formatting;

public static class SpeedFormatter
{
    private static readonly string[] ByteUnits = { "B/s", "KB/s", "MB/s", "GB/s" };
    private static readonly string[] BitUnits = { "bps", "Kbps", "Mbps", "Gbps" };
    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>Formats a speed; switches B/s, KB/s, MB/s, GB/s automatically (or bps...Gbps).</summary>
    public static string FormatSpeed(double bytesPerSec, bool bits, int precision)
    {
        if (double.IsNaN(bytesPerSec) || bytesPerSec < 0) bytesPerSec = 0;
        precision = Math.Clamp(precision, 0, 3);

        double step = bits ? 1000d : 1024d;
        string[] units = bits ? BitUnits : ByteUnits;
        double v = bits ? bytesPerSec * 8d : bytesPerSec;

        int i = 0;
        while (i < units.Length - 1 && Math.Round(v, Decimals(v, i, precision)) >= step)
        {
            v /= step;
            i++;
        }

        return v.ToString("F" + Decimals(v, i, precision), CultureInfo.InvariantCulture) + " " + units[i];
    }

    /// <summary>Formats a data size (1024 based): 12.3 MB, 1.25 GB.</summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) bytes = 0;
        double v = bytes;
        int i = 0;
        while (i < SizeUnits.Length - 1 && Math.Round(v, Decimals(v, i, 2)) >= 1024d)
        {
            v /= 1024d;
            i++;
        }
        return v.ToString("F" + Decimals(v, i, 2), CultureInfo.InvariantCulture) + " " + SizeUnits[i];
    }

    private static int Decimals(double value, int unitIndex, int precision)
    {
        if (unitIndex == 0) return 0;
        if (value >= 100) return 0;
        if (value >= 10) return Math.Min(1, precision);
        return precision;
    }
}
