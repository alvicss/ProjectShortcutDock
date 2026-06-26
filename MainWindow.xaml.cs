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
using Forms = System.Windows.Forms;

namespace ProjectShortcutDock;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string DesktopWindowMode = "Desktop";
    private const string TopmostWindowMode = "Topmost";
    private const string DefaultLanguage = "en";
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

    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
    {
        ["zh-TW"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "專案捷徑",
            ["Subtitle"] = "將檔案或資料夾拖放到這裡",
            ["CardsHint"] = "先選取卡片再拖曳，或直接拖到指定卡片做分群",
            ["AddCard"] = "新增卡片",
            ["DeleteCard"] = "刪除卡片",
            ["DeleteCardConfirm"] = "要刪除這張卡片與其中記憶的捷徑嗎？這不會刪除原本的檔案或資料夾。",
            ["CardColor"] = "卡片顏色",
            ["CardDefaultName"] = "卡片",
            ["CardColorSlate"] = "石墨灰",
            ["CardColorSky"] = "天空藍",
            ["CardColorEmerald"] = "翠綠",
            ["CardColorAmber"] = "琥珀橙",
            ["CardColorRose"] = "玫瑰紅",
            ["CardColorViolet"] = "紫羅蘭",
            ["Settings"] = "設定",
            ["About"] = "關於",
            ["HideToTray"] = "隱藏到系統匣",
            ["Style"] = "樣式",
            ["WindowMode"] = "視窗模式",
            ["Language"] = "語系",
            ["TerminalShell"] = "Shell",
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
            ["FolderMissing"] = "這個項目已不存在。",
            ["ChooseIcon"] = "選擇圖示",
            ["IconFilter"] = "圖示或圖片檔案|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|所有檔案|*.*"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Project Shortcuts",
            ["Subtitle"] = "Drop files or folders here",
            ["CardsHint"] = "Select a card first, or drop directly onto a card to group shortcuts",
            ["AddCard"] = "Add Card",
            ["DeleteCard"] = "Delete card",
            ["DeleteCardConfirm"] = "Delete this card and its remembered shortcuts? This will not delete the original files or folders.",
            ["CardColor"] = "Card color",
            ["CardDefaultName"] = "Card",
            ["CardColorSlate"] = "Slate",
            ["CardColorSky"] = "Sky",
            ["CardColorEmerald"] = "Emerald",
            ["CardColorAmber"] = "Amber",
            ["CardColorRose"] = "Rose",
            ["CardColorViolet"] = "Violet",
            ["Settings"] = "Settings",
            ["About"] = "About",
            ["HideToTray"] = "Hide to tray",
            ["Style"] = "Style",
            ["WindowMode"] = "Window mode",
            ["Language"] = "Language",
            ["TerminalShell"] = "Shell",
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
            ["FolderMissing"] = "This item no longer exists.",
            ["ChooseIcon"] = "Choose icon",
            ["IconFilter"] = "Icon or image files|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|All files|*.*"
        },
        ["ja"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "プロジェクトショートカット",
            ["Subtitle"] = "ファイルまたはフォルダーをここにドロップ",
            ["CardsHint"] = "先にカードを選ぶか、目的のカードへ直接ドロップしてグループ化します",
            ["AddCard"] = "カード追加",
            ["DeleteCard"] = "カードを削除",
            ["DeleteCardConfirm"] = "このカードと記憶されたショートカットを削除しますか？元のファイルやフォルダーは削除されません。",
            ["CardColor"] = "カード色",
            ["CardDefaultName"] = "カード",
            ["CardColorSlate"] = "スレート",
            ["CardColorSky"] = "スカイ",
            ["CardColorEmerald"] = "エメラルド",
            ["CardColorAmber"] = "アンバー",
            ["CardColorRose"] = "ローズ",
            ["CardColorViolet"] = "バイオレット",
            ["Settings"] = "設定",
            ["About"] = "About",
            ["HideToTray"] = "トレイに隠す",
            ["Style"] = "スタイル",
            ["WindowMode"] = "ウィンドウモード",
            ["Language"] = "言語",
            ["TerminalShell"] = "Shell",
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
            ["FolderMissing"] = "この項目は存在しません。",
            ["ChooseIcon"] = "アイコンを選択",
            ["IconFilter"] = "アイコンまたは画像ファイル|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe;*.dll|すべてのファイル|*.*"
        }
    };

    private readonly AppSettings _settings;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly List<ShellOption> _terminalShellOptions = new();
    private bool _allowClose;
    private bool _isLoading = true;
    private IntPtr _windowHandle;
    private ShortcutGroup? _selectedGroup;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShortcutGroup> Groups { get; } = new();

    public IReadOnlyList<CardColorOption> GroupColorOptions { get; private set; } = Array.Empty<CardColorOption>();

    public string CardColorLabelText => T("CardColor");

    public string DeleteCardToolTipText => T("DeleteCard");

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _settings.Language = NormalizeLanguage(_settings.Language);
        _terminalShellOptions = DetectTerminalShellOptions();
        _settings.TerminalShell = NormalizeTerminalShell(_settings.TerminalShell);
        DataContext = this;

        ConfigureSettingsControls();
        ApplyLanguage();
        LoadGroups();
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
        TerminalShellBox.ItemsSource = _terminalShellOptions;
        TerminalShellBox.DisplayMemberPath = nameof(ShellOption.DisplayName);
        TerminalShellBox.SelectedValuePath = nameof(ShellOption.Code);
        TerminalShellBox.SelectedValue = _settings.TerminalShell;
        StartWithWindowsBox.IsChecked = _settings.StartWithWindows;
    }

    private void LoadGroups()
    {
        var storedGroups = _settings.Groups.Count > 0
            ? _settings.Groups
            : CreateLegacyGroups();

        if (storedGroups.Count == 0)
        {
            storedGroups.Add(CreateNewGroup());
        }

        Groups.Clear();

        for (var index = 0; index < storedGroups.Count; index++)
        {
            var group = storedGroups[index];
            PrepareGroup(group, index + 1);
            Groups.Add(group);
        }

        SetSelectedGroup(Groups.FirstOrDefault());
    }

    private List<ShortcutGroup> CreateLegacyGroups()
    {
        if (_settings.Shortcuts.Count == 0)
        {
            return new List<ShortcutGroup>();
        }

        return new List<ShortcutGroup>
        {
            new()
            {
                Name = CreateDefaultGroupName(1),
                ColorKey = CardColorPalette.DefaultKey,
                Shortcuts = new ObservableCollection<ShortcutItem>(_settings.Shortcuts)
            }
        };
    }

    private void PrepareGroup(ShortcutGroup group, int fallbackIndex)
    {
        group.Name = string.IsNullOrWhiteSpace(group.Name) ? CreateDefaultGroupName(fallbackIndex) : group.Name;
        group.ColorKey = group.ColorKey;
        group.Shortcuts ??= new ObservableCollection<ShortcutItem>();
        group.Shortcuts = new ObservableCollection<ShortcutItem>(
            group.Shortcuts.Where(x => ShortcutPathExists(x.Path)));

        foreach (var item in group.Shortcuts)
        {
            item.RefreshIcon();
        }

        group.IsSelected = false;
        group.RefreshAppearance();
    }

    private ShortcutGroup CreateNewGroup()
    {
        var group = new ShortcutGroup
        {
            Name = CreateDefaultGroupName(Groups.Count + 1),
            ColorKey = CardColorPalette.DefaultKey
        };
        group.RefreshAppearance();
        return group;
    }

    private string CreateDefaultGroupName(int index)
    {
        return $"{T("CardDefaultName")} {index}";
    }

    private void AddGroup()
    {
        var group = CreateNewGroup();
        Groups.Add(group);
        SetSelectedGroup(group);
        SaveSettings();
    }

    private void RemoveGroup(ShortcutGroup group)
    {
        var result = MessageBox.Show(
            T("DeleteCardConfirm"),
            T("DeleteCard"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var removedIndex = Groups.IndexOf(group);
        Groups.Remove(group);

        if (Groups.Count == 0)
        {
            var newGroup = CreateNewGroup();
            Groups.Add(newGroup);
            SetSelectedGroup(newGroup);
        }
        else
        {
            var nextIndex = Math.Clamp(removedIndex, 0, Groups.Count - 1);
            SetSelectedGroup(Groups[nextIndex]);
        }

        SaveSettings();
    }

    private void SetSelectedGroup(ShortcutGroup? group)
    {
        if (ReferenceEquals(_selectedGroup, group))
        {
            return;
        }

        if (_selectedGroup is not null)
        {
            _selectedGroup.IsSelected = false;
        }

        _selectedGroup = group;

        if (_selectedGroup is not null)
        {
            _selectedGroup.IsSelected = true;
        }
    }

    private ShortcutGroup GetOrCreateDropTargetGroup()
    {
        if (_selectedGroup is not null)
        {
            return _selectedGroup;
        }

        if (Groups.Count > 0)
        {
            SetSelectedGroup(Groups[0]);
            return Groups[0];
        }

        var group = CreateNewGroup();
        Groups.Add(group);
        SetSelectedGroup(group);
        return group;
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var icon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = T("AppTitle"),
            Visible = true,
            ContextMenuStrip = CreateTrayContextMenu()
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "image", "project-shortcut-dock.ico");
        return File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : System.Drawing.SystemIcons.Application;
    }

    private ContextMenu CreateShortcutContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem(T("Open"), "\uE8E5", OpenMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenParent"), "\uE838", OpenParentMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("CopyPath"), "\uE8C8", CopyPathMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("OpenTerminal"), "\uE756", OpenTerminalMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenCodex"), LoadMenuIcon("codex.ico"), OpenCodexMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenClaude"), LoadMenuIcon("claude.ico"), OpenClaudeMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("OpenAgy"), LoadMenuIcon("agy.ico"), OpenAgyMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("ChangeIcon"), "\uE8B9", ChangeIconMenuItem_Click));
        menu.Items.Add(CreateMenuItem(T("ResetIcon"), "\uE72C", ResetIconMenuItem_Click));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(T("RemoveShortcut"), "\uE74D", RemoveMenuItem_Click));
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

    private void AddShortcutsToGroup(ShortcutGroup group, string[] paths)
    {
        var added = false;
        foreach (var path in paths.Where(ShortcutPathExists))
        {
            var normalizedPath = NormalizeShortcutPath(path);
            if (group.Shortcuts.Any(x => string.Equals(x.Path, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var item = new ShortcutItem
            {
                Path = normalizedPath,
                Name = GetShortcutName(normalizedPath)
            };
            item.RefreshIcon();
            group.Shortcuts.Add(item);
            added = true;
        }

        if (added)
        {
            SaveSettings();
        }
    }

    private void RemoveShortcut(ShortcutItem item)
    {
        var group = FindGroupForShortcut(item);
        if (group is null)
        {
            return;
        }

        group.Shortcuts.Remove(item);
        SaveSettings();
    }

    private ShortcutGroup? FindGroupForShortcut(ShortcutItem item)
    {
        return Groups.FirstOrDefault(group => group.Shortcuts.Contains(item));
    }

    private void OpenPath(string path)
    {
        if (!ShortcutPathExists(path))
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

    private static bool ShortcutPathExists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }

    private static string NormalizeShortcutPath(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var rootPath = System.IO.Path.GetPathRoot(fullPath);
        return Directory.Exists(fullPath) &&
               System.IO.Path.EndsInDirectorySeparator(fullPath) &&
               !string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
            : fullPath;
    }

    private static string GetShortcutName(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static string? GetWorkingDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            return path;
        }

        return File.Exists(path) ? System.IO.Path.GetDirectoryName(path) : null;
    }

    private void SaveSettings(bool normalizeGroupNames = false)
    {
        if (_isLoading)
        {
            return;
        }

        if (normalizeGroupNames)
        {
            NormalizeBlankGroupNames();
        }

        _settings.Groups = Groups.ToList();
        _settings.Shortcuts = new List<ShortcutItem>();
        _settings.Theme = ThemeBox.SelectedItem?.ToString() ?? _settings.Theme;
        _settings.WindowMode = WindowModeBox.SelectedItem?.ToString() ?? _settings.WindowMode;
        _settings.Language = NormalizeLanguage(LanguageBox.SelectedValue?.ToString() ?? _settings.Language);
        _settings.TerminalShell = NormalizeTerminalShell(TerminalShellBox.SelectedValue?.ToString() ?? _settings.TerminalShell);
        _settings.StartWithWindows = StartWithWindowsBox.IsChecked == true;
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Width = Width;
        _settings.Height = Height;
        _settings.Save();
    }

    private void NormalizeBlankGroupNames()
    {
        for (var index = 0; index < Groups.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(Groups[index].Name))
            {
                Groups[index].Name = CreateDefaultGroupName(index + 1);
            }
        }
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

    private string NormalizeTerminalShell(string? shell)
    {
        return _terminalShellOptions.Any(x => string.Equals(x.Code, shell, StringComparison.OrdinalIgnoreCase))
            ? shell!
            : DefaultTerminalShell;
    }

    private string T(string key)
    {
        var language = NormalizeLanguage(_settings.Language);
        return Texts[language].TryGetValue(key, out var text) ? text : Texts[DefaultLanguage][key];
    }

    internal string GetText(string key) => T(key);

    private void ApplyLanguage()
    {
        Title = T("AppTitle");
        TitleText.Text = T("AppTitle");
        SubtitleText.Text = T("Subtitle");
        CardsHintText.Text = T("CardsHint");
        AddCardButton.Content = T("AddCard");
        SettingsButton.ToolTip = T("Settings");
        AboutButton.ToolTip = T("About");
        HideButton.ToolTip = T("HideToTray");
        StyleLabel.Text = T("Style");
        WindowModeLabel.Text = T("WindowMode");
        LanguageLabel.Text = T("Language");
        TerminalShellLabel.Text = T("TerminalShell");
        StartWithWindowsBox.Content = T("StartWithWindows");
        GroupColorOptions = CardColorPalette.Build(T);
        NotifyPropertyChanged(nameof(GroupColorOptions));
        NotifyPropertyChanged(nameof(CardColorLabelText));
        NotifyPropertyChanged(nameof(DeleteCardToolTipText));

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
            return item;
        }

        if (sender is System.Windows.Controls.MenuItem menuItem &&
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

        return null;
    }

    private ShortcutGroup? GroupFromSender(object sender)
    {
        return sender is FrameworkElement element
            ? element.DataContext as ShortcutGroup
            : null;
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
        SaveSettings(true);
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
            AddShortcutsToGroup(GetOrCreateDropTargetGroup(), paths);
            e.Handled = true;
        }
    }

    private void GroupCard_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void GroupCard_Drop(object sender, DragEventArgs e)
    {
        if (GroupFromSender(sender) is not ShortcutGroup group)
        {
            return;
        }

        SetSelectedGroup(group);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddShortcutsToGroup(group, paths);
        }

        e.Handled = true;
    }

    private void GroupCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (GroupFromSender(sender) is ShortcutGroup group)
        {
            SetSelectedGroup(group);
        }
    }

    private void AddCardButton_Click(object sender, RoutedEventArgs e)
    {
        AddGroup();
    }

    private void RemoveGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupFromSender(sender) is ShortcutGroup group)
        {
            RemoveGroup(group);
        }
    }

    private void GroupName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (GroupFromSender(sender) is ShortcutGroup group)
        {
            SetSelectedGroup(group);
            SaveSettings();
        }
    }

    private void GroupName_LostFocus(object sender, RoutedEventArgs e)
    {
        if (GroupFromSender(sender) is not ShortcutGroup group)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(group.Name))
        {
            group.Name = CreateDefaultGroupName(Math.Max(1, Groups.IndexOf(group) + 1));
        }

        SaveSettings();
    }

    private void GroupColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        if (GroupFromSender(sender) is ShortcutGroup group)
        {
            SetSelectedGroup(group);
            SaveSettings();
        }
    }

    private void ShortcutList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not ShortcutItem)
        {
            e.Handled = true;
            return;
        }

        listBox.ContextMenu = CreateShortcutContextMenu();
    }

    private void ShortcutList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete &&
            sender is ListBox listBox &&
            listBox.SelectedItem is ShortcutItem item)
        {
            RemoveShortcut(item);
            e.Handled = true;
        }
    }

    private void ShortcutList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox &&
            listBox.SelectedItem is ShortcutItem item)
        {
            OpenPath(item.Path);
        }
    }

    private void ShortcutList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var listItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (listItem is null)
        {
            if (sender is ListBox listBox)
            {
                listBox.SelectedItem = null;
            }

            return;
        }

        listItem.IsSelected = true;
        if (listItem.DataContext is ShortcutItem shortcut &&
            FindGroupForShortcut(shortcut) is ShortcutGroup group)
        {
            SetSelectedGroup(group);
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

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow(this);
        aboutWindow.ShowDialog();
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
        SaveSettings(true);
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void WindowModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var mode = WindowModeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(mode))
        {
            _settings.WindowMode = NormalizeWindowMode(mode);
            ApplyWindowMode();
            SaveSettings();
        }
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var themeName = ThemeBox.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(themeName))
        {
            _settings.Theme = themeName;
            ApplyTheme(themeName);
            SaveSettings();
        }
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Language = NormalizeLanguage(LanguageBox.SelectedValue?.ToString());
        ApplyLanguage();
        SaveSettings();
    }

    private void TerminalShellBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.TerminalShell = NormalizeTerminalShell(TerminalShellBox.SelectedValue?.ToString());
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

    private void OpenTerminal(string path, string? command = null)
    {
        if (!ShortcutPathExists(path))
        {
            MessageBox.Show(T("FolderMissing"), T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var workingDirectory = GetWorkingDirectory(path);
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            MessageBox.Show(T("FolderMissing"), T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var shell = _terminalShellOptions.FirstOrDefault(x => x.Code == NormalizeTerminalShell(_settings.TerminalShell))
            ?? _terminalShellOptions.First(x => x.Code == DefaultTerminalShell);
        var startInfo = CreateTerminalStartInfo(shell, workingDirectory, command);
        Process.Start(startInfo);
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
            RemoveShortcut(item);
        }
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

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    private sealed record ShellOption(string Code, string DisplayName, string ExecutablePath);
}
