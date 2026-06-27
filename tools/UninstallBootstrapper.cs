using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

internal static class UninstallBootstrapper
{
    private const string AppName = "Lazy Shortcut";
    private const string ExeName = "LazyShortcut.exe";
    private const string LegacyExeName = "ProjectShortcutDock.exe";
    private const string StartMenuFolderName = "Lazy Shortcut";
    private const string LegacyStartMenuFolderName = "Project Shortcut Dock";
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
                "確定要移除 Lazy Shortcut 嗎？",
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

            EnsureAppHasExited(installDir);
            RemoveStartWithWindows();
            RemoveStartMenuShortcuts();
            StartCleanupProcess(Process.GetCurrentProcess().Id, installDir, removeSettings ? settingsDir : null);

            MessageBox.Show(
                "已啟動 Lazy Shortcut 的移除程序。" + Environment.NewLine +
                "關閉此視窗後，系統會繼續清理安裝資料夾。" + Environment.NewLine +
                "只有在安裝資料夾刪除完成後，這個程式才會從已安裝的應用程式清單中移除。",
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

    private static void EnsureAppHasExited(string installDir)
    {
        EnsureInstalledProcessIsClosed(installDir, ExeName);
        EnsureInstalledProcessIsClosed(installDir, LegacyExeName);
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
        RemoveStartMenuFolder(StartMenuFolderName);
        RemoveStartMenuFolder(LegacyStartMenuFolderName);
    }

    private static void ClearDirectoryAttributes(string rootDir)
    {
        foreach (string file in Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }

    private static void StartCleanupProcess(int uninstallProcessId, string installDir, string settingsDir)
    {
        string scriptPath = Path.Combine(
            Path.GetTempPath(),
            "LazyShortcut-uninstall-" + Guid.NewGuid().ToString("N") + ".ps1");
        File.WriteAllText(scriptPath, BuildCleanupScript(uninstallProcessId, installDir, settingsDir, scriptPath), new UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static string TryGetProcessPath(Process process)
    {
        try
        {
            return process.MainModule != null ? process.MainModule.FileName : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildCleanupScript(int uninstallProcessId, string installDir, string settingsDir, string scriptPath)
    {
        string installDirLiteral = EscapePowerShellSingleQuotedString(installDir);
        string settingsDirLiteral = EscapePowerShellSingleQuotedString(settingsDir ?? string.Empty);
        string scriptPathLiteral = EscapePowerShellSingleQuotedString(scriptPath);

        return
            "$ErrorActionPreference = 'SilentlyContinue'" + Environment.NewLine +
            "$uninstallProcessId = " + uninstallProcessId + Environment.NewLine +
            "$installDir = '" + installDirLiteral + "'" + Environment.NewLine +
            "$settingsDir = '" + settingsDirLiteral + "'" + Environment.NewLine +
            "$scriptPath = '" + scriptPathLiteral + "'" + Environment.NewLine +
            "$uninstallRegistryPath = 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ProjectShortcutDock'" + Environment.NewLine +
            "function Wait-ForProcessExit([int]$processId) {" + Environment.NewLine +
            "    for ($attempt = 0; $attempt -lt 120; $attempt++) {" + Environment.NewLine +
            "        if (-not (Get-Process -Id $processId -ErrorAction SilentlyContinue)) { return }" + Environment.NewLine +
            "        Start-Sleep -Milliseconds 500" + Environment.NewLine +
            "    }" + Environment.NewLine +
            "}" + Environment.NewLine +
            "function Remove-DirectoryWithRetries([string]$path) {" + Environment.NewLine +
            "    if ([string]::IsNullOrWhiteSpace($path)) { return $true }" + Environment.NewLine +
            "    for ($attempt = 0; $attempt -lt 20; $attempt++) {" + Environment.NewLine +
            "        if (-not (Test-Path -LiteralPath $path)) { return $true }" + Environment.NewLine +
            "        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {" + Environment.NewLine +
            "            if (-not $_.PSIsContainer) { [System.IO.File]::SetAttributes($_.FullName, [System.IO.FileAttributes]::Normal) }" + Environment.NewLine +
            "        }" + Environment.NewLine +
            "        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue" + Environment.NewLine +
            "        Start-Sleep -Milliseconds 500" + Environment.NewLine +
            "    }" + Environment.NewLine +
            "    return -not (Test-Path -LiteralPath $path)" + Environment.NewLine +
            "}" + Environment.NewLine +
            "Wait-ForProcessExit -processId $uninstallProcessId" + Environment.NewLine +
            "$installRemoved = Remove-DirectoryWithRetries -path $installDir" + Environment.NewLine +
            "if ($installRemoved) {" + Environment.NewLine +
            "    Remove-Item -LiteralPath $uninstallRegistryPath -Recurse -Force -ErrorAction SilentlyContinue" + Environment.NewLine +
            "    Remove-DirectoryWithRetries -path $settingsDir | Out-Null" + Environment.NewLine +
            "}" + Environment.NewLine +
            "Start-Sleep -Milliseconds 500" + Environment.NewLine +
            "Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue" + Environment.NewLine;
    }

    private static void EnsureInstalledProcessIsClosed(string installDir, string exeName)
    {
        string installedExePath = Path.Combine(installDir, exeName);
        string processName = Path.GetFileNameWithoutExtension(exeName);

        foreach (Process process in Process.GetProcessesByName(processName))
        {
            try
            {
                string processPath = TryGetProcessPath(process);
                if (!string.Equals(processPath, installedExePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(5000))
                    {
                        throw new InvalidOperationException(
                            "偵測到 Lazy Shortcut 仍在執行。" + Environment.NewLine +
                            "請先從系統匣結束程式，或在工作管理員中關閉 " + exeName + " 後再試一次。");
                    }
                }
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void RemoveStartMenuFolder(string folderName)
    {
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft\\Windows\\Start Menu\\Programs",
            folderName);

        if (!Directory.Exists(startMenuDir))
        {
            return;
        }

        ClearDirectoryAttributes(startMenuDir);
        Directory.Delete(startMenuDir, true);
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''");
    }
}
