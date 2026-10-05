using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Ribbon;

namespace WindowChromeApiProposalDemo
{
    /// <summary>
    /// File explorer with a ribbon in the title bar area. Selecting a picture shows the "Picture Tools" contextual tab,
    /// which can set the picture as the desktop background. Navigation and the show/hide buttons are in the XAML.
    /// </summary>
    public partial class RibbonExplorerWindow : RibbonWindow
    {
        private static readonly HashSet<string> s_pictureExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp",
        };

        private string? _picture;

        public RibbonExplorerWindow()
        {
            InitializeComponent();
            Explorer.SelectionChanged += (_, _) => UpdatePictureTools();
        }

        private void UpdatePictureTools()
        {
            _picture = Explorer.SelectedPaths is [string single] && s_pictureExtensions.Contains(Path.GetExtension(single))
                ? single
                : null;
            PictureTools.Visibility = _picture != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenInShell_Click(object sender, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Explorer.CurrentPath}\"") { UseShellExecute = true });

        private void OpenPicture_Click(object sender, RoutedEventArgs e)
        {
            if (_picture != null)
            {
                Process.Start(new ProcessStartInfo(_picture) { UseShellExecute = true });
            }
        }

        private void SetWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (_picture != null && !SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, _picture, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
            {
                MessageBox.Show(this, $"Could not set the desktop background (error {Marshal.GetLastWin32Error()}).", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);
    }
}
