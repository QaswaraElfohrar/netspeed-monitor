using System.Globalization;
using System.Text.Json;
using NetSpeedMonitor.Core.Diagnostics;

namespace NetSpeedMonitor.Core.Storage;

public sealed class DayUsage
{
    public long Down { get; set; }
    public long Up { get; set; }
}

public readonly record struct UsageTotals(long Down, long Up)
{
    public long Sum => Down + Up;
}

public readonly record struct DailyUsage(DateOnly Date, long Down, long Up);

/// <summary>
/// Per-day traffic totals kept in a small JSON file. Written at most once a minute
/// (only when something changed) and on shutdown, atomically (temp file + move).
/// </summary>
public sealed class HistoryStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly Dictionary<string, DayUsage> _days = new();
    private bool _dirty;

    public HistoryStore(string? path = null)
    {
        _path = path ?? AppPaths.HistoryFile;
        Load();
    }

    public void Add(DateOnly day, long down, long up)
    {
        if (down <= 0 && up <= 0) return;
        string key = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        lock (_gate)
        {
            if (!_days.TryGetValue(key, out var u))
                _days[key] = u = new DayUsage();
            u.Down += Math.Max(0, down);
            u.Up += Math.Max(0, up);
            _dirty = true;
        }
    }

    public UsageTotals Today(DateOnly today) => Sum(d => d == today);

    public UsageTotals ThisWeek(DateOnly today, DayOfWeek firstDay)
    {
        int diff = ((int)today.DayOfWeek - (int)firstDay + 7) % 7;
        var start = today.AddDays(-diff);
        return Sum(d => d >= start && d <= today);
    }

    public UsageTotals ThisMonth(DateOnly today) => Sum(d => d.Year == today.Year && d.Month == today.Month);

    public UsageTotals Total() => Sum(_ => true);

    /// <summary>The last <paramref name="count"/> days (newest first), including empty days.</summary>
    public List<DailyUsage> LastDays(DateOnly today, int count)
    {
        var list = new List<DailyUsage>(count);
        lock (_gate)
        {
            for (int i = 0; i < count; i++)
            {
                var d = today.AddDays(-i);
                _days.TryGetValue(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), out var u);
                list.Add(new DailyUsage(d, u?.Down ?? 0, u?.Up ?? 0));
            }
        }
        return list;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _days.Clear();
            _dirty = true;
        }
        SaveIfDirty();
    }

    public void SaveIfDirty()
    {
        string json;
        lock (_gate)
        {
            if (!_dirty) return;
            json = JsonSerializer.Serialize(_days);
            _dirty = false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, true);
        }
        catch (Exception ex)
        {
            lock (_gate) _dirty = true;
            Logger.Error("Could not save history", ex);
        }
    }

    private UsageTotals Sum(Func<DateOnly, bool> filter)
    {
        long down = 0, up = 0;
        lock (_gate)
        {
            foreach (var (key, u) in _days)
            {
                if (DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && filter(d))
                {
                    down += u.Down;
                    up += u.Up;
                }
            }
        }
        return new UsageTotals(down, up);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var data = JsonSerializer.Deserialize<Dictionary<string, DayUsage>>(File.ReadAllText(_path));
            if (data == null) return;
            foreach (var kv in data) _days[kv.Key] = kv.Value;
        }
        catch (Exception ex)
        {
            Logger.Error("Could not read history; starting fresh", ex);
            try { File.Move(_path, _path + ".bad", true); } catch { /* ignore */ }
        }
    }
}
