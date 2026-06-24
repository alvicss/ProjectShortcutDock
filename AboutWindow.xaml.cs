using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ProjectShortcutDock;

public partial class AboutWindow : Window
{
    public AboutWindow(Window owner)
    {
        InitializeComponent();

        Owner = owner;
        Title = owner is MainWindow mainWindow ? mainWindow.GetText("About") : "About";
        ApplyThemeFrom(owner);
    }

    private void ApplyThemeFrom(Window owner)
    {
        CopyResource(owner, "WindowBrush", Brushes.White);
        CopyResource(owner, "TextBrush", Brushes.Black);
        CopyResource(owner, "BorderBrush", Brushes.LightGray);
    }

    private void CopyResource(Window owner, string key, object fallback)
    {
        Resources[key] = owner.Resources[key] ?? fallback;
    }

    private void AuthorLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });

        e.Handled = true;
    }
}
