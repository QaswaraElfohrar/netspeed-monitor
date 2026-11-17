# NetSpeed Monitor

Real-time download / upload speed next to the Windows clock. Lightweight (.NET 8 + WPF), no telemetry, no admin rights, no network access.
Languages: **English · العربية (RTL) · Français** — switch live in Settings → General.

```
↓ 2.45 MB/s   ↑ 850 KB/s        ← sits left of the system tray, on the taskbar
```

## Download / run

| File | What |
|------|------|
| `dist\NetSpeedMonitor\NetSpeedMonitor.exe` | Self-contained single exe (Windows 10/11 x64). Nothing else to install. |
| `dist\NetSpeedMonitor-1.0.0-portable-win-x64.zip` | Same exe + `portable.txt` → settings/history stay in a `data` folder next to it. |
| `dist\NetSpeedMonitor-1.0.0-setup.exe` | Installer (created when Inno Setup 6 is installed, see below). |

Double-click the exe. The speed widget appears beside the clock and a tray icon is added.

## Using it

* **Widget**: drag to move (turns off automatic alignment), double-click = dashboard, right-click = menu. It never takes keyboard focus.
* **Tray icon**: right-click → Show/Hide, Network Statistics, Settings, Reset Statistics, Start with Windows, About, Exit. Double-click → dashboard.
* **Dashboard**: *Live* (speeds, 2-minute graph, session totals, adapter, status) · *Usage History* (today / week / month / total + last 30 days) · *Adapters* (automatic or pick one).
* Starting the exe a second time just brings the dashboard of the running instance to the front.

## Settings (`%AppData%\NetSpeedMonitor\settings.json`, or `data\settings.json` in portable mode)

| Key | Values | Meaning |
|-----|--------|---------|
| `Language` | `system` `en` `ar` `fr` | UI language |
| `Theme` | `system` `dark` `light` | Dashboard/settings theme and widget default background |
| `FontFamily`, `FontSize` | e.g. `Segoe UI`, `12` | Widget font |
| `DownloadColor`, `UploadColor` | `#RRGGBB` | Widget text colours (default green / blue) |
| `BackgroundColor` | `#RRGGBB` or empty | Empty = follows the theme |
| `TransparentBackground`, `BackgroundOpacity` | `true`, `0..1` | Opacity applies when not transparent |
| `Layout` | `horizontal` `vertical` | Side by side or stacked (a vertical taskbar always stacks) |
| `WidgetWidth` | `0..600` | DIPs; `0` = automatic |
| `RefreshIntervalMs` | `500` `1000` `2000` | Sampling interval |
| `AutoAdapter`, `AdapterId` | `true`/`false`, id | Automatic or one chosen adapter |
| `Unit`, `Precision` | `bytes`/`bits`, `0..3` | B/s–GB/s or bps–Gbps |
| `ShowDownload`, `ShowUpload` | bool | |
| `WidgetVisible`, `AutoPosition`, `ManualX/Y`, `LockPosition`, `AlwaysOnTop` | | Position (pixels). In automatic mode the widget is always top-most |
| `StartWithWindows`, `StartMinimized`, `MinimizeToTray`, `DashboardOnDoubleClick` | bool | |

Usage history is `history.json` in the same folder (one entry per day, saved every minute and on exit). Errors go to `error.log`.
"Check for updates" is intentionally absent: there is no update server configured, and the app makes no network connections.

## How it works / Windows limitations (please read)

* **The widget is a normal borderless top-most window aligned to the taskbar — not a native taskbar component.** Windows 10/11 has no supported API to put text inside the taskbar (the old "deskbands" were removed in Windows 11), and injecting into Explorer is unstable, so this app does not do it.
* It finds the taskbar (`Shell_TrayWnd`) and the notification area (`TrayNotifyWnd`) and sits just left of the tray. Position is re-checked every second, and after display changes, DPI changes, sleep/resume and Explorer restarts (`TaskbarCreated`).
* Auto-hidden taskbar: the widget hides while the taskbar is hidden. It also hides while a full-screen app/game or presentation is active.
* Multiple monitors: automatic mode follows the primary taskbar. For another monitor, drag the widget there (manual position is kept inside the monitor it is on).
* Speeds = difference of the interface byte counters (`GetIfEntry2`, 64-bit) divided by the real elapsed time (monotonic `Stopwatch`). Counter resets, 32-bit wrap, sleep/resume gaps and adapter unplug/replug are handled without spikes.
* **No double counting**: automatic mode sums only real adapters that have a gateway and ignores VPN/tunnel/virtual adapters (Hyper-V, VMware, WSL, TAP, WireGuard...), whose traffic is already counted on the physical card. If *only* a VPN/virtual link is up it is used as a fallback. With a VPN connected the figure therefore includes the VPN's encryption overhead. You can pick any single adapter manually in Dashboard → Adapters or Settings → Monitoring.
* Windows 11 25H2 taskbar changes could in the future move the notification area; the fallback is manual position.

## Build from source

Requires the **.NET 8 SDK**. (Installer additionally needs [Inno Setup 6](https://jrsoftware.org/isinfo.php).)

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1      # tests -> single-file exe -> portable zip -> installer
dotnet test tests/NetSpeedMonitor.Tests                  # unit tests only
dotnet run --project src/NetSpeedMonitor.App             # run from source
```

Open `NetSpeedMonitor.sln` in Visual Studio 2022 to edit.

## Project layout

```
NetSpeedMonitor.sln
src/NetSpeedMonitor.Core/   monitoring engine (SpeedCalculator, adapter selection, NetworkMonitor), settings, history, localization — no UI
src/NetSpeedMonitor.App/    WPF app: Views, ViewModels (MVVM), Services (tray, taskbar positioning, startup, themes), Controls (graph)
tests/NetSpeedMonitor.Tests xUnit: speed maths, formatting, adapter selection, storage, translations
installer/                  Inno Setup script        tools/make-icon.ps1  icon generator
```

## Privacy

No telemetry, no network connections, no admin rights. Everything stays in the data folder above.

## License
MIT — see [LICENSE](LICENSE). Ready-made installer: [apps.qaswaraelfohrar.com](https://apps.qaswaraelfohrar.com) · by [Qaswara Elfohrar IT](https://qaswaraelfohrar.com)
