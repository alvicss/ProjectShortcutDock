using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectShortcutDock;

public sealed class ShortcutGroup : INotifyPropertyChanged
{
    private string _name = "";
    private byte _colorR = CardColorPalette.DefaultSkyColor.R;
    private byte _colorG = CardColorPalette.DefaultSkyColor.G;
    private byte _colorB = CardColorPalette.DefaultSkyColor.B;
    private ObservableCollection<ShortcutItem> _shortcuts = new();

    public ShortcutGroup()
    {
        _shortcuts.CollectionChanged += Shortcuts_CollectionChanged;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Width { get; set; } = 360;

    public double Height { get; set; } = 260;

    public byte ColorR
    {
        get => _colorR;
        set
        {
            if (_colorR == value)
            {
                return;
            }

            _colorR = value;
            OnPropertyChanged();
        }
    }

    public byte ColorG
    {
        get => _colorG;
        set
        {
            if (_colorG == value)
            {
                return;
            }

            _colorG = value;
            OnPropertyChanged();
        }
    }

    public byte ColorB
    {
        get => _colorB;
        set
        {
            if (_colorB == value)
            {
                return;
            }

            _colorB = value;
            OnPropertyChanged();
        }
    }

    public string ColorKey { get; set; } = "";

    public ObservableCollection<ShortcutItem> Shortcuts
    {
        get => _shortcuts;
        set
        {
            if (ReferenceEquals(_shortcuts, value))
            {
                return;
            }

            _shortcuts.CollectionChanged -= Shortcuts_CollectionChanged;
            _shortcuts = value ?? new ObservableCollection<ShortcutItem>();
            _shortcuts.CollectionChanged += Shortcuts_CollectionChanged;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [JsonIgnore]
    public bool IsEmpty => Shortcuts.Count == 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Color GetAccentColor() => Color.FromRgb(ColorR, ColorG, ColorB);

    public void SetAccentColor(Color color)
    {
        ColorR = color.R;
        ColorG = color.G;
        ColorB = color.B;
        ColorKey = "";
    }

    public void MigrateLegacyColorIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(ColorKey))
        {
            return;
        }

        SetAccentColor(CardColorPalette.ResolveLegacyColor(ColorKey));
        ColorKey = "";
    }

    private void Shortcuts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
