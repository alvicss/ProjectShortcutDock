using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectShortcutDock;

public sealed class ShortcutGroup : INotifyPropertyChanged
{
    private string _name = "";
    private string _colorKey = CardColorPalette.DefaultKey;
    private bool _isSelected;
    private Brush _cardBorderBrush = CreateBrush(CardColorPalette.Resolve(CardColorPalette.DefaultKey).AccentColor);
    private Brush _cardBackgroundBrush = CreateBrush(CardColorPalette.Resolve(CardColorPalette.DefaultKey).BackgroundColor);
    private Brush _cardAccentBrush = CreateBrush(CardColorPalette.Resolve(CardColorPalette.DefaultKey).AccentColor);

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

    public string ColorKey
    {
        get => _colorKey;
        set
        {
            var option = CardColorPalette.Resolve(value);
            if (_colorKey == option.Key)
            {
                RefreshAppearance();
                return;
            }

            _colorKey = option.Key;
            ApplyColor(option);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ShortcutItem> Shortcuts { get; set; } = new();

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Brush CardBorderBrush
    {
        get => _cardBorderBrush;
        private set
        {
            _cardBorderBrush = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Brush CardBackgroundBrush
    {
        get => _cardBackgroundBrush;
        private set
        {
            _cardBackgroundBrush = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Brush CardAccentBrush
    {
        get => _cardAccentBrush;
        private set
        {
            _cardAccentBrush = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshAppearance()
    {
        ApplyColor(CardColorPalette.Resolve(ColorKey));
    }

    private void ApplyColor(CardColorOption option)
    {
        CardBorderBrush = CreateBrush(option.AccentColor);
        CardBackgroundBrush = CreateBrush(option.BackgroundColor);
        CardAccentBrush = CreateBrush(option.AccentColor);
    }

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
