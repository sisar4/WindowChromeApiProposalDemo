using System.Windows;
using System.Windows.Controls;

namespace WindowChromeApiProposalDemo
{
    /// <summary>Launcher: one button per demo scenario (see the XAML).</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>Opens a scenario straight away: <c>--open 1</c> .. <c>--open 6</c> on the command line.</summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--open");
            if (at >= 0 && at + 1 < args.Length && int.TryParse(args[at + 1], out int n) && n >= 1 && n <= Scenarios.Children.Count)
            {
                Open((Button)Scenarios.Children[n - 1]);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e) => Open((Button)sender);

        private static void Open(Button scenario) => ((Window)Activator.CreateInstance((Type)scenario.Tag)!).Show();
    }
}
