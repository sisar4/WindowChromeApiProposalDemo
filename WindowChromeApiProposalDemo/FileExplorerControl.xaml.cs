using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowChromeApiProposalDemo
{
    /// <summary>
    /// Two-pane file explorer: a folder tree on the left, the shell view of the selected folder (hosted by the
    /// WebBrowser control) on the right.
    /// </summary>
    public partial class FileExplorerControl : UserControl
    {
        private const string DummyTag = "<dummy>";

        public static readonly DependencyProperty IsNavigationBarVisibleProperty = DependencyProperty.Register(
            nameof(IsNavigationBarVisible), typeof(bool), typeof(FileExplorerControl),
            new PropertyMetadata(true, (d, e) => ((FileExplorerControl)d).NavigationBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public static readonly DependencyProperty IsStatusBarVisibleProperty = DependencyProperty.Register(
            nameof(IsStatusBarVisible), typeof(bool), typeof(FileExplorerControl),
            new PropertyMetadata(true, (d, e) => ((FileExplorerControl)d).StatusBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public static readonly DependencyProperty IsTreeVisibleProperty = DependencyProperty.Register(
            nameof(IsTreeVisible), typeof(bool), typeof(FileExplorerControl),
            new PropertyMetadata(true, (d, e) => ((FileExplorerControl)d).OnIsTreeVisibleChanged((bool)e.NewValue)));

        public static readonly DependencyProperty TreeBackgroundProperty = DependencyProperty.Register(
            nameof(TreeBackground), typeof(Brush), typeof(FileExplorerControl), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty PageBackgroundProperty = DependencyProperty.Register(
            nameof(PageBackground), typeof(Brush), typeof(FileExplorerControl), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty NavigationBarBackgroundProperty = DependencyProperty.Register(
            nameof(NavigationBarBackground), typeof(Brush), typeof(FileExplorerControl), new PropertyMetadata(Brushes.Transparent));

        /// <summary>Folder shown. Bindable (two-way); environment variables such as %USERPROFILE% are expanded.</summary>
        public static readonly DependencyProperty FolderPathProperty = DependencyProperty.Register(
            nameof(FolderPath), typeof(string), typeof(FileExplorerControl),
            new FrameworkPropertyMetadata(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((FileExplorerControl)d).OnFolderPathChanged((string)e.NewValue)));

        private static readonly DependencyPropertyKey FolderNamePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FolderName), typeof(string), typeof(FileExplorerControl), new PropertyMetadata(""));

        /// <summary>Name of the folder shown (the drive for a root folder).</summary>
        public static readonly DependencyProperty FolderNameProperty = FolderNamePropertyKey.DependencyProperty;

        /// <summary>Up one level. Back, Forward and Refresh use the standard <see cref="NavigationCommands"/>.</summary>
        public static readonly RoutedUICommand GoUpCommand = new("Up", nameof(GoUpCommand), typeof(FileExplorerControl));

        private readonly List<string> _recent = new();
        private string? _shownPath;
        private readonly System.Windows.Threading.DispatcherTimer _selectionTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private string[] _selectedPaths = [];
        private bool _syncingTree;
        private double _treeWidth = 240;

        public FileExplorerControl()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseBack, (_, _) => GoBack(), (_, e) => e.CanExecute = Browser.CanGoBack));
            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseForward, (_, _) => GoForward(), (_, e) => e.CanExecute = Browser.CanGoForward));
            CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, (_, _) => Refresh()));
            CommandBindings.Add(new CommandBinding(GoUpCommand, (_, _) => GoUp(), (_, e) => e.CanExecute = ParentOf(CurrentPath) != null));

            BuildTree();
            Browser.Navigated += Browser_Navigated;
            Browser.LoadCompleted += (_, _) =>
                CommandManager.InvalidateRequerySuggested();
            Loaded += (_, _) =>
            {
                Navigate(FolderPath, force: true);
                _selectionTimer.Start();
            };
            Unloaded += (_, _) => _selectionTimer.Stop();
            _selectionTimer.Tick += (_, _) =>
                PollSelection();
        }

        public string FolderPath
        {
            get => (string)GetValue(FolderPathProperty);
            set => SetValue(FolderPathProperty, value);
        }

        public string FolderName => (string)GetValue(FolderNameProperty);

        /// <summary>Folder currently shown.</summary>
        public string CurrentPath => _shownPath ?? FolderPath;

        private void OnFolderPathChanged(string path)
        {
            string expanded = Environment.ExpandEnvironmentVariables(path);
            if (expanded != path)
            {
                FolderPath = expanded;
            }
            else if (!string.Equals(path, _shownPath, StringComparison.OrdinalIgnoreCase))
            {
                Navigate(path);
            }
        }

        /// <summary>Background of the folder tree; transparent by default so the window backdrop shows through.</summary>
        public Brush TreeBackground
        {
            get => (Brush)GetValue(TreeBackgroundProperty);
            set => SetValue(TreeBackgroundProperty, value);
        }

        /// <summary>Fill of the area below the navigation bar (tree, splitter, status bar); transparent by default.</summary>
        public Brush PageBackground
        {
            get => (Brush)GetValue(PageBackgroundProperty);
            set => SetValue(PageBackgroundProperty, value);
        }

        /// <summary>Fill of the navigation bar; transparent by default so the window backdrop shows through.</summary>
        public Brush NavigationBarBackground
        {
            get => (Brush)GetValue(NavigationBarBackgroundProperty);
            set => SetValue(NavigationBarBackgroundProperty, value);
        }

        /// <summary>Full paths of the items selected in the folder view.</summary>
        public IReadOnlyList<string> SelectedPaths => _selectedPaths;

        /// <summary>Raised when the selection in the folder view changes.</summary>
        public event EventHandler? SelectionChanged;

        public bool IsNavigationBarVisible
        {
            get => (bool)GetValue(IsNavigationBarVisibleProperty);
            set => SetValue(IsNavigationBarVisibleProperty, value);
        }

        public bool IsStatusBarVisible
        {
            get => (bool)GetValue(IsStatusBarVisibleProperty);
            set => SetValue(IsStatusBarVisibleProperty, value);
        }

        public bool IsTreeVisible
        {
            get => (bool)GetValue(IsTreeVisibleProperty);
            set => SetValue(IsTreeVisibleProperty, value);
        }


        #region Navigation

        public void Navigate(string path) => Navigate(path, force: false);

        private void Navigate(string path, bool force)
        {
            if (!Directory.Exists(path))
            {
                PathBox.Foreground = Brushes.IndianRed;
                return;
            }

            PathBox.ClearValue(ForegroundProperty);
            if (!force && string.Equals(path, CurrentPath, StringComparison.OrdinalIgnoreCase) && Browser.Source != null)
            {
                return;
            }

            Browser.Navigate(new Uri(path));
            ApplyPath(path);
        }

        public void GoBack()
        {
            if (Browser.CanGoBack)
            {
                Browser.GoBack();
            }
        }

        public void GoForward()
        {
            if (Browser.CanGoForward)
            {
                Browser.GoForward();
            }
        }

        public void GoUp()
        {
            if (ParentOf(CurrentPath) is { } parent)
            {
                Navigate(parent.FullName);
            }
        }

        private static DirectoryInfo? ParentOf(string path) => Directory.GetParent(path.TrimEnd(Path.DirectorySeparatorChar));

        public void Refresh() => Browser.Refresh();

        private void Browser_Navigated(object? sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // The user may have opened a folder inside the shell view: follow it.
            if (e.Uri is { IsFile: true } uri && Directory.Exists(uri.LocalPath))
            {
                ApplyPath(uri.LocalPath);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void ApplyPath(string path)
        {
            _shownPath = path;
            SetCurrentValue(FolderPathProperty, path);
            string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
            SetValue(FolderNamePropertyKey, string.IsNullOrEmpty(name) ? path : name);
            PathBox.Text = path;
            PathBox.ClearValue(ForegroundProperty);
            StatusText.Text = DescribeFolder(path);

            _recent.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            _recent.Insert(0, path);
            if (_recent.Count > 12)
            {
                _recent.RemoveAt(_recent.Count - 1);
            }

            SelectInTree(path);
            CommandManager.InvalidateRequerySuggested();
        }

        private static string DescribeFolder(string path)
        {
            try
            {
                int count = Directory.EnumerateFileSystemEntries(path).Count();
                return count == 1 ? "1 item" : $"{count} items";
            }
            catch (Exception)
            {
                return "Cannot read the folder";
            }
        }

        #endregion

        #region Selection

        // The folder view is the shell's own ShellFolderView, an IDispatch object: it raises COM events that WPF does
        // not surface, so the selection is polled.
        private void PollSelection()
        {
            string[] current = ReadSelection();
            if (!current.SequenceEqual(_selectedPaths, StringComparer.OrdinalIgnoreCase))
            {
                _selectedPaths = current;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private string[] ReadSelection()
        {
            try
            {
                if (Browser.Document is not { } document)
                {
                    return [];
                }

                dynamic items = ((dynamic)document).SelectedItems();
                int count = items.Count;
                var paths = new string[count];
                for (int i = 0; i < count; i++)
                {
                    paths[i] = items.Item(i).Path;
                }

                return paths;
            }
            catch (Exception)
            {
                // No folder view yet, or the page is not a shell view.
                return [];
            }
        }

        #endregion

        #region Navigation bar events

        private void Recent_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = RecentButton, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            foreach (string path in _recent)
            {
                var item = new MenuItem { Header = path };
                item.Click += (_, _) => Navigate(path);
                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }

        private void PathBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Navigate(PathBox.Text.Trim().Trim('"'));
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PathBox.Text = CurrentPath;
                PathBox.ClearValue(ForegroundProperty);
                e.Handled = true;
            }
        }

        private void PathBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => PathBox.SelectAll();

        #endregion

        #region Tree

        private void OnIsTreeVisibleChanged(bool visible)
        {
            if (visible)
            {
                Tree.Visibility = Visibility.Visible;
                RootGrid.ColumnDefinitions[0].Width = new GridLength(_treeWidth);
                RootGrid.ColumnDefinitions[1].Width = new GridLength(5);
            }
            else
            {
                _treeWidth = Math.Max(120, RootGrid.ColumnDefinitions[0].ActualWidth);
                Tree.Visibility = Visibility.Collapsed;
                RootGrid.ColumnDefinitions[0].Width = new GridLength(0);
                RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }

        private void BuildTree()
        {
            (string Name, Environment.SpecialFolder Folder, string Glyph)[] quick =
            [
                ("Desktop", Environment.SpecialFolder.DesktopDirectory, ""),
                ("Documents", Environment.SpecialFolder.MyDocuments, ""),
                ("Pictures", Environment.SpecialFolder.MyPictures, ""),
                ("Music", Environment.SpecialFolder.MyMusic, ""),
                ("Videos", Environment.SpecialFolder.MyVideos, ""),
            ];

            foreach (var (name, folder, glyph) in quick)
            {
                string path = Environment.GetFolderPath(folder);
                if (Directory.Exists(path))
                {
                    Tree.Items.Add(CreateNode(name, path, glyph, "#FF3C8DD9"));
                }
            }

            var thisPc = new TreeViewItem { Header = CreateHeader("This PC", "", "#FF3C8DD9"), IsExpanded = true, Tag = null };
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                string label = string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                thisPc.Items.Add(CreateNode($"{label} ({drive.Name.TrimEnd('\\')})", drive.RootDirectory.FullName, "", "#FF8A8A8A"));
            }

            Tree.Items.Add(thisPc);
            ThisPcNode = thisPc;
        }

        private TreeViewItem ThisPcNode { get; set; } = null!;

        private TreeViewItem CreateNode(string name, string path, string glyph = "", string glyphColor = "#FFF2C14E")
        {
            var node = new TreeViewItem { Header = CreateHeader(name, glyph, glyphColor), Tag = path };
            if (HasSubfolders(path))
            {
                node.Items.Add(new TreeViewItem { Tag = DummyTag });
            }

            node.Expanded += Node_Expanded;
            return node;
        }

        private static StackPanel CreateHeader(string text, string glyph, string glyphColor)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                Foreground = (Brush)new BrushConverter().ConvertFromString(glyphColor)!,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            panel.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private static bool HasSubfolders(string path)
        {
            try
            {
                return EnumerateVisibleFolders(path).Any();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static IEnumerable<DirectoryInfo> EnumerateVisibleFolders(string path) =>
            new DirectoryInfo(path).EnumerateDirectories()
                .Where(d => (d.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase);

        private void Node_Expanded(object sender, RoutedEventArgs e)
        {
            var node = (TreeViewItem)sender;
            e.Handled = true;
            LoadChildren(node);
        }

        private void LoadChildren(TreeViewItem node)
        {
            if (node.Tag is not string path || node.Items.Count != 1 || (node.Items[0] as TreeViewItem)?.Tag as string != DummyTag)
            {
                return;
            }

            node.Items.Clear();
            try
            {
                foreach (DirectoryInfo dir in EnumerateVisibleFolders(path))
                {
                    node.Items.Add(CreateNode(dir.Name, dir.FullName));
                }
            }
            catch (Exception)
            {
                // Access denied: the node simply stays empty.
            }
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!_syncingTree && e.NewValue is TreeViewItem { Tag: string path })
            {
                Navigate(path);
            }
        }

        /// <summary>Expands and selects the node of <paramref name="path"/> under "This PC", if the path is on a ready drive.</summary>
        private void SelectInTree(string path)
        {
            string? root = Path.GetPathRoot(path);
            if (root == null)
            {
                return;
            }

            TreeViewItem? node = ThisPcNode.Items.OfType<TreeViewItem>()
                .FirstOrDefault(n => string.Equals(n.Tag as string, root, StringComparison.OrdinalIgnoreCase));
            if (node == null)
            {
                return;
            }

            string current = root;
            foreach (string segment in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                node.IsExpanded = true;
                LoadChildren(node);
                current = Path.Combine(current, segment);
                TreeViewItem? next = node.Items.OfType<TreeViewItem>()
                    .FirstOrDefault(n => string.Equals(n.Tag as string, current, StringComparison.OrdinalIgnoreCase));
                if (next == null)
                {
                    break;
                }

                node = next;
            }

            _syncingTree = true;
            try
            {
                node.IsSelected = true;
                node.BringIntoView();
            }
            finally
            {
                _syncingTree = false;
            }
        }

        #endregion
    }
}
