using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class SetupBootstrapper
{
    private const string AppName = "Project Shortcut Dock";
    private const string ExeName = "ProjectShortcutDock.exe";
    private const string RuntimeDownloadUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.9/windowsdesktop-runtime-10.0.9-win-x64.exe";

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
            CreateStartMenuShortcut(installDir);

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
                "安裝失敗：" + Environment.NewLine + ex.Message,
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
            "此程式需要 .NET 10 Desktop Runtime。現在要從 Microsoft 官方網站下載並安裝嗎？",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            throw new InvalidOperationException("未安裝 .NET 10 Desktop Runtime。");
        }

        string installerPath = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-10.0.9-win-x64.exe");
        using (var client = new WebClient())
        {
            client.DownloadFile(RuntimeDownloadUrl, installerPath);
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/install /quiet /norestart",
            UseShellExecute = true,
            Verb = "runas"
        });

        if (process == null)
        {
            throw new InvalidOperationException("無法啟動 .NET Desktop Runtime 安裝程式。");
        }

        process.WaitForExit();
        if (process.ExitCode != 0 && process.ExitCode != 3010)
        {
            throw new InvalidOperationException(".NET Desktop Runtime 安裝失敗，ExitCode=" + process.ExitCode + "。");
        }

        if (!HasDesktopRuntime10())
        {
            throw new InvalidOperationException("找不到 .NET 10 Desktop Runtime。請重新啟動電腦或手動安裝後再試。");
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

    private static void ExtractApp(string installDir)
    {
        string tempZip = Path.Combine(Path.GetTempPath(), "ProjectShortcutDock-app.zip");
        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.zip"))
        {
            if (resource == null)
            {
                throw new InvalidOperationException("安裝檔缺少內嵌 app.zip。");
            }

            using (var output = File.Create(tempZip))
            {
                resource.CopyTo(output);
            }
        }

        if (Directory.Exists(installDir))
        {
            foreach (string file in Directory.GetFiles(installDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

        ZipFile.ExtractToDirectory(tempZip, installDir);
    }

    private static void CreateStartMenuShortcut(string installDir)
    {
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft\\Windows\\Start Menu\\Programs\\Project Shortcut Dock");
        Directory.CreateDirectory(startMenuDir);

        string shortcutPath = Path.Combine(startMenuDir, "Project Shortcut Dock.lnk");
        string targetPath = Path.Combine(installDir, ExeName);

        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType);
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = installDir;
        shortcut.IconLocation = targetPath + ",0";
        shortcut.Save();
    }
}
