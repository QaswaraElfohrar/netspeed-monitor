using System.Net.NetworkInformation;

namespace NetSpeedMonitor.Core.Monitoring;

/// <summary>Static facts about a network interface (refreshed when the network changes).</summary>
public sealed class AdapterInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required NetworkInterfaceType Type { get; init; }
    public required bool IsUp { get; init; }
    public required bool HasGateway { get; init; }
    public required bool IsVirtual { get; init; }
    public required bool IsVpn { get; init; }
    internal NetworkInterface? Nic { get; init; }

    public string DisplayName => Name == Description || string.IsNullOrEmpty(Description) ? Name : $"{Name} ({Description})";
}

/// <summary>One adapter's live numbers inside a snapshot.</summary>
public sealed record AdapterStat(AdapterInfo Info, double DownBytesPerSec, double UpBytesPerSec, bool Selected);

public sealed record MonitorSnapshot(
    double DownBytesPerSec,
    double UpBytesPerSec,
    long SessionDownBytes,
    long SessionUpBytes,
    string AdapterText,
    bool Connected,
    IReadOnlyList<AdapterStat> Adapters);

public static class AdapterClassifier
{
    private static readonly string[] VirtualMarkers =
    {
        "virtual", "vmware", "virtualbox", "hyper-v", "vethernet", "loopback", "bluetooth",
        "pseudo", "isatap", "teredo", "6to4", "npcap", "miniport", "wi-fi direct", "docker", "wsl"
    };

    private static readonly string[] VpnMarkers =
    {
        "vpn", "tap-windows", "tap-", "wireguard", "wintun", "openvpn", "nordlynx", "tailscale",
        "zerotier", "fortinet", "cisco anyconnect", "pulse secure", "globalprotect", "proton", "mullvad", "expressvpn"
    };

    public static bool IsVpn(string name, string description, NetworkInterfaceType type)
    {
        string text = (name + " " + description).ToLowerInvariant();
        if (VpnMarkers.Any(text.Contains)) return true;
        return type == NetworkInterfaceType.Ppp;
    }

    public static bool IsVirtual(string name, string description, NetworkInterfaceType type)
    {
        if (type is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) return true;
        string text = (name + " " + description).ToLowerInvariant();
        // Hyper-V/WSL switch adapters must not be confused with a real card named "Ethernet".
        return VirtualMarkers.Any(text.Contains);
    }
}

/// <summary>
/// Decides which adapters count towards the displayed speed.
/// Automatic mode sums only real (non-virtual, non-VPN) adapters that have a gateway, so
/// traffic that a VPN or virtual switch re-encapsulates is never counted twice.
/// If only a VPN/virtual link is available it is used as a fallback.
/// </summary>
public static class AdapterSelector
{
    public static List<AdapterInfo> Select(IReadOnlyList<AdapterInfo> adapters, bool auto, string? adapterId)
    {
        if (!auto && !string.IsNullOrEmpty(adapterId))
        {
            var chosen = adapters.FirstOrDefault(a => a.Id == adapterId);
            if (chosen != null) return new List<AdapterInfo> { chosen };
            // The chosen adapter vanished (USB card unplugged...): fall back to automatic.
        }

        var live = adapters.Where(a => a.IsUp && a.HasGateway && a.Type != NetworkInterfaceType.Loopback).ToList();
        var physical = live.Where(a => !a.IsVirtual && !a.IsVpn).ToList();
        if (physical.Count > 0) return physical;
        return live;
    }
}
