using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ProjectShortcutDock;

public static class CardColorPalette
{
    public const string DefaultKey = "sky";

    private static readonly CardColorDefinition[] Definitions =
    {
        new("slate", Color.FromRgb(100, 116, 139)),
        new("sky", Color.FromRgb(14, 165, 233)),
        new("emerald", Color.FromRgb(16, 185, 129)),
        new("amber", Color.FromRgb(245, 158, 11)),
        new("rose", Color.FromRgb(244, 63, 94)),
        new("violet", Color.FromRgb(139, 92, 246))
    };

    public static IReadOnlyList<CardColorOption> Build(Func<string, string> text)
    {
        return Definitions
            .Select(definition => new CardColorOption(
                definition.Key,
                text(GetDisplayTextKey(definition.Key)),
                definition.AccentColor,
                CreateBackgroundColor(definition.AccentColor)))
            .ToArray();
    }

    public static CardColorOption Resolve(string? key)
    {
        var definition = Definitions.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? Definitions.First(x => x.Key == DefaultKey);
        return new CardColorOption(
            definition.Key,
            definition.Key,
            definition.AccentColor,
            CreateBackgroundColor(definition.AccentColor));
    }

    private static string GetDisplayTextKey(string key) => key switch
    {
        "slate" => "CardColorSlate",
        "emerald" => "CardColorEmerald",
        "amber" => "CardColorAmber",
        "rose" => "CardColorRose",
        "violet" => "CardColorViolet",
        _ => "CardColorSky"
    };

    private static Color CreateBackgroundColor(Color accentColor)
    {
        return Color.FromArgb(34, accentColor.R, accentColor.G, accentColor.B);
    }

    private sealed record CardColorDefinition(string Key, Color AccentColor);
}

public sealed record CardColorOption
{
    public CardColorOption(string key, string displayName, Color accentColor, Color backgroundColor)
    {
        Key = key;
        DisplayName = displayName;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        AccentBrush = CreateBrush(accentColor);
    }

    public string Key { get; }

    public string DisplayName { get; }

    public Color AccentColor { get; }

    public Color BackgroundColor { get; }

    public Brush AccentBrush { get; }

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
