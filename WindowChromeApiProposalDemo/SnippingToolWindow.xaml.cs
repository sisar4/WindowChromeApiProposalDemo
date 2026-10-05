using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowChromeApiProposalDemo
{
    /// <summary>Fake Snipping Tool: the whole window is acrylic. Drag a region to "capture" a placeholder image.</summary>
    public partial class SnippingToolWindow : Window
    {
        private Point? _start;

        public SnippingToolWindow()
        {
            InitializeComponent();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Selection.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Visible;
            Overlay.Focus();
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _start = e.GetPosition(Overlay);
            Canvas.SetLeft(Selection, _start.Value.X);
            Canvas.SetTop(Selection, _start.Value.Y);
            Selection.Width = 0;
            Selection.Height = 0;
            Selection.Visibility = Visibility.Visible;
            Overlay.CaptureMouse();
        }

        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (_start is not { } start)
            {
                return;
            }

            Point p = e.GetPosition(Overlay);
            Canvas.SetLeft(Selection, Math.Min(start.X, p.X));
            Canvas.SetTop(Selection, Math.Min(start.Y, p.Y));
            Selection.Width = Math.Abs(p.X - start.X);
            Selection.Height = Math.Abs(p.Y - start.Y);
        }

        private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_start == null)
            {
                return;
            }

            _start = null;
            Overlay.ReleaseMouseCapture();
            Overlay.Visibility = Visibility.Collapsed;

            if (Selection.Width < 16 || Selection.Height < 16)
            {
                return;
            }

            // Fake capture: a gradient of the size of the selection, scaled down to fit the result card.
            double scale = Math.Min(1, Math.Min((ActualWidth - 80) / Selection.Width, (ActualHeight - 160) / Selection.Height));
            Capture.Width = Selection.Width * scale;
            Capture.Height = Selection.Height * scale;
            CaptureLabel.Text = $"{(int)Selection.Width} × {(int)Selection.Height}";
            Capture.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
        }

        private void Overlay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _start = null;
                Overlay.ReleaseMouseCapture();
                Overlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}
