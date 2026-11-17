using System.Diagnostics;
using System.Windows;
using NetSpeedMonitor.App.Services;
using NetSpeedMonitor.Core.Localization;

namespace NetSpeedMonitor.App.Views;

public partial class AboutWindow : Window
{
    public const string WebsiteUrl = "https://qaswaraelfohrar.com";
    public const string FacebookUrl = "https://www.facebook.com/QaswaraElfohrar";

    public AboutWindow()
    {
        InitializeComponent();
        string version = typeof(AboutWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        VersionText.Text = Loc.Instance.Format("about.version", version);
        SourceInitialized += (_, _) => ThemeService.AttachTitleBar(this);
        Loaded += (_, _) => { Topmost = true; Topmost = false; Activate(); }; // come to the front
    }

    private static void Open(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { Core.Diagnostics.Logger.Error("Could not open " + url, ex); }
    }

    private void OnWebsite(object sender, RoutedEventArgs e) => Open(WebsiteUrl);
    private void OnFacebook(object sender, RoutedEventArgs e) => Open(FacebookUrl);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
