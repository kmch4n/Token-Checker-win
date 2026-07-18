using System.Globalization;
using System.Resources;

namespace UsageBeacon.Localization;

public static class LocalizationService
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;
    private static readonly ResourceManager Resources = new(
        "UsageBeacon.Resources.Strings",
        typeof(LocalizationService).Assembly);

    private static string _languagePreference = "system";
    private static CultureInfo _culture = ResolveCulture("system");

    public static event Action? LanguageChanged;

    public static string LanguagePreference => _languagePreference;

    public static CultureInfo Culture => _culture;

    public static IReadOnlyList<LanguageOption> SupportedLanguages =>
        new[] { new LanguageOption("system", Get("LanguageSystemDefault")) }
            .Concat(LanguageCatalog.All.Select(language => new LanguageOption(
                language.Code,
                GetForCulture("LanguageNativeName", language.CultureName))))
            .ToArray();

    public static string NormalizePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference) || preference == "system") return "system";
        return LanguageCatalog.Find(preference)?.Code ?? LanguageCatalog.English.Code;
    }

    public static void SetLanguage(string? preference)
    {
        var normalized = NormalizePreference(preference);
        var culture = ResolveCulture(normalized);
        if (_languagePreference == normalized && _culture.Name == culture.Name) return;

        _languagePreference = normalized;
        _culture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
        => Resources.GetString(key, _culture)
           ?? Resources.GetString(key, CultureInfo.GetCultureInfo(LanguageCatalog.English.CultureName))
           ?? $"[{key}]";

    public static string Format(string key, params object?[] args)
        => string.Format(_culture, Get(key), args);

    internal static CultureInfo ResolveCulture(string? preference)
    {
        var normalized = NormalizePreference(preference);
        if (normalized != "system")
            return CultureInfo.GetCultureInfo(LanguageCatalog.Find(normalized)!.CultureName);

        return LanguageCatalog.Find(SystemUiCulture) != null
            ? SystemUiCulture
            : CultureInfo.GetCultureInfo(LanguageCatalog.English.CultureName);
    }

    private static string GetForCulture(string key, string cultureName)
        => Resources.GetString(key, CultureInfo.GetCultureInfo(cultureName))
           ?? Resources.GetString(key, CultureInfo.GetCultureInfo(LanguageCatalog.English.CultureName))
           ?? $"[{key}]";
}
