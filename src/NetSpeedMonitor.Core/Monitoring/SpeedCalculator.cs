using System.Diagnostics;

namespace NetSpeedMonitor.Core.Monitoring;

/// <summary>Result of one counter comparison.</summary>
public readonly record struct TrafficSample(
    bool Valid,
    double DownBytesPerSec,
    double UpBytesPerSec,
    ulong DownDelta,
    ulong UpDelta);

/// <summary>
/// Turns consecutive cumulative byte counters into speeds using the real elapsed time
/// between the two readings (monotonic <see cref="Stopwatch"/> timestamps).
/// Counter resets, 32-bit wrap-around, non-monotonic timestamps and long gaps
/// (sleep/resume) never produce bogus spikes: the baseline is simply re-taken.
/// </summary>
public sealed class SpeedCalculator
{
    private readonly long _frequency;
    private readonly double _maxGapSeconds;
    private bool _hasBaseline;
    private ulong _lastRx;
    private ulong _lastTx;
    private long _lastStamp;

    public SpeedCalculator(double maxGapSeconds = 10, long? timestampFrequency = null)
    {
        _maxGapSeconds = maxGapSeconds;
        _frequency = timestampFrequency ?? Stopwatch.Frequency;
    }

    /// <summary>Forget the previous reading (adapter reconnected, system resumed...).</summary>
    public void Reset() => _hasBaseline = false;

    public TrafficSample Update(ulong rxBytes, ulong txBytes, long stamp)
    {
        if (!_hasBaseline)
        {
            Store(rxBytes, txBytes, stamp);
            return default;
        }

        if (stamp <= _lastStamp)
        {
            // Clock did not advance: ignore this reading, keep the old baseline.
            return default;
        }

        double elapsed = (stamp - _lastStamp) / (double)_frequency;
        bool okRx = TryDelta(_lastRx, rxBytes, out ulong dRx);
        bool okTx = TryDelta(_lastTx, txBytes, out ulong dTx);
        Store(rxBytes, txBytes, stamp);

        if (!okRx || !okTx || elapsed > _maxGapSeconds)
            return default;

        return new TrafficSample(true, dRx / elapsed, dTx / elapsed, dRx, dTx);
    }

    private void Store(ulong rx, ulong tx, long stamp)
    {
        _lastRx = rx;
        _lastTx = tx;
        _lastStamp = stamp;
        _hasBaseline = true;
    }

    private static bool TryDelta(ulong previous, ulong current, out ulong delta)
    {
        if (current >= previous)
        {
            delta = current - previous;
            return true;
        }

        // Counter went backwards. A drop from near 2^32 to a small number is a 32-bit wrap.
        if (previous > 0xC000_0000UL && previous <= uint.MaxValue && current < 0x4000_0000UL)
        {
            delta = (uint.MaxValue - previous) + 1UL + current;
            return true;
        }

        // Otherwise: the interface was reset. Re-baseline, count nothing.
        delta = 0;
        return false;
    }
}
