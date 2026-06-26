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
using System.Windows.Media.Imaging;

namespace ProjectShortcutDock;

public partial class MainWindow : Window
{
    private const string DesktopWindowMode = "Desktop";
    private const string TopmostWindowMode = "Topmost";
    private const string DefaultTerminalShell = "cmd";
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

    private readonly DockManager _manager;
    private readonly AppSettings _settings;
    private readonly ShortcutGroup _group;
    private readonly List<ShellOption> _terminalShellOptions = new();
    private bool _allowClose;
    private bool _isLoading = true;
    private IntPtr _windowHandle;

    public MainWindow(DockManager manager, ShortcutGroup group)
    {
        InitializeComponent();

        _manager = manager;
        _settings = manager.Settings;
        _group = group;
        _terminalShellOptions = DetectTerminalShellOptions();
        _settings.TerminalShell = NormalizeTerminalShell(_settings.TerminalShell);

        DataContext = this;

        foreach (var item in _group.Shortcuts)
        {
            item.RefreshIcon();
        }

        _group.PropertyChanged += Group_PropertyChanged;

        ConfigureSettingsControls();
        ApplyLanguage();
        ApplySavedWindowPosition();
        ApplyTheme(_settings.Theme);
        ApplyWindowMode();
        ApplyGroupAccent();
        UpdateTitle();
        UpdateSubtitleVisibility();
        SetTitleEditing(false);
        Opacity = 0.68;

        _isLoading = false;
    }

    public ShortcutGroup Group => _group;

    public ObservableCollection<ShortcutItem> Shortcuts => _group.Shortcuts;

    internal string GetText(string key) => UiText.Get(_settings.Language, key);

    internal string CurrentLanguage => _settings.Language;

    public void PrepareForClose()
    {
        _allowClose = true;
    }

    public void ShowFromManager()
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

    public void FocusTitle()
    {
        SetTitleEditing(true);
        GroupNameBox.Focus();
        GroupNameBox.SelectAll();
    }

    public void RefreshFromSharedSettings()
    {
        _isLoading = true;
        ConfigureSettingsControls();
        ApplyLanguage();
        ApplyTheme(_settings.Theme);
        ApplyWindowMode();
        ApplyGroupAccent();
        UpdateTitle();
        UpdateSubtitleVisibility();
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
        LanguageBox.SelectedValue = UiText.NormalizeLanguage(_settings.Language);
        TerminalShellBox.ItemsSource = _terminalShellOptions;
        TerminalShellBox.DisplayMemberPath = nameof(ShellOption.DisplayName);
        TerminalShellBox.SelectedValuePath = nameof(ShellOption.Code);
        TerminalShellBox.SelectedValue = _settings.TerminalShell;
        StartWithWindowsBox.IsChecked = _settings.StartWithWindows;
    }

    private void ApplySavedWindowPosition()
    {
        Width = _group.Width > 0 ? _group.Width : Width;
        Height = _group.Height > 0 ? _group.Height : Height;

        if (_group.Left.HasValue && _group.Top.HasValue)
        {
            Left = _group.Left.Value;
            Top = _group.Top.Value;
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 18;
        Top = workArea.Top + 18;
    }

    private void ApplyLanguage()
    {
        SubtitleText.Text = GetText("EmptyHint");
        AddWindowButton.ToolTip = GetText("AddWindow");
        RemoveWindowButton.ToolTip = GetText("RemoveWindow");
        RenameButton.ToolTip = GetText("RenameWindow");
        ColorButton.ToolTip = GetText("ChangeColor");
        SettingsButton.ToolTip = GetText("Settings");
        AboutButton.ToolTip = GetText("About");
        HideButton.ToolTip = GetText("HideToTray");
        StyleLabel.Text = GetText("Style");
        WindowModeLabel.Text = GetText("WindowMode");
        LanguageLabel.Text = GetText("Language");
        TerminalShellLabel.Text = GetText("TerminalShell");
        StartWithWindowsBox.Content = GetText("StartWithWindows");
    }

    private void ApplyTheme(string themeName)
    {
        var palette = ThemePalette.For(themeName);
        Resources["WindowBrush"] = CreateBrush(palette.Window);
        Resources["PanelBrush"] = CreateBrush(palette.Panel);
        Resources["TextBrush"] = CreateBrush(palette.Text);
        Resources["SubtleTextBrush"] = CreateBrush(palette.SubtleText);
        Resources["BorderBrush"] = CreateBrush(palette.Border);
        Resources["HoverBrush"] = CreateBrush(palette.Hover);
        ShellShadow.Color = palette.Shadow;
        ShellShadow.Opacity = palette.ShadowOpacity;
        ApplyGroupAccent();
    }

    private void ApplyGroupAccent()
    {
        var theme = ThemePalette.For(_settings.Theme);
        var accent = _group.GetAccentColor();
        var accentWeight = CardColorPalette.GetAlphaWeight(accent);
        ShellBorder.BorderBrush = CreateBrush(accent);
        ShellBorder.Background = CreateBrush(CardColorPalette.Blend(theme.Window, CardColorPalette.WithoutAlpha(accent), 0.10 * accentWeight));
        SettingsPanel.BorderBrush = CreateBrush(CardColorPalette.Blend(theme.Border, CardColorPalette.WithoutAlpha(accent), 0.30 * accentWeight));
        SettingsPanel.Background = CreateBrush(CardColorPalette.Blend(theme.Panel, CardColorPalette.WithoutAlpha(accent), 0.06 * accentWeight));
        var visibleAccent = CardColorPalette.WithoutAlpha(accent);
        ColorIndicatorFill.Fill = CreateBrush(accent);
        ColorIndicatorFill.Stroke = CreateBrush(visibleAccent);
        ColorIndicatorOutline.Stroke = CreateBrush(visibleAccent);
        ColorIndicatorOutline.Visibility = accent.A < 30 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyWindowMode()
    {
        _settings.WindowMode = NormalizeWindowMode(_settings.WindowMode);
        Topmost = _settings.WindowMode == TopmostWindowMode;

        if (!Topmost)
        {
            SendBehindOtherWindows();
        }
    }

    private void UpdateTitle()
    {
        Title = string.IsNullOrWhiteSpace(_group.Name)
            ? GetDefaultGroupName()
            : _group.Name;
    }

    private void UpdateSubtitleVisibility()
    {
        SubtitleText.Visibility = _group.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private string GetDefaultGroupName()
    {
        var index = Math.Max(1, _settings.Groups.IndexOf(_group) + 1);
        return $"{GetText("GroupDefaultName")} {index}";
    }

    private void PersistWindowBounds()
    {
        if (_isLoading || WindowState != WindowState.Normal)
        {
            return;
        }

        _group.Left = Left;
        _group.Top = Top;
        _group.Width = Width;
        _group.Height = Height;
        _manager.SaveSettings();
    }

    private void BeginWindowDrag(MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
        PersistWindowBounds();
        e.Handled = true;
    }

    private void SetTitleEditing(bool isEditing)
    {
        GroupNameBox.IsReadOnly = !isEditing;
        GroupNameBox.Cursor = isEditing ? Cursors.IBeam : Cursors.SizeAll;
    }

    private void AddShortcuts(string[] paths)
    {
        var added = false;
        foreach (var path in paths.Where(ShortcutPathExists))
        {
            var normalizedPath = NormalizeShortcutPath(path);
            if (_group.Shortcuts.Any(x => string.Equals(x.Path, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var item = new ShortcutItem
            {
                Path = normalizedPath,
                Name = GetShortcutName(normalizedPath)
            };
            item.RefreshIcon();
            _group.Shortcuts.Add(item);
            added = true;
        }

        if (added)
        {
            UpdateSubtitleVisibility();
            _manager.SaveSettings();
        }
    }

    private void RemoveShortcut(ShortcutItem item)
    {
        _group.Shortcuts.Remove(item);
        UpdateSubtitleVisibility();
        _manager.SaveSettings();
    }

    private void OpenPath(string path)
    {
        if (!ShortcutPathExists(path))
        {
            MessageBox.Show(GetText("ItemMissing"), GetText("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void OpenTerminal(string path, string? command = null)
    {
        if (!ShortcutPathExists(path))
        {
            MessageBox.Show(GetText("ItemMissing"), GetText("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var workingDirectory = GetWorkingDirectory(path);
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            MessageBox.Show(GetText("ItemMissing"), GetText("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var shell = _terminalShellOptions.FirstOrDefault(x => x.Code == NormalizeTerminalShell(_settings.TerminalShell))
            ?? _terminalShellOptions.First(x => x.Code == DefaultTerminalShell);
        Process.Start(CreateTerminalStartInfo(shell, workingDirectory, command));
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

    private static bool ShortcutPathExists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }

    private static string NormalizeShortcutPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var rootPath = Path.GetPathRoot(fullPath);
        return Directory.Exists(fullPath) &&
               Path.EndsInDirectorySeparator(fullPath) &&
               !string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            : fullPath;
    }

    private static string GetShortcutName(string path)
    {
        var name = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static string? GetWorkingDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            return path;
        }

        return File.Exists(path) ? Path.GetDirectoryName(path) : null;
    }

    private static string NormalizeWindowMode(string? mode)
    {
        return string.Equals(mode, TopmostWindowMode, StringComparison.OrdinalIgnoreCase)
            ? TopmostWindowMode
            : DesktopWindowMode;
    }

    private string NormalizeTerminalShell(string? shell)
    {
        return _terminalShellOptions.Any(x => string.Equals(x.Code, shell, StringComparison.OrdinalIgnoreCase))
            ? shell!
            : DefaultTerminalShell;
    }

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static ProcessStartInfo CreateTerminalStartInfo(ShellOption shell, string path, string? command)
    {
        return shell.Code switch
        {
            "pwsh" or "powershell" => new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildPowerShellArguments(path, command),
                UseShellExecute = false
            },
            "git-bash" => new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildGitBashArguments(path, command),
                UseShellExecute = false
            },
            "wsl" => new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildWslArguments(path, command),
                UseShellExecute = false
            },
            _ => new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildCmdArguments(path, command),
                UseShellExecute = false
            }
        };
    }

    private static string BuildCmdArguments(string path, string? command)
    {
        return string.IsNullOrWhiteSpace(command)
            ? $"/k cd /d \"{path}\""
            : $"/k cd /d \"{path}\" && {command}";
    }

    private static string BuildPowerShellArguments(string path, string? command)
    {
        var escapedPath = path.Replace("'", "''");
        return string.IsNullOrWhiteSpace(command)
            ? $"-NoExit -Command \"Set-Location -LiteralPath '{escapedPath}'\""
            : $"-NoExit -Command \"Set-Location -LiteralPath '{escapedPath}'; {command}\"";
    }

    private static string BuildGitBashArguments(string path, string? command)
    {
        var bashPath = path.Replace('\\', '/').Replace("\"", "\\\"");
        var bashCommand = string.IsNullOrWhiteSpace(command)
            ? $"cd \"{bashPath}\"; exec bash"
            : $"cd \"{bashPath}\" && {command}; exec bash";
        return $"--login -i -c \"{bashCommand.Replace("\"", "\\\"")}\"";
    }

    private static string BuildWslArguments(string path, string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return $"--cd \"{path}\"";
        }

        return $"--cd \"{path}\" --exec bash -lc \"{command}; exec bash\"";
    }

    private static List<ShellOption> DetectTerminalShellOptions()
    {
        var options = new List<ShellOption>
        {
            new(DefaultTerminalShell, "cmd.exe", "cmd.exe")
        };

        if (FindExecutable("powershell.exe") is string windowsPowerShell)
        {
            options.Add(new("powershell", "Windows PowerShell", windowsPowerShell));
        }

        if (FindExecutable("pwsh.exe") is string powerShell7)
        {
            options.Add(new("pwsh", "PowerShell 7", powerShell7));
        }

        if (FindGitBash() is string gitBash)
        {
            options.Add(new("git-bash", "Git Bash", gitBash));
        }

        if (FindExecutable("wsl.exe") is string wsl)
        {
            options.Add(new("wsl", "WSL", wsl));
        }

        return options;
    }

    private static string? FindGitBash()
    {
        var candidates = new[]
        {
            @"C:\Program Files\Git\bin\bash.exe",
            @"C:\Program Files (x86)\Git\bin\bash.exe"
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindExecutable(string fileName)
    {
        if (File.Exists(fileName))
        {
            return fileName;
        }

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in paths)
        {
            try
            {
                var candidate = Path.Combine(path.Trim(), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore malformed PATH entries.
            }
        }

        return null;
    }

    private ContextMenu CreateShortcutContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem(GetText("Open"), "\uE8E5", OpenMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("OpenParent"), "\uE838", OpenParentMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("CopyPath"), "\uE8C8", CopyPathMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(GetText("OpenTerminal"), "\uE756", OpenTerminalMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("OpenCodex"), LoadMenuIcon("codex.ico"), OpenCodexMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("OpenClaude"), LoadMenuIcon("claude.ico"), OpenClaudeMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("OpenAgy"), LoadMenuIcon("agy.ico"), OpenAgyMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(GetText("ChangeIcon"), "\uE8B9", ChangeIconMenuItem_Click));
        menu.Items.Add(CreateMenuItem(GetText("ResetIcon"), "\uE72C", ResetIconMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(GetText("RemoveShortcut"), "\uE74D", RemoveMenuItem_Click));
        return menu;
    }

    private static MenuItem CreateMenuItem(string header, object? icon, RoutedEventHandler clickHandler)
    {
        var item = new MenuItem
        {
            Header = header,
            Icon = icon is string glyph ? CreateMenuIcon(glyph) : icon
        };
        item.Click += clickHandler;
        return item;
    }

    private static TextBlock CreateMenuIcon(string glyph)
    {
        return new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 15,
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private static Image LoadMenuIcon(string fileName)
    {
        var image = new Image
        {
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform
        };
        var path = Path.Combine(AppContext.BaseDirectory, "image", fileName);
        if (File.Exists(path))
        {
            image.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
        }

        return image;
    }

    private ShortcutItem? ItemFromSender(object sender)
    {
        if (sender is FrameworkElement element && element.DataContext is ShortcutItem item)
        {
            return item;
        }

        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu contextMenu &&
            contextMenu.PlacementTarget is FrameworkElement placementTarget)
        {
            if (placementTarget.DataContext is ShortcutItem contextItem)
            {
                return contextItem;
            }

            if (placementTarget is ListBox listBox && listBox.SelectedItem is ShortcutItem selectedItem)
            {
                return selectedItem;
            }
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

    private void Group_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShortcutGroup.Name))
        {
            UpdateTitle();
            if (!_isLoading)
            {
                _manager.SaveSettings();
            }
        }

        if (e.PropertyName is nameof(ShortcutGroup.ColorA) or nameof(ShortcutGroup.ColorR) or nameof(ShortcutGroup.ColorG) or nameof(ShortcutGroup.ColorB))
        {
            ApplyGroupAccent();
            if (!_isLoading)
            {
                _manager.SaveSettings();
            }
        }

        if (e.PropertyName == nameof(ShortcutGroup.IsEmpty))
        {
            UpdateSubtitleVisibility();
        }
    }

    private void AddWindowButton_Click(object sender, RoutedEventArgs e)
    {
        _manager.AddWindow(this);
    }

    private void RemoveWindowButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            GetText("RemoveWindowConfirm"),
            GetText("RemoveWindow"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _manager.RemoveWindow(this);
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerWindow(this, _group.GetAccentColor());
        if (dialog.ShowDialog() != false)
        {
            _group.SetAccentColor(dialog.SelectedColor);
        }
    }

    private void GroupNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        UpdateTitle();
        _manager.SaveSettings();
    }

    private void GroupNameBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (GroupNameBox.IsReadOnly)
        {
            GroupNameBox.Focusable = false;
            try
            {
                BeginWindowDrag(e);
            }
            finally
            {
                GroupNameBox.Focusable = true;
            }
        }
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        SetTitleEditing(true);
        GroupNameBox.Focus();
        GroupNameBox.SelectAll();
    }

    private void GroupNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SetTitleEditing(false);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            GroupNameBox.Text = string.IsNullOrWhiteSpace(_group.Name) ? GetDefaultGroupName() : _group.Name;
            SetTitleEditing(false);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void GroupNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_group.Name))
        {
            _group.Name = GetDefaultGroupName();
        }

        SetTitleEditing(false);
    }

    private void ToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow(this);
        aboutWindow.ShowDialog();
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        _manager.HideWindow(this);
    }

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
        PersistWindowBounds();
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        PersistWindowBounds();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        _manager.HideWindow(this);
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
            AddShortcuts(paths);
            e.Handled = true;
        }
    }

    private void ShortcutList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ShortcutList.SelectedItem is not ShortcutItem)
        {
            e.Handled = true;
            return;
        }

        ShortcutList.ContextMenu = CreateShortcutContextMenu();
    }

    private void ShortcutList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && ShortcutList.SelectedItem is ShortcutItem item)
        {
            RemoveShortcut(item);
            e.Handled = true;
        }
    }

    private void ShortcutList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ShortcutList.SelectedItem is ShortcutItem item)
        {
            OpenPath(item.Path);
        }
    }

    private void ShortcutList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item is null)
        {
            ShortcutList.SelectedItem = null;
            return;
        }

        item.IsSelected = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            SetTitleEditing(false);
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return;
        }

        SetTitleEditing(false);
        BeginWindowDrag(e);
    }

    private void WindowModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var mode = WindowModeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(mode))
        {
            _settings.WindowMode = NormalizeWindowMode(mode);
            _manager.RefreshAllWindows();
            _manager.SaveSettings();
        }
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var themeName = ThemeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(themeName))
        {
            _settings.Theme = themeName;
            _manager.RefreshAllWindows();
            _manager.SaveSettings();
        }
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Language = UiText.NormalizeLanguage(LanguageBox.SelectedValue?.ToString());
        _manager.RefreshAllWindows();
        _manager.SaveSettings();
    }

    private void TerminalShellBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.TerminalShell = NormalizeTerminalShell(TerminalShellBox.SelectedValue?.ToString());
        _manager.SaveSettings();
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
        _manager.SaveSettings();
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            OpenPath(item.Path);
        }
    }

    private void OpenParentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            var parent = Directory.GetParent(item.Path)?.FullName;
            if (parent is not null)
            {
                OpenPath(parent);
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

    private void ChangeIconMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is not ShortcutItem item)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = GetText("ChooseIcon"),
            Filter = GetText("IconFilter"),
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            item.IconPath = dialog.FileName;
            item.RefreshIcon();
            _manager.SaveSettings();
        }
    }

    private void ResetIconMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            item.IconPath = null;
            item.RefreshIcon();
            _manager.SaveSettings();
        }
    }

    private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromSender(sender) is ShortcutItem item)
        {
            RemoveShortcut(item);
        }
    }

    private sealed record LanguageOption(string Code, string DisplayName);

    private sealed record ShellOption(string Code, string DisplayName, string ExecutablePath);

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
