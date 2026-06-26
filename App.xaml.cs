using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ProjectShortcutDock;

public partial class App : Application
{
    private DockManager? _dockManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        ClearPreviousErrorLog();
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogException(args.ExceptionObject as Exception);
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        _dockManager = new DockManager(this);
        _dockManager.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _dockManager?.Dispose();
        base.OnExit(e);
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        MessageBox.Show(e.Exception.Message, $"{UiText.Get(UiText.DefaultLanguage, "AppTitle")} startup error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void LogException(Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        Directory.CreateDirectory(AppSettings.DirectoryPath);
        File.WriteAllText(Path.Combine(AppSettings.DirectoryPath, "last-error.log"), exception.ToString());
    }

    private static void ClearPreviousErrorLog()
    {
        var path = Path.Combine(AppSettings.DirectoryPath, "last-error.log");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
