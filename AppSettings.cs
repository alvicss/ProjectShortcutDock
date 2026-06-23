using System;
using System.Collections.Generic;
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
    public string WindowMode { get; set; } = "Desktop";
    public string Language { get; set; } = "zh-TW";
    public bool StartWithWindows { get; set; }
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 360;
    public double Height { get; set; } = 260;
    public List<ShortcutItem> Shortcuts { get; set; } = new();

    public static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectShortcutDock");

    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
