namespace TokenChecker.Utilities;

public enum PopupTransparency
{
    Percent75,
    Percent55,
    Percent35,
    Percent15,
    Percent0,
}

public static class PopupTransparencyExtensions
{
    public static byte ToAlpha(this PopupTransparency transparency) => transparency switch
    {
        PopupTransparency.Percent75 => 0x40,
        PopupTransparency.Percent55 => 0x70,
        PopupTransparency.Percent35 => 0xA6,
        PopupTransparency.Percent15 => 0xD9,
        PopupTransparency.Percent0  => 0xFF,
        _                           => 0x70,
    };

    public static string ToLabel(this PopupTransparency transparency) => transparency switch
    {
        PopupTransparency.Percent75 => "75% (薄い)",
        PopupTransparency.Percent55 => "55%",
        PopupTransparency.Percent35 => "35%",
        PopupTransparency.Percent15 => "15% (濃い)",
        PopupTransparency.Percent0  => "0% (不透明)",
        _                           => transparency.ToString(),
    };

    public static readonly PopupTransparency Default = PopupTransparency.Percent55;

    public static readonly PopupTransparency[] All =
    [
        PopupTransparency.Percent75,
        PopupTransparency.Percent55,
        PopupTransparency.Percent35,
        PopupTransparency.Percent15,
        PopupTransparency.Percent0,
    ];
}
