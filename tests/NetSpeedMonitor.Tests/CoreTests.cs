using System.Net.NetworkInformation;
using NetSpeedMonitor.Core.Formatting;
using NetSpeedMonitor.Core.Localization;
using NetSpeedMonitor.Core.Monitoring;
using NetSpeedMonitor.Core.Settings;
using NetSpeedMonitor.Core.Storage;
using Xunit;

namespace NetSpeedMonitor.Tests;

public class SpeedCalculatorTests
{
    private const long Freq = 1000; // 1000 ticks = 1 second

    [Fact]
    public void FirstReadingOnlySetsBaseline()
    {
        var c = new SpeedCalculator(10, Freq);
        Assert.False(c.Update(1000, 500, 0).Valid);
    }

    [Fact]
    public void ComputesSpeedFromDeltaAndRealElapsedTime()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(0, 0, 0);
        var s = c.Update(2_000_000, 500_000, 2000); // 2 s later
        Assert.True(s.Valid);
        Assert.Equal(1_000_000, s.DownBytesPerSec, 3);
        Assert.Equal(250_000, s.UpBytesPerSec, 3);
        Assert.Equal(2_000_000UL, s.DownDelta);
    }

    [Fact]
    public void UnevenIntervalsAreNotAssumedToBeOneSecond()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(0, 0, 0);
        var s = c.Update(1500, 0, 1500); // 1.5 s
        Assert.Equal(1000, s.DownBytesPerSec, 3);
    }

    [Fact]
    public void CounterResetProducesNoSpikeAndRebaselines()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(5_000_000_000UL, 5_000_000_000UL, 0);
        Assert.False(c.Update(100, 100, 1000).Valid);      // interface reset
        var next = c.Update(1100, 100, 2000);               // normal again, relative to the new baseline
        Assert.True(next.Valid);
        Assert.Equal(1000, next.DownBytesPerSec, 3);
    }

    [Fact]
    public void ThirtyTwoBitWrapAroundIsHandled()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(uint.MaxValue - 99UL, 0, 0);
        var s = c.Update(900UL, 0, 1000); // wrapped: 100 + 900 = 1000 bytes
        Assert.True(s.Valid);
        Assert.Equal(1000UL, s.DownDelta);
    }

    [Fact]
    public void LongGapAfterSleepIsDiscarded()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(0, 0, 0);
        Assert.False(c.Update(9_000_000, 9_000_000, 3_600_000).Valid); // an hour later
        Assert.True(c.Update(9_001_000, 9_000_000, 3_601_000).Valid);  // recovers on the next sample
    }

    [Fact]
    public void NonAdvancingClockIsIgnored()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(0, 0, 1000);
        Assert.False(c.Update(500, 0, 1000).Valid);
        Assert.False(c.Update(500, 0, 900).Valid);
        Assert.Equal(1000, c.Update(1000, 0, 2000).DownBytesPerSec, 3); // baseline still the first reading
    }

    [Fact]
    public void ExplicitResetDropsBaseline()
    {
        var c = new SpeedCalculator(10, Freq);
        c.Update(0, 0, 0);
        c.Reset();
        Assert.False(c.Update(1_000_000, 0, 1000).Valid);
    }
}

public class FormatterTests
{
    [Theory]
    [InlineData(0, false, 2, "0 B/s")]
    [InlineData(512, false, 2, "512 B/s")]
    [InlineData(850 * 1024, false, 2, "850 KB/s")]
    [InlineData(2.45 * 1024 * 1024, false, 2, "2.45 MB/s")]
    [InlineData(12.34 * 1024 * 1024, false, 2, "12.3 MB/s")]
    [InlineData(1023.9 * 1024, false, 2, "1.00 MB/s")]
    [InlineData(125_000, true, 1, "1.0 Mbps")]
    [InlineData(-5, false, 2, "0 B/s")]
    public void FormatsSpeed(double bytesPerSec, bool bits, int precision, string expected)
        => Assert.Equal(expected, SpeedFormatter.FormatSpeed(bytesPerSec, bits, precision));

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(5L * 1024 * 1024 * 1024, "5.00 GB")]
    public void FormatsBytes(long bytes, string expected) => Assert.Equal(expected, SpeedFormatter.FormatBytes(bytes));
}

public class AdapterSelectionTests
{
    private static AdapterInfo A(string id, NetworkInterfaceType t, bool up = true, bool gw = true, string name = "x", string desc = "x")
        => new()
        {
            Id = id, Name = name, Description = desc, Type = t, IsUp = up, HasGateway = gw,
            IsVirtual = AdapterClassifier.IsVirtual(name, desc, t),
            IsVpn = AdapterClassifier.IsVpn(name, desc, t),
        };

    [Fact]
    public void AutoPicksPhysicalAndIgnoresVpnAndVirtualToAvoidDoubleCounting()
    {
        var list = new[]
        {
            A("eth", NetworkInterfaceType.Ethernet, name: "Ethernet", desc: "Intel I219-V"),
            A("vpn", NetworkInterfaceType.Ethernet, name: "NordLynx", desc: "WireGuard Tunnel"),
            A("hv", NetworkInterfaceType.Ethernet, name: "vEthernet (WSL)", desc: "Hyper-V Virtual Ethernet Adapter"),
            A("wifi", NetworkInterfaceType.Wireless80211, up: false, name: "Wi-Fi", desc: "Intel AX201"),
        };
        var sel = AdapterSelector.Select(list, true, null);
        Assert.Equal(new[] { "eth" }, sel.Select(s => s.Id));
    }

    [Fact]
    public void AutoFallsBackToVpnWhenNothingElseIsUp()
    {
        var list = new[] { A("vpn", NetworkInterfaceType.Ppp, name: "Corp", desc: "VPN") };
        Assert.Equal("vpn", AdapterSelector.Select(list, true, null).Single().Id);
    }

    [Fact]
    public void ManualChoiceWinsAndVanishedChoiceFallsBackToAuto()
    {
        var list = new[]
        {
            A("eth", NetworkInterfaceType.Ethernet, name: "Ethernet", desc: "Intel"),
            A("wifi", NetworkInterfaceType.Wireless80211, name: "Wi-Fi", desc: "Intel AX"),
        };
        Assert.Equal("wifi", AdapterSelector.Select(list, false, "wifi").Single().Id);
        Assert.Equal(2, AdapterSelector.Select(list, false, "gone").Count);
    }

    [Fact]
    public void DisconnectedMeansNothingSelected()
        => Assert.Empty(AdapterSelector.Select(new[] { A("eth", NetworkInterfaceType.Ethernet, up: false, name: "Ethernet", desc: "Intel") }, true, null));
}

public class StorageTests
{
    [Fact]
    public void HistoryAccumulatesAndPersists()
    {
        string path = Path.Combine(Path.GetTempPath(), "nsm_hist_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var today = new DateOnly(2026, 11, 8); // a Sunday
            var h = new HistoryStore(path);
            h.Add(today, 100, 10);
            h.Add(today, 50, 5);
            h.Add(today.AddDays(-1), 1000, 100);
            h.Add(today.AddDays(-40), 7, 7);
            h.SaveIfDirty();

            var again = new HistoryStore(path);
            Assert.Equal(new UsageTotals(150, 15), again.Today(today));
            Assert.Equal(new UsageTotals(1150, 115), again.ThisWeek(today, DayOfWeek.Saturday)); // week began Saturday 7th? no: Sat Nov 7
            Assert.Equal(new UsageTotals(150, 15), again.ThisWeek(today, DayOfWeek.Sunday));
            Assert.Equal(new UsageTotals(1157, 122), again.Total());
            Assert.Equal(30, again.LastDays(today, 30).Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SettingsRoundTripAndNormalize()
    {
        string path = Path.Combine(Path.GetTempPath(), "nsm_set_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var s = new AppSettings { Language = "ar", Theme = "dark", FontSize = 99, RefreshIntervalMs = 777, Precision = 9 };
            SettingsStore.Save(s, path);
            var loaded = SettingsStore.Load(path);
            Assert.Equal("ar", loaded.Language);
            Assert.Equal("dark", loaded.Theme);
            Assert.Equal(32, loaded.FontSize);
            Assert.Equal(1000, loaded.RefreshIntervalMs);
            Assert.Equal(3, loaded.Precision);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CorruptSettingsFileFallsBackToDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), "nsm_bad_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.Equal("system", SettingsStore.Load(path).Theme);
        }
        finally { File.Delete(path); File.Delete(path + ".bad"); }
    }
}

public class LocalizationTests
{
    [Fact]
    public void EveryKeyHasEnglishArabicAndFrench()
    {
        foreach (var key in Strings.Keys)
        {
            for (int lang = 0; lang < 3; lang++)
                Assert.False(string.IsNullOrWhiteSpace(Strings.Get(key, lang)), $"{key} missing in language {lang}");
        }
    }

    [Fact]
    public void ArabicIsRtlAndSwitchingWorks()
    {
        Loc.Instance.SetLanguage("ar");
        Assert.True(Loc.Instance.IsRtl);
        Assert.Equal("خروج", Loc.Instance["menu.exit"]);
        Loc.Instance.SetLanguage("fr");
        Assert.False(Loc.Instance.IsRtl);
        Assert.Equal("Quitter", Loc.Instance["menu.exit"]);
        Loc.Instance.SetLanguage("en");
        Assert.Equal("Exit", Loc.Instance["menu.exit"]);
    }
}
