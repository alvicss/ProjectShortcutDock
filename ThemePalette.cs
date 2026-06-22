using System.Collections.Generic;
using System.Windows.Media;

namespace ProjectShortcutDock;

public sealed record ThemePalette(
    Color Window,
    Color Panel,
    Color Text,
    Color SubtleText,
    Color Border,
    Color Hover,
    Color Shadow,
    double ShadowOpacity)
{
    public const string Normal = "Normal";
    public const string Dark = "Dark";
    public const string Glass = "Glass";
    public const string Tech = "Tech";
    public const string Aero = "Aero";

    public static IReadOnlyList<string> Names { get; } = new[] { Normal, Dark, Glass, Tech, Aero };

    public static ThemePalette For(string? name) => name switch
    {
        Dark => new ThemePalette(
            Color.FromRgb(27, 31, 36),
            Color.FromRgb(37, 43, 50),
            Color.FromRgb(241, 245, 249),
            Color.FromRgb(148, 163, 184),
            Color.FromRgb(75, 85, 99),
            Color.FromRgb(45, 55, 72),
            Color.FromRgb(0, 0, 0),
            0.35),
        Glass => new ThemePalette(
            Color.FromArgb(176, 250, 252, 255),
            Color.FromArgb(136, 255, 255, 255),
            Color.FromRgb(20, 32, 43),
            Color.FromRgb(71, 85, 105),
            Color.FromArgb(158, 203, 213, 225),
            Color.FromArgb(132, 224, 242, 254),
            Color.FromRgb(15, 23, 42),
            0.18),
        Tech => new ThemePalette(
            Color.FromRgb(8, 22, 32),
            Color.FromRgb(12, 35, 48),
            Color.FromRgb(221, 252, 255),
            Color.FromRgb(125, 211, 252),
            Color.FromRgb(20, 184, 166),
            Color.FromRgb(18, 83, 94),
            Color.FromRgb(0, 229, 255),
            0.28),
        Aero => new ThemePalette(
            Color.FromArgb(218, 232, 244, 255),
            Color.FromArgb(190, 255, 255, 255),
            Color.FromRgb(31, 41, 55),
            Color.FromRgb(75, 85, 99),
            Color.FromRgb(147, 197, 253),
            Color.FromRgb(219, 234, 254),
            Color.FromRgb(59, 130, 246),
            0.2),
        _ => new ThemePalette(
            Color.FromRgb(247, 248, 250),
            Color.FromRgb(255, 255, 255),
            Color.FromRgb(31, 41, 55),
            Color.FromRgb(107, 114, 128),
            Color.FromRgb(209, 213, 219),
            Color.FromRgb(238, 242, 255),
            Color.FromRgb(15, 23, 42),
            0.22)
    };
}
