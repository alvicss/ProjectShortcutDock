using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

internal static class SetupBootstrapper
{
    private const string AppName = "Project Shortcut Dock";
    private const string ExeName = "ProjectShortcutDock.exe";
    private const string UninstallExeName = "ProjectShortcutDock.Uninstall.exe";
    private const string PublisherName = "alvicss";
    private const string StartMenuFolderName = "Project Shortcut Dock";
    private const string StartWithWindowsRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ProjectShortcutDock";
    private const string RuntimeInstallerResourceName = "dotnet-runtime-installer.exe";
    private const string RuntimeInstallerFileName = "windowsdesktop-runtime-10.0.9-win-x64.exe";
    private const string RuntimeManualInstallUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.9-windows-x64-installer";

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();

        try
        {
            EnsureDesktopRuntime();

            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProjectShortcutDock");
            Directory.CreateDirectory(installDir);

            ExtractApp(installDir);
            CreateStartMenuShortcuts(installDir);
            RegisterUninstallInfo(installDir);

            Process.Start(Path.Combine(installDir, ExeName));
            MessageBox.Show(
                AppName + " 已安裝完成。",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                BuildFailureMessage(ex),
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void EnsureDesktopRuntime()
    {
        if (HasDesktopRuntime10())
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            "此程式需要 .NET 10 Desktop Runtime。現在要安裝內嵌的 Microsoft 官方離線安裝包嗎？",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            throw new InvalidOperationException("未安裝 .NET 10 Desktop Runtime。");
        }

        string installerPath = Path.Combine(Path.GetTempPath(), RuntimeInstallerFileName);
        try
        {
            ExtractEmbeddedFile(RuntimeInstallerResourceName, installerPath);
        }
        catch (Exception ex)
        {
            throw new RuntimeInstallException(
                "無法準備 .NET 10 Desktop Runtime 安裝檔。" + Environment.NewLine +
                "原因：" + ex.Message,
                ex);
        }

        Process process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/install /quiet /norestart",
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Exception ex)
        {
            throw new RuntimeInstallException(
                "無法啟動 .NET 10 Desktop Runtime 安裝程式。" + Environment.NewLine +
                "原因：" + ex.Message,
                ex);
        }

        if (process == null)
        {
            throw new RuntimeInstallException("無法啟動 .NET 10 Desktop Runtime 安裝程式。");
        }

        process.WaitForExit();
        if (process.ExitCode != 0 && process.ExitCode != 3010)
        {
            throw new RuntimeInstallException(".NET 10 Desktop Runtime 安裝失敗，ExitCode=" + process.ExitCode + "。");
        }

        if (!HasDesktopRuntime10())
        {
            throw new RuntimeInstallException("找不到 .NET 10 Desktop Runtime。請重新啟動電腦或手動安裝後再試。");
        }
    }

    private static bool HasDesktopRuntime10()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string runtimeDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
        if (Directory.Exists(runtimeDir))
        {
            foreach (string dir in Directory.GetDirectories(runtimeDir, "10.*"))
            {
                if (Directory.Exists(dir))
                {
                    return true;
                }
            }
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output.IndexOf("Microsoft.WindowsDesktop.App 10.", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildFailureMessage(Exception ex)
    {
        string message = "安裝失敗：" + Environment.NewLine + ex.Message;
        if (ex is RuntimeInstallException)
        {
            message += Environment.NewLine + Environment.NewLine +
                "請先手動安裝 .NET 10 Desktop Runtime (x64) 後，再重新執行安裝程式：" +
                Environment.NewLine + RuntimeManualInstallUrl;
        }

        return message;
    }

    private static void ExtractEmbeddedFile(string resourceName, string destinationPath)
    {
        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (resource == null)
            {
                throw new InvalidOperationException("找不到內嵌資源：" + resourceName + "。");
            }

            using (var output = File.Create(destinationPath))
            {
                resource.CopyTo(output);
            }
        }
    }

    private static void ExtractApp(string installDir)
    {
        string tempZip = Path.Combine(Path.GetTempPath(), "ProjectShortcutDock-app.zip");
        ExtractEmbeddedFile("app.zip", tempZip);

        if (Directory.Exists(installDir))
        {
            foreach (string file in Directory.GetFiles(installDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

        ZipFile.ExtractToDirectory(tempZip, installDir);
    }

    private static void CreateStartMenuShortcuts(string installDir)
    {
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft\\Windows\\Start Menu\\Programs",
            StartMenuFolderName);
        Directory.CreateDirectory(startMenuDir);

        string targetPath = Path.Combine(installDir, ExeName);
        string uninstallPath = Path.Combine(installDir, UninstallExeName);

        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType);
        CreateShortcut(shell, Path.Combine(startMenuDir, "Project Shortcut Dock.lnk"), targetPath, installDir, targetPath + ",0");
        if (File.Exists(uninstallPath))
        {
            CreateShortcut(shell, Path.Combine(startMenuDir, "Uninstall Project Shortcut Dock.lnk"), uninstallPath, installDir, targetPath + ",0");
        }
    }

    private static void CreateShortcut(dynamic shell, string shortcutPath, string targetPath, string workingDirectory, string iconLocation)
    {
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = iconLocation;
        shortcut.Save();
    }

    private static void RegisterUninstallInfo(string installDir)
    {
        string targetPath = Path.Combine(installDir, ExeName);
        string uninstallPath = Path.Combine(installDir, UninstallExeName);
        string displayVersion = FileVersionInfo.GetVersionInfo(targetPath).ProductVersion ?? "0.1.2";

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath))
        {
            if (key == null)
            {
                throw new InvalidOperationException("無法建立解除安裝登錄資訊。");
            }

            key.SetValue("DisplayName", AppName);
            key.SetValue("DisplayVersion", displayVersion);
            key.SetValue("Publisher", PublisherName);
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", targetPath);
            key.SetValue("UninstallString", "\"" + uninstallPath + "\"");
            key.SetValue("QuietUninstallString", "\"" + uninstallPath + "\"");
            key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartWithWindowsRegistryPath, true))
        {
            if (key == null)
            {
                return;
            }

            object existingValue = key.GetValue("ProjectShortcutDock");
            if (existingValue == null)
            {
                return;
            }

            key.SetValue("ProjectShortcutDock", "\"" + targetPath + "\"");
        }
    }

    private sealed class RuntimeInstallException : Exception
    {
        public RuntimeInstallException(string message)
            : base(message)
        {
        }

        public RuntimeInstallException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
