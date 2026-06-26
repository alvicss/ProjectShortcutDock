using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace ProjectShortcutDock;

public partial class ColorPickerWindow : Window
{
    private bool _isSyncingValues;
    private bool _isExplicitCloseResult;

    public ColorPickerWindow(Window owner, Color initialColor)
    {
        InitializeComponent();

        Owner = owner;
        SelectedColor = initialColor;
        ApplyThemeFrom(owner);
        ApplyLanguage(owner);
        SetSliderValues(initialColor);
        UpdatePreview();
    }

    public Color SelectedColor { get; private set; }

    private void ApplyThemeFrom(Window owner)
    {
        CopyResource(owner, "WindowBrush", Brushes.White);
        CopyResource(owner, "PanelBrush", Brushes.WhiteSmoke);
        CopyResource(owner, "TextBrush", Brushes.Black);
        CopyResource(owner, "SubtleTextBrush", Brushes.DimGray);
        CopyResource(owner, "BorderBrush", Brushes.LightGray);
    }

    private void ApplyLanguage(Window owner)
    {
        var uiLanguage = owner is MainWindow mainWindow
            ? mainWindow.CurrentLanguage
            : UiText.DefaultLanguage;

        Title = UiText.Get(uiLanguage, "ColorDialogTitle");
        RedLabel.Text = "R";
        GreenLabel.Text = "G";
        BlueLabel.Text = "B";
        AlphaLabel.Text = "A";
        CancelButton.Content = UiText.Get(uiLanguage, "Cancel");
        ConfirmButton.Content = UiText.Get(uiLanguage, "Confirm");
    }

    private void CopyResource(Window owner, string key, object fallback)
    {
        Resources[key] = owner.Resources[key] ?? fallback;
    }

    private void SetSliderValues(Color color)
    {
        _isSyncingValues = true;
        AlphaSlider.Value = color.A;
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        RedValueBox.Text = color.R.ToString();
        GreenValueBox.Text = color.G.ToString();
        BlueValueBox.Text = color.B.ToString();
        AlphaValueBox.Text = color.A.ToString();
        _isSyncingValues = false;
    }

    private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSyncingValues)
        {
            return;
        }

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        _isSyncingValues = true;
        SelectedColor = Color.FromArgb(
            (byte)AlphaSlider.Value,
            (byte)RedSlider.Value,
            (byte)GreenSlider.Value,
            (byte)BlueSlider.Value);

        PreviewSwatch.Background = new SolidColorBrush(SelectedColor);
        PreviewSwatch.BorderBrush = new SolidColorBrush(CardColorPalette.WithoutAlpha(SelectedColor));
        RedValueBox.Text = ((int)RedSlider.Value).ToString();
        GreenValueBox.Text = ((int)GreenSlider.Value).ToString();
        BlueValueBox.Text = ((int)BlueSlider.Value).ToString();
        AlphaValueBox.Text = ((int)AlphaSlider.Value).ToString();
        HexValueText.Text = $"#{SelectedColor.A:X2}{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        _isSyncingValues = false;
    }

    private void ChannelValueBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter)
        {
            return;
        }

        CommitChannelValue(sender as TextBox);
        e.Handled = true;
    }

    private void ChannelValueBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitChannelValue(sender as TextBox);
    }

    private void CommitChannelValue(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        if (!byte.TryParse(textBox.Text, out var value))
        {
            value = textBox.Name switch
            {
                nameof(RedValueBox) => (byte)RedSlider.Value,
                nameof(GreenValueBox) => (byte)GreenSlider.Value,
                nameof(BlueValueBox) => (byte)BlueSlider.Value,
                _ => (byte)AlphaSlider.Value
            };
        }

        _isSyncingValues = true;
        switch (textBox.Name)
        {
            case nameof(RedValueBox):
                RedSlider.Value = value;
                break;
            case nameof(GreenValueBox):
                GreenSlider.Value = value;
                break;
            case nameof(BlueValueBox):
                BlueSlider.Value = value;
                break;
            default:
                AlphaSlider.Value = value;
                break;
        }

        _isSyncingValues = false;
        UpdatePreview();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        _isExplicitCloseResult = true;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _isExplicitCloseResult = true;
        DialogResult = false;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExplicitCloseResult || DialogResult.HasValue)
        {
            return;
        }

        _isExplicitCloseResult = true;
        DialogResult = true;
    }
}
