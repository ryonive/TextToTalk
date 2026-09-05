using System;
using System.Globalization;
using Dalamud.Game;
using TextToTalk.Resources;

namespace TextToTalk.Localization;

public sealed class Localizer
{
    public Localizer(string uiLanguage)
    {
        SetLanguage(uiLanguage);
    }

    public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;
    public ClientLanguage GameDataLanguage { get; private set; }
    public bool HasGameDataSheet { get; private set; }

    public void SetUiLanguage(string uiLanguage) => SetLanguage(uiLanguage);

    private void SetLanguage(string uiLanguage)
    {
        GameDataLanguage = uiLanguage.ToLowerInvariant() switch
        {
            "ja" => ClientLanguage.Japanese,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            _ => ClientLanguage.English,
        };
        HasGameDataSheet = uiLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
                           || uiLanguage.Equals("ja", StringComparison.OrdinalIgnoreCase)
                           || uiLanguage.Equals("de", StringComparison.OrdinalIgnoreCase)
                           || uiLanguage.Equals("fr", StringComparison.OrdinalIgnoreCase);
        Culture = GetCulture(uiLanguage);
        Strings.Culture = Culture;
    }

    private static CultureInfo GetCulture(string dalamudLanguage)
    {
        var cultureName = dalamudLanguage switch
        {
            "zh" => "zh-Hans",
            "tw" => "zh-Hant",
            _ => dalamudLanguage,
        };

        return CultureInfo.GetCultureInfo(cultureName);
    }

    public string Format(string template, params object?[] args) =>
        string.Format(Culture, template, args);
}
