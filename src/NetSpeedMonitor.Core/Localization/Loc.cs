using System.ComponentModel;
using System.Globalization;

namespace NetSpeedMonitor.Core.Localization;

/// <summary>Runtime language switch (English / Arabic / French). Bindable: {Binding [key], Source=Loc.Instance}.</summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private static readonly string[] Codes = { "en", "ar", "fr" };
    private int _lang;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language => Codes[_lang];
    public bool IsRtl => _lang == 1;

    public string this[string key] => Strings.Get(key, _lang);

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, this[key], args);

    /// <param name="setting">"system", "en", "ar" or "fr"</param>
    public void SetLanguage(string setting)
    {
        int idx = setting switch
        {
            "en" => 0,
            "ar" => 1,
            "fr" => 2,
            _ => FromSystem()
        };

        if (idx == _lang && PropertyChanged != null) return;
        _lang = idx;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRtl)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }

    private static int FromSystem() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "ar" => 1,
        "fr" => 2,
        _ => 0
    };
}
