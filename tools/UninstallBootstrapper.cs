using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class UninstallBootstrapper
{
    private const string AppName = "Project Shortcut Dock";
    private const string StartMenuFolderName = "Project Shortcut Dock";
    private const string RunValueName = "ProjectShortcutDock";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ProjectShortcutDock";

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();

        try
        {
            string installDir = GetValidatedInstallDir();
            string settingsDir = GetSettingsDir();

            if (MessageBox.Show(
                "確定要移除 Project Shortcut Dock 嗎？",
                AppName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return 0;
            }

            DialogResult removeSettingsResult = MessageBox.Show(
                "是否連同設定與捷徑清單一起刪除？" + Environment.NewLine +
                "這會刪除 %APPDATA%\\ProjectShortcutDock。",
                AppName,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (removeSettingsResult == DialogResult.Cancel)
            {
                return 0;
            }

            bool removeSettings = removeSettingsResult == DialogResult.Yes;

            RemoveStartWithWindows();
            RemoveUninstallRegistration();
            RemoveStartMenuShortcuts();
            StartCleanupProcess(installDir, removeSettings ? settingsDir : null);

            MessageBox.Show(
                "已開始移除 Project Shortcut Dock。" + Environment.NewLine +
                "若有檔案仍在使用中，請稍後再確認安裝資料夾是否已刪除。",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "移除失敗：" + Environment.NewLine + ex.Message,
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string GetValidatedInstallDir()
    {
        string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectShortcutDock");

        if (!string.Equals(installDir, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("解除安裝程式只能從正式安裝位置執行。");
        }

        return installDir;
    }

    private static string GetSettingsDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProjectShortcutDock");
    }

    private static void RemoveStartWithWindows()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            true))
        {
            if (key != null)
            {
                key.DeleteValue(RunValueName, false);
            }
        }
    }

    private static void RemoveUninstallRegistration()
    {
        Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, false);
    }

    private static void RemoveStartMenuShortcuts()
    {
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft\\Windows\\Start Menu\\Programs",
            StartMenuFolderName);

        if (!Directory.Exists(startMenuDir))
        {
            return;
        }

        ClearDirectoryAttributes(startMenuDir);
        Directory.Delete(startMenuDir, true);
    }

    private static void ClearDirectoryAttributes(string rootDir)
    {
        foreach (string file in Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }

    private static void StartCleanupProcess(string installDir, string settingsDir)
    {
        string arguments = "/c ping 127.0.0.1 -n 3 > nul" +
            " & rmdir /s /q \"" + installDir + "\"";

        if (!string.IsNullOrWhiteSpace(settingsDir))
        {
            arguments += " & rmdir /s /q \"" + settingsDir + "\"";
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }
}
