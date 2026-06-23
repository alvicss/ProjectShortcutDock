using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    private const string DefaultLanguage = "zh-TW";
    private static readonly IntPtr HwndBottom = new(1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly LanguageOption[] LanguageOptions =
    {
        new("zh-TW", "繁體中文"),
        new("en", "English"),
        new("ja", "日本語")
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
    {
        ["zh-TW"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "專案捷徑",
            ["Subtitle"] = "將專案資料夾拖放到這裡",
            ["Settings"] = "設定",
            ["HideToTray"] = "隱藏到系統匣",
            ["Style"] = "樣式",
            ["WindowMode"] = "視窗模式",
            ["Language"] = "語系",
            ["StartWithWindows"] = "隨 Windows 啟動",
            ["Show"] = "顯示",
            ["Hide"] = "隱藏",
            ["Exit"] = "結束",
            ["Open"] = "開啟",
            ["OpenParent"] = "開啟上層資料夾",
            ["CopyPath"] = "複製路徑",
            ["OpenTerminal"] = "在終端機中開啟",
            ["OpenCodex"] = "用 Codex 開啟",
            ["OpenClaude"] = "用 Claude 開啟",
            ["OpenAgy"] = "用 Antigravity 開啟",
            ["ChangeIcon"] = "變更圖示...",
            ["ResetIcon"] = "重設圖示",
            ["RemoveShortcut"] = "移除捷徑",
            ["FolderMissing"] = "這個資料夾已不存在。",
            ["ChooseIcon"] = "選擇圖示",
            ["IconFilter"] = "圖示或圖片檔案|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|所有檔案|*.*"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Project Shortcuts",
            ["Subtitle"] = "Drop project folders here",
            ["Settings"] = "Settings",
            ["HideToTray"] = "Hide to tray",
            ["Style"] = "Style",
            ["WindowMode"] = "Window mode",
            ["Language"] = "Language",
            ["StartWithWindows"] = "Start with Windows",
            ["Show"] = "Show",
            ["Hide"] = "Hide",
            ["Exit"] = "Exit",
            ["Open"] = "Open",
            ["OpenParent"] = "Open parent folder",
            ["CopyPath"] = "Copy path",
            ["OpenTerminal"] = "Open in Terminal",
            ["OpenCodex"] = "Open with Codex",
            ["OpenClaude"] = "Open with Claude",
            ["OpenAgy"] = "Open with Antigravity",
            ["ChangeIcon"] = "Change icon...",
            ["ResetIcon"] = "Reset icon",
            ["RemoveShortcut"] = "Remove shortcut",
            ["FolderMissing"] = "This folder no longer exists.",
            ["ChooseIcon"] = "Choose icon",
            ["IconFilter"] = "Icon or image files|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|All files|*.*"
        },
        ["ja"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "プロジェクトショートカット",
            ["Subtitle"] = "プロジェクトフォルダーをここにドロップ",
            ["Settings"] = "設定",
            ["HideToTray"] = "トレイに隠す",
            ["Style"] = "スタイル",
            ["WindowMode"] = "ウィンドウモード",
            ["Language"] = "言語",
            ["StartWithWindows"] = "Windows 起動時に開始",
            ["Show"] = "表示",
            ["Hide"] = "非表示",
            ["Exit"] = "終了",
            ["Open"] = "開く",
            ["OpenParent"] = "親フォルダーを開く",
            ["CopyPath"] = "パスをコピー",
            ["OpenTerminal"] = "ターミナルで開く",
            ["OpenCodex"] = "Codex で開く",
            ["OpenClaude"] = "Claude で開く",
            ["OpenAgy"] = "Antigravity で開く",
            ["ChangeIcon"] = "アイコンを変更...",
            ["ResetIcon"] = "アイコンをリセット",
            ["RemoveShortcut"] = "ショートカットを削除",
            ["FolderMissing"] = "このフォルダーは存在しません。",
            ["ChooseIcon"] = "アイコンを選択",
            ["IconFilter"] = "アイコンまたは画像ファイル|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|すべてのファイル|*.*"
        }
    };

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
        _settings.Language = NormalizeLanguage(_settings.Language);
        DataContext = this;
        ShortcutList.ContextMenu = CreateShortcutContextMenu();

        ConfigureSettingsControls();
        ApplyLanguage();
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
        LanguageBox.ItemsSource = LanguageOptions;
        LanguageBox.DisplayMemberPath = nameof(LanguageOption.DisplayName);
        LanguageBox.SelectedValuePath = nameof(LanguageOption.Code);
        LanguageBox.SelectedValue = _settings.Language;
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
        var icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = T("AppTitle"),
            Visible = true,
            ContextMenuStrip = CreateTrayContextMenu()
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private ContextMenu CreateShortcutContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem(T("Open"), OpenMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenParent"), OpenParentMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("CopyPath"), CopyPathMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("OpenTerminal"), OpenTerminalMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenCodex"), OpenCodexMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenClaude"), OpenClaudeMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenAgy"), OpenAgyMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("ChangeIcon"), ChangeIconMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("ResetIcon"), ResetIconMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("RemoveShortcut"), RemoveMenuItem_Click));
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

    private void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            MessageBox.Show(T("FolderMissing"), T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
        _settings.Language = NormalizeLanguage(LanguageBox.SelectedValue?.ToString() ?? _settings.Language);
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

    private static string NormalizeLanguage(string? language)
    {
        return LanguageOptions.Any(x => string.Equals(x.Code, language, StringComparison.OrdinalIgnoreCase))
            ? language!
            : DefaultLanguage;
    }

    private string T(string key)
    {
        var language = NormalizeLanguage(_settings.Language);
        return Texts[language].TryGetValue(key, out var text) ? text : Texts[DefaultLanguage][key];
    }

    private void ApplyLanguage()
    {
        Title = T("AppTitle");
        TitleText.Text = T("AppTitle");
        SubtitleText.Text = T("Subtitle");
        SettingsButton.ToolTip = T("Settings");
        HideButton.ToolTip = T("HideToTray");
        StyleLabel.Text = T("Style");
        WindowModeLabel.Text = T("WindowMode");
        LanguageLabel.Text = T("Language");
        StartWithWindowsBox.Content = T("StartWithWindows");
        ShortcutList.ContextMenu = CreateShortcutContextMenu();

        if (_trayIcon is not null)
        {
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.ContextMenuStrip = CreateTrayContextMenu();
            _trayIcon.Text = T("AppTitle");
        }
    }

    private Forms.ContextMenuStrip CreateTrayContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(T("Show"), null, (_, _) => ShowFromTray());
        menu.Items.Add(T("Hide"), null, (_, _) => Hide());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(T("Exit"), null, (_, _) => ExitApplication());
        return menu;
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

    private void LanguageBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Language = NormalizeLanguage(LanguageBox.SelectedValue?.ToString());
        ApplyLanguage();
        SaveSettings();
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

    private void OpenTerminalMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenTerminal(item.Path);
        }
    }

    private void OpenCodexMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenTerminal(item.Path, "codex");
        }
    }

    private void OpenClaudeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenTerminal(item.Path, "CLAUDE");
        }
    }

    private void OpenAgyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenTerminal(item.Path, "agy");
        }
    }

    private void OpenTerminal(string path, string? command = null)
    {
        if (!Directory.Exists(path))
        {
            MessageBox.Show(T("FolderMissing"), T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var wtArguments = string.IsNullOrWhiteSpace(command)
                ? $"-d \"{path}\""
                : $"-d \"{path}\" cmd /k {command}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = wtArguments,
                UseShellExecute = false
            });
        }
        catch
        {
            var cmdArguments = string.IsNullOrWhiteSpace(command)
                ? $"/k cd /d \"{path}\""
                : $"/k cd /d \"{path}\" && {command}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArguments,
                UseShellExecute = false
            });
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
            Title = T("ChooseIcon"),
            Filter = T("IconFilter"),
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

    private sealed record LanguageOption(string Code, string DisplayName);
}
