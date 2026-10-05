using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowChromeApiProposalDemo
{
    public partial class TabbedExplorerWindow : Window
    {
        public TabbedExplorerWindow()
        {
            InitializeComponent();
        }

        private void NewTab_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tab = new TabItem { Content = new FileExplorerControl { FolderPath = "%USERPROFILE%\\Desktop" } };
            Tabs.Items.Add(tab);
            tab.IsSelected = true;
        }

        private void CloseTab_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Tabs.Items.Count == 1)
            {
                Close();
            }
            else
            {
                Tabs.Items.Remove(e.Parameter);
            }
        }

        // Selecting a tab by clicking relies on keyboard focus, which an element in the caption band does not
        // always get: select explicitly.
        private void Tab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => ((TabItem)sender).IsSelected = true;
    }
}
