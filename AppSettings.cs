using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ProjectShortcutDock;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string Theme { get; set; } = ThemePalette.Normal;
    public string WindowMode { get; set; } = "Topmost";
    public string Language { get; set; } = "en";
    public string TerminalShell { get; set; } = "cmd";
    public bool StartWithWindows { get; set; }
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 360;
    public double Height { get; set; } = 260;
    public List<ShortcutItem> Shortcuts { get; set; } = new();
    public List<ShortcutGroup> Groups { get; set; } = new();

    public static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectShortcutDock");

    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return CreateFirstRunDefaults();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateFirstRunDefaults();
        }
        catch
        {
            return CreateFirstRunDefaults();
        }
    }

    private static AppSettings CreateFirstRunDefaults()
    {
        return new AppSettings
        {
            Theme = ThemePalette.Normal,
            WindowMode = "Topmost",
            Language = DetectDefaultLanguage()
        };
    }

    private static string DetectDefaultLanguage()
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-TW";
        }

        if (cultureName.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            return "ja";
        }

        return "en";
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
