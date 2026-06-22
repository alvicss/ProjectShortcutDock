using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectShortcutDock;

public sealed class ShortcutItem : INotifyPropertyChanged
{
    private ImageSource? _iconSource;

    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string? IconPath { get; set; }

    [JsonIgnore]
    public ImageSource? IconSource
    {
        get => _iconSource;
        private set
        {
            _iconSource = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshIcon()
    {
        IconSource = IconHelper.LoadIcon(Path, IconPath);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
