using System.Windows.Media;

namespace ProjectShortcutDock;

public static class CardColorPalette
{
    public static readonly Color DefaultSkyColor = Color.FromRgb(14, 165, 233);

    public static Color FromDrawingColor(System.Drawing.Color color) => Color.FromRgb(color.R, color.G, color.B);

    public static System.Drawing.Color ToDrawingColor(Color color) => System.Drawing.Color.FromArgb(color.R, color.G, color.B);

    public static Color CreateTintBackground(Color accentColor)
    {
        return Color.FromArgb(34, accentColor.R, accentColor.G, accentColor.B);
    }

    public static Color Blend(Color baseColor, Color accentColor, double accentWeight)
    {
        var clamped = accentWeight < 0 ? 0 : accentWeight > 1 ? 1 : accentWeight;
        var baseWeight = 1 - clamped;
        return Color.FromArgb(
            255,
            (byte)((baseColor.R * baseWeight) + (accentColor.R * clamped)),
            (byte)((baseColor.G * baseWeight) + (accentColor.G * clamped)),
            (byte)((baseColor.B * baseWeight) + (accentColor.B * clamped)));
    }

    public static Color ResolveLegacyColor(string? colorKey) => colorKey?.ToLowerInvariant() switch
    {
        "slate" => Color.FromRgb(100, 116, 139),
        "emerald" => Color.FromRgb(16, 185, 129),
        "amber" => Color.FromRgb(245, 158, 11),
        "rose" => Color.FromRgb(244, 63, 94),
        "violet" => Color.FromRgb(139, 92, 246),
        _ => DefaultSkyColor
    };
}
