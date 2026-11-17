using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.Core.Formatting;
using NetSpeedMonitor.Core.Localization;
using NetSpeedMonitor.Core.Monitoring;

namespace NetSpeedMonitor.App.ViewModels;

public sealed class AdapterRowViewModel : ViewModelBase
{
    private readonly Action<string> _onSelect;
    private string _speed = "";
    private string _status = "";
    private bool _selected;
    private bool _isUp;

    public AdapterRowViewModel(AdapterInfo info, Action<string> onSelect)
    {
        Id = info.Id;
        _onSelect = onSelect;
    }

    public string Id { get; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Tags { get; private set; } = "";

    public string Speed { get => _speed; private set => Set(ref _speed, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsUp { get => _isUp; private set => Set(ref _isUp, value); }

    /// <summary>Two-way bound to the radio button: choosing it makes this adapter the manual choice.</summary>
    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            if (value) _onSelect(Id);
        }
    }

    public void SetChosen(bool chosen)
    {
        if (_selected == chosen) return;
        _selected = chosen;
        Raise(nameof(IsSelected));
    }

    public void Update(AdapterStat st, bool bits, int precision)
    {
        var i = st.Info;
        Name = i.Name;
        Description = i.Description;
        var tags = new List<string>();
        if (i.IsVpn) tags.Add(Loc.Instance["adapters.vpn"]);
        else if (i.IsVirtual) tags.Add(Loc.Instance["adapters.virtual"]);
        Tags = string.Join(", ", tags);
        Raise(nameof(Name));
        Raise(nameof(Description));
        Raise(nameof(Tags));
        IsUp = i.IsUp;
        Status = Loc.Instance[i.IsUp ? "adapters.up" : "adapters.down"];
        Speed = i.IsUp
            ? $"↓ {SpeedFormatter.FormatSpeed(st.DownBytesPerSec, bits, precision)}   ↑ {SpeedFormatter.FormatSpeed(st.UpBytesPerSec, bits, precision)}"
            : "";
    }
}

public sealed record UsageRow(string Date, string Down, string Up, string Sum);

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AppController _app;
    private readonly DispatcherTimer _usageTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly Dictionary<string, AdapterRowViewModel> _rowsById = new();
    private int _tab;
    private string _down = "0 B/s", _up = "0 B/s", _sessionDown = "0 B", _sessionUp = "0 B", _adapter = "—";
    private bool _connected;
    private bool _attached;

    public DashboardViewModel(AppController app)
    {
        _app = app;
        _usageTimer.Tick += (_, _) => RefreshUsage();
    }

    public event Action<double, double>? Sample;

    public ObservableCollection<AdapterRowViewModel> Adapters { get; } = new();
    public ObservableCollection<UsageRow> Daily { get; } = new();

    public int Tab
    {
        get => _tab;
        set
        {
            if (!Set(ref _tab, value)) return;
            Raise(nameof(IsLiveTab));
            Raise(nameof(IsUsageTab));
            Raise(nameof(IsAdaptersTab));
            if (value == 1) RefreshUsage();
        }
    }

    public bool IsLiveTab { get => _tab == 0; set { if (value) Tab = 0; } }
    public bool IsUsageTab { get => _tab == 1; set { if (value) Tab = 1; } }
    public bool IsAdaptersTab { get => _tab == 2; set { if (value) Tab = 2; } }

    public string DownText { get => _down; private set => Set(ref _down, value); }
    public string UpText { get => _up; private set => Set(ref _up, value); }
    public string SessionDownText { get => _sessionDown; private set => Set(ref _sessionDown, value); }
    public string SessionUpText { get => _sessionUp; private set => Set(ref _sessionUp, value); }
    public string AdapterText { get => _adapter; private set => Set(ref _adapter, value); }
    public bool IsConnected { get => _connected; private set => Set(ref _connected, value); }

    // usage tiles
    private string _todayDown = "", _todayUp = "", _weekDown = "", _weekUp = "", _monthDown = "", _monthUp = "", _totalDown = "", _totalUp = "";
    public string TodayDown { get => _todayDown; private set => Set(ref _todayDown, value); }
    public string TodayUp { get => _todayUp; private set => Set(ref _todayUp, value); }
    public string WeekDown { get => _weekDown; private set => Set(ref _weekDown, value); }
    public string WeekUp { get => _weekUp; private set => Set(ref _weekUp, value); }
    public string MonthDown { get => _monthDown; private set => Set(ref _monthDown, value); }
    public string MonthUp { get => _monthUp; private set => Set(ref _monthUp, value); }
    public string TotalDown { get => _totalDown; private set => Set(ref _totalDown, value); }
    public string TotalUp { get => _totalUp; private set => Set(ref _totalUp, value); }

    // adapter selection
    public bool IsAuto
    {
        get => _app.Settings.AutoAdapter;
        set
        {
            if (!value || _app.Settings.AutoAdapter) return;
            var s = _app.Settings.Clone();
            s.AutoAdapter = true;
            _app.ApplySettings(s);
            MarkSelection();
        }
    }

    public void Attach()
    {
        if (_attached) return;
        _attached = true;
        _app.SnapshotReceived += OnSnapshot;
        _usageTimer.Start();
        RefreshUsage();
        if (_app.Last != null) OnSnapshot(_app.Last);
    }

    public void Detach()
    {
        if (!_attached) return;
        _attached = false;
        _app.SnapshotReceived -= OnSnapshot;
        _usageTimer.Stop();
    }

    private void SelectAdapter(string id)
    {
        var s = _app.Settings.Clone();
        s.AutoAdapter = false;
        s.AdapterId = id;
        _app.ApplySettings(s);
        MarkSelection();
    }

    private void MarkSelection()
    {
        Raise(nameof(IsAuto));
        foreach (var r in Adapters)
            r.SetChosen(!_app.Settings.AutoAdapter && r.Id == _app.Settings.AdapterId);
    }

    private void OnSnapshot(MonitorSnapshot s)
    {
        var set = _app.Settings;
        bool bits = set.Unit == "bits";
        DownText = SpeedFormatter.FormatSpeed(s.DownBytesPerSec, bits, set.Precision);
        UpText = SpeedFormatter.FormatSpeed(s.UpBytesPerSec, bits, set.Precision);
        SessionDownText = SpeedFormatter.FormatBytes(s.SessionDownBytes);
        SessionUpText = SpeedFormatter.FormatBytes(s.SessionUpBytes);
        AdapterText = string.IsNullOrEmpty(s.AdapterText) ? "—" : s.AdapterText;
        IsConnected = s.Connected;
        Sample?.Invoke(s.DownBytesPerSec, s.UpBytesPerSec);

        if (_tab == 2 || Adapters.Count == 0) SyncAdapters(s, bits, set.Precision);
    }

    private void SyncAdapters(MonitorSnapshot s, bool bits, int precision)
    {
        var seen = new HashSet<string>();
        foreach (var st in s.Adapters)
        {
            seen.Add(st.Info.Id);
            if (!_rowsById.TryGetValue(st.Info.Id, out var row))
            {
                row = new AdapterRowViewModel(st.Info, SelectAdapter);
                _rowsById[st.Info.Id] = row;
                Adapters.Add(row);
            }
            row.Update(st, bits, precision);
        }

        foreach (var gone in _rowsById.Keys.Where(k => !seen.Contains(k)).ToList())
        {
            Adapters.Remove(_rowsById[gone]);
            _rowsById.Remove(gone);
        }
        MarkSelection();
    }

    public void RefreshUsage()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var first = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var t = _app.History.Today(today);
        var w = _app.History.ThisWeek(today, first);
        var m = _app.History.ThisMonth(today);
        var all = _app.History.Total();
        TodayDown = SpeedFormatter.FormatBytes(t.Down); TodayUp = SpeedFormatter.FormatBytes(t.Up);
        WeekDown = SpeedFormatter.FormatBytes(w.Down); WeekUp = SpeedFormatter.FormatBytes(w.Up);
        MonthDown = SpeedFormatter.FormatBytes(m.Down); MonthUp = SpeedFormatter.FormatBytes(m.Up);
        TotalDown = SpeedFormatter.FormatBytes(all.Down); TotalUp = SpeedFormatter.FormatBytes(all.Up);

        Daily.Clear();
        foreach (var d in _app.History.LastDays(today, 30))
        {
            Daily.Add(new UsageRow(
                d.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                SpeedFormatter.FormatBytes(d.Down),
                SpeedFormatter.FormatBytes(d.Up),
                SpeedFormatter.FormatBytes(d.Down + d.Up)));
        }
    }
}
