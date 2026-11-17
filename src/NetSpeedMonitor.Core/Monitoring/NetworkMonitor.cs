using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using NetSpeedMonitor.Core.Diagnostics;
using NetSpeedMonitor.Core.Storage;

namespace NetSpeedMonitor.Core.Monitoring;

/// <summary>
/// Samples the interface byte counters on a background loop (PeriodicTimer, no busy-wait)
/// and raises <see cref="SnapshotReady"/> on that background thread.
/// </summary>
public sealed class NetworkMonitor : IDisposable
{
    private readonly HistoryStore _history;
    private readonly object _gate = new();
    private readonly Dictionary<string, SpeedCalculator> _calculators = new();
    private readonly CancellationTokenSource _cts = new();
    private PeriodicTimer? _timer;
    private Task? _loop;

    private AdapterInfo[] _adapters = Array.Empty<AdapterInfo>();
    private volatile bool _catalogDirty = true;
    private long _lastCatalogStamp;

    private volatile bool _auto = true;
    private volatile string _adapterId = "";
    private volatile int _intervalMs = 1000;
    private long _sessionDown;
    private long _sessionUp;

    public event Action<MonitorSnapshot>? SnapshotReady;

    public NetworkMonitor(HistoryStore history)
    {
        _history = history;
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
    }

    public void Configure(bool autoAdapter, string? adapterId, int intervalMs)
    {
        _auto = autoAdapter;
        _adapterId = adapterId ?? "";
        _intervalMs = intervalMs;
        var t = _timer;
        if (t != null)
        {
            try { t.Period = TimeSpan.FromMilliseconds(intervalMs); } catch (ObjectDisposedException) { }
        }
    }

    public void Start()
    {
        if (_loop != null) return;
        _loop = Task.Run(() => LoopAsync(_cts.Token));
    }

    /// <summary>Drop all baselines (system resumed from sleep, adapters changed...).</summary>
    public void ResetBaselines()
    {
        lock (_gate)
        {
            foreach (var c in _calculators.Values) c.Reset();
        }
        _catalogDirty = true;
    }

    public void ResetSession()
    {
        lock (_gate)
        {
            _sessionDown = 0;
            _sessionUp = 0;
        }
    }

    /// <summary>All interfaces currently known (for the adapter pickers).</summary>
    public IReadOnlyList<AdapterInfo> GetAdapters()
    {
        if (_adapters.Length == 0) RefreshCatalog();
        return _adapters;
    }

    private void OnNetworkChanged(object? sender, EventArgs e) => _catalogDirty = true;

    private async Task LoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervalMs));
        _timer = timer;
        try
        {
            SafeTick();
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                SafeTick();
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        finally
        {
            _timer = null;
        }
    }

    private void SafeTick()
    {
        try { Tick(); }
        catch (Exception ex) { Logger.Error("Monitor tick failed", ex); _catalogDirty = true; }
    }

    private void RefreshCatalog()
    {
        var list = new List<AdapterInfo>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            try
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                bool hasGateway = false;
                try
                {
                    hasGateway = nic.GetIPProperties().GatewayAddresses.Any(g =>
                        !g.Address.Equals(IPAddress.Any) && !g.Address.Equals(IPAddress.IPv6Any));
                }
                catch { /* some virtual NICs refuse */ }

                list.Add(new AdapterInfo
                {
                    Id = nic.Id,
                    Name = nic.Name,
                    Description = nic.Description,
                    Type = nic.NetworkInterfaceType,
                    IsUp = nic.OperationalStatus == OperationalStatus.Up,
                    HasGateway = hasGateway,
                    IsVirtual = AdapterClassifier.IsVirtual(nic.Name, nic.Description, nic.NetworkInterfaceType),
                    IsVpn = AdapterClassifier.IsVpn(nic.Name, nic.Description, nic.NetworkInterfaceType),
                    Nic = nic,
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Adapter skipped", ex);
            }
        }

        _adapters = list.ToArray();
        _catalogDirty = false;
        _lastCatalogStamp = Stopwatch.GetTimestamp();
    }

    private void Tick()
    {
        long now = Stopwatch.GetTimestamp();
        bool stale = (now - _lastCatalogStamp) / (double)Stopwatch.Frequency > 30;
        if (_catalogDirty || stale || _adapters.Length == 0) RefreshCatalog();

        var adapters = _adapters;
        var samples = new Dictionary<string, TrafficSample>(adapters.Length);

        lock (_gate)
        {
            // forget calculators of adapters that disappeared
            foreach (var key in _calculators.Keys.Where(k => adapters.All(a => a.Id != k)).ToList())
                _calculators.Remove(key);

            foreach (var a in adapters)
            {
                if (!a.IsUp || a.Nic == null)
                {
                    _calculators.Remove(a.Id); // reconnect starts a fresh baseline
                    continue;
                }

                ulong rx, tx;
                try
                {
                    var s = a.Nic.GetIPStatistics();
                    rx = (ulong)Math.Max(0, s.BytesReceived);
                    tx = (ulong)Math.Max(0, s.BytesSent);
                }
                catch (Exception)
                {
                    _catalogDirty = true; // adapter vanished while reading
                    continue;
                }

                if (!_calculators.TryGetValue(a.Id, out var calc))
                    _calculators[a.Id] = calc = new SpeedCalculator(maxGapSeconds: Math.Max(10, _intervalMs / 1000.0 * 5));

                samples[a.Id] = calc.Update(rx, tx, now);
            }
        }

        var selected = AdapterSelector.Select(adapters, _auto, _adapterId);
        var selectedIds = selected.Select(s => s.Id).ToHashSet();

        double down = 0, up = 0;
        long dDown = 0, dUp = 0;
        foreach (var a in selected)
        {
            if (samples.TryGetValue(a.Id, out var s) && s.Valid)
            {
                down += s.DownBytesPerSec;
                up += s.UpBytesPerSec;
                dDown += (long)s.DownDelta;
                dUp += (long)s.UpDelta;
            }
        }

        long sessionDown, sessionUp;
        lock (_gate)
        {
            _sessionDown += dDown;
            _sessionUp += dUp;
            sessionDown = _sessionDown;
            sessionUp = _sessionUp;
        }

        if (dDown > 0 || dUp > 0)
            _history.Add(DateOnly.FromDateTime(DateTime.Now), dDown, dUp);

        var stats = adapters
            .Select(a =>
            {
                samples.TryGetValue(a.Id, out var s);
                return new AdapterStat(a, s.Valid ? s.DownBytesPerSec : 0, s.Valid ? s.UpBytesPerSec : 0, selectedIds.Contains(a.Id));
            })
            .ToList();

        bool connected = selected.Any(a => a.IsUp);
        string text = selected.Count == 0 ? "" : string.Join(", ", selected.Select(a => a.Name));
        SnapshotReady?.Invoke(new MonitorSnapshot(down, up, sessionDown, sessionUp, text, connected, stats));
    }

    public void Dispose()
    {
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
        _cts.Cancel();
        try { _loop?.Wait(2000); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
