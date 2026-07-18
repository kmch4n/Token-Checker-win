using System.Globalization;

namespace UsageBeacon.Localization;

internal sealed record LanguageDefinition(string Code, string CultureName);

internal static class LanguageCatalog
{
    public static IReadOnlyList<LanguageDefinition> All { get; } =
    [
        new("en", "en-US"),
        new("ja", "ja-JP"),
    ];

    public static LanguageDefinition English => All[0];

    public static LanguageDefinition? Find(string? code)
        => All.FirstOrDefault(language => language.Code == code);

    public static LanguageDefinition? Find(CultureInfo culture)
        => Find(culture.TwoLetterISOLanguageName);
}
