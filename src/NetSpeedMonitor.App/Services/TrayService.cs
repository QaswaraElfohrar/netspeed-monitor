using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using NetSpeedMonitor.Core.Localization;

namespace NetSpeedMonitor.App.Services;

/// <summary>System tray icon + its context menu (built from the controller's shared menu model).</summary>
public sealed class TrayService : IDisposable
{
    private readonly AppController _app;
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu = new();
    private long _lastTipStamp;
    private string _lastTip = "";

    public TrayService(AppController app)
    {
        _app = app;
        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = Loc.Instance["app.name"],
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _menu.Opening += (_, _) => RebuildMenu();
        _icon.DoubleClick += (_, _) =>
        {
            if (_app.Settings.DashboardOnDoubleClick) _app.ShowDashboard();
        };
    }

    private static Icon LoadIcon()
    {
        try
        {
            var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
            if (info != null)
            {
                using var s = info.Stream;
                return new Icon(s, SystemInformation.SmallIconSize);
            }
        }
        catch (Exception ex)
        {
            Core.Diagnostics.Logger.Error("Tray icon failed to load", ex);
        }
        return SystemIcons.Application;
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        _menu.RightToLeft = Loc.Instance.IsRtl ? RightToLeft.Yes : RightToLeft.No;
        foreach (var entry in _app.MenuEntries)
        {
            if (entry.SeparatorBefore) _menu.Items.Add(new ToolStripSeparator());
            var item = new ToolStripMenuItem(entry.Text())
            {
                CheckOnClick = false,
                Checked = entry.IsChecked?.Invoke() ?? false,
            };
            var action = entry.Action;
            item.Click += (_, _) => action();
            _menu.Items.Add(item);
        }
    }

    /// <summary>Tooltip with the current speeds; refreshed at most every 3 seconds.</summary>
    public void SetTooltip(string down, string up)
    {
        long now = Stopwatch.GetTimestamp();
        if ((now - _lastTipStamp) / (double)Stopwatch.Frequency < 3) return;
        _lastTipStamp = now;
        string tip = Loc.Instance.Format("tray.tooltip", down, up);
        if (tip == _lastTip) return;
        _lastTip = tip;
        _icon.Text = tip.Length > 120 ? tip[..120] : tip;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
