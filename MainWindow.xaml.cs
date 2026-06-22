using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace ProjectShortcutDock;

public partial class MainWindow : Window
{
    private const string DesktopWindowMode = "Desktop";
    private const string TopmostWindowMode = "Topmost";
    private static readonly IntPtr HwndBottom = new(1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    private readonly AppSettings _settings;
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _allowClose;
    private bool _isLoading = true;
    private IntPtr _windowHandle;

    public ObservableCollection<ShortcutItem> Shortcuts { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        DataContext = this;
        ShortcutList.ContextMenu = CreateShortcutContextMenu();

        ConfigureSettingsControls();
        LoadShortcuts();
        ApplySavedWindowPosition();
        ApplyTheme(_settings.Theme);
        ApplyWindowMode();
        Opacity = 0.68;

        _trayIcon = CreateTrayIcon();
        _isLoading = false;
    }

    private void ConfigureSettingsControls()
    {
        ThemeBox.ItemsSource = ThemePalette.Names;
        ThemeBox.SelectedItem = _settings.Theme;
        WindowModeBox.ItemsSource = new[] { DesktopWindowMode, TopmostWindowMode };
        WindowModeBox.SelectedItem = NormalizeWindowMode(_settings.WindowMode);
        StartWithWindowsBox.IsChecked = _settings.StartWithWindows;
    }

    private void LoadShortcuts()
    {
        foreach (var item in _settings.Shortcuts.Where(x => Directory.Exists(x.Path)))
        {
            item.RefreshIcon();
            Shortcuts.Add(item);
        }
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowFromTray());
        menu.Items.Add("Hide", null, (_, _) => Hide());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        var icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Project Shortcuts",
            Visible = true,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private ContextMenu CreateShortcutContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem("Open", OpenMenuItem_Click));
        menu.Items.Add(CreateMenuItem("Open parent folder", OpenParentMenuItem_Click));
        menu.Items.Add(CreateMenuItem("Copy path", CopyPathMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Change icon...", ChangeIconMenuItem_Click));
        menu.Items.Add(CreateMenuItem("Reset icon", ResetIconMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Remove shortcut", RemoveMenuItem_Click));
        return menu;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler clickHandler)
    {
        var item = new MenuItem { Header = header };
        item.Click += clickHandler;
        return item;
    }

    private void AddFolders(string[] paths)
    {
        foreach (var path in paths.Where(Directory.Exists))
        {
            var normalizedPath = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            if (Shortcuts.Any(x => string.Equals(x.Path, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var item = new ShortcutItem
            {
                Path = normalizedPath,
                Name = System.IO.Path.GetFileName(normalizedPath)
            };
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                item.Name = normalizedPath;
            }
            item.RefreshIcon();
            Shortcuts.Add(item);
        }

        SaveSettings();
    }

    private void RemoveSelectedShortcut()
    {
        if (ShortcutList.SelectedItem is not ShortcutItem selected)
        {
            return;
        }

        Shortcuts.Remove(selected);
        SaveSettings();
    }

    private void OpenSelectedShortcut()
    {
        if (ShortcutList.SelectedItem is ShortcutItem selected)
        {
            OpenFolder(selected.Path);
        }
    }

    private static void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            MessageBox.Show("This folder no longer exists.", "Project Shortcuts", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void SaveSettings()
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Shortcuts = Shortcuts.ToList();
        _settings.Theme = ThemeBox.SelectedItem?.ToString() ?? _settings.Theme;
        _settings.WindowMode = WindowModeBox.SelectedItem?.ToString() ?? _settings.WindowMode;
        _settings.StartWithWindows = StartWithWindowsBox.IsChecked == true;
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Width = Width;
        _settings.Height = Height;
        _settings.Save();
    }

    private void ApplySavedWindowPosition()
    {
        Width = _settings.Width > 0 ? _settings.Width : Width;
        Height = _settings.Height > 0 ? _settings.Height : Height;

        if (_settings.Left.HasValue && _settings.Top.HasValue)
        {
            Left = _settings.Left.Value;
            Top = _settings.Top.Value;
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 18;
        Top = workArea.Top + 18;
    }

    private void SendBehindOtherWindows()
    {
        if (NormalizeWindowMode(_settings.WindowMode) == TopmostWindowMode)
        {
            return;
        }

        if (_windowHandle != IntPtr.Zero)
        {
            SetWindowPos(_windowHandle, HwndBottom, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
        }
    }

    private void ApplyWindowMode()
    {
        _settings.WindowMode = NormalizeWindowMode(_settings.WindowMode);
        Topmost = _settings.WindowMode == TopmostWindowMode;

        if (Topmost)
        {
            return;
        }

        SendBehindOtherWindows();
    }

    private static string NormalizeWindowMode(string? mode)
    {
        return string.Equals(mode, TopmostWindowMode, StringComparison.OrdinalIgnoreCase)
            ? TopmostWindowMode
            : DesktopWindowMode;
    }

    private void ApplyTheme(string themeName)
    {
        var palette = ThemePalette.For(themeName);
        Resources["WindowBrush"] = new SolidColorBrush(palette.Window);
        Resources["PanelBrush"] = new SolidColorBrush(palette.Panel);
        Resources["TextBrush"] = new SolidColorBrush(palette.Text);
        Resources["SubtleTextBrush"] = new SolidColorBrush(palette.SubtleText);
        Resources["BorderBrush"] = new SolidColorBrush(palette.Border);
        Resources["HoverBrush"] = new SolidColorBrush(palette.Hover);
        ShellShadow.Color = palette.Shadow;
        ShellShadow.Opacity = palette.ShadowOpacity;
    }

    private void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null)
        {
            return;
        }

        const string valueName = "ProjectShortcutDock";
        if (enabled)
        {
            key.SetValue(valueName, $"\"{Environment.ProcessPath}\"");
        }
        else
        {
            key.DeleteValue(valueName, false);
        }
    }

    private ShortcutItem? ItemFromSender(object sender)
    {
        if (sender is FrameworkElement element && element.DataContext is ShortcutItem item)
        {
            ShortcutList.SelectedItem = item;
            return item;
        }
        if (sender is System.Windows.Controls.MenuItem menuItem &&
            menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu &&
            contextMenu.PlacementTarget is FrameworkElement placementTarget &&
            placementTarget.DataContext is ShortcutItem contextItem)
        {
            ShortcutList.SelectedItem = contextItem;
            return contextItem;
        }
        return ShortcutList.SelectedItem as ShortcutItem;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        if (Topmost)
        {
            Activate();
        }
        else
        {
            SendBehindOtherWindows();
        }
    }

    private void ExitApplication()
    {
        _allowClose = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        SaveSettings();
        Close();
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddFolders(paths);
        }
    }

    private void ShortcutList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            RemoveSelectedShortcut();
        }
    }

    private void ShortcutList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelectedShortcut();
    }

    private void ShortcutList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item is not null)
        {
            item.IsSelected = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return;
        }

        DragMove();
        SaveSettings();
    }

    private void ToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_MouseEnter(object sender, MouseEventArgs e) => Opacity = 1;

    private void Window_MouseLeave(object sender, MouseEventArgs e) => Opacity = 0.68;

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        ApplyWindowMode();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SendBehindOtherWindows();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Width = Width;
        _settings.Height = Height;
        _settings.Save();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void WindowModeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var mode = WindowModeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(mode))
        {
            _settings.WindowMode = NormalizeWindowMode(mode);
            ApplyWindowMode();
            SaveSettings();
        }
    }

    private void ThemeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var themeName = ThemeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(themeName))
        {
            _settings.Theme = themeName;
            ApplyTheme(themeName);
            SaveSettings();
        }
    }

    private void StartWithWindowsBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var enabled = StartWithWindowsBox.IsChecked == true;
        _settings.StartWithWindows = enabled;
        SetStartWithWindows(enabled);
        SaveSettings();
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenFolder(item.Path);
        }
    }

    private void OpenParentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            var parent = Directory.GetParent(item.Path)?.FullName;
            if (parent is not null)
            {
                OpenFolder(parent);
            }
        }
    }

    private void CopyPathMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            Clipboard.SetText(item.Path);
        }
    }

    private void ChangeIconMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is not ShortcutItem item)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Choose icon",
            Filter = "Icon or image files|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            item.IconPath = dialog.FileName;
            item.RefreshIcon();
            SaveSettings();
        }
    }

    private void ResetIconMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            item.IconPath = null;
            item.RefreshIcon();
            SaveSettings();
        }
    }

    private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            Shortcuts.Remove(item);
            SaveSettings();
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
