using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowChromeApiProposalDemo
{
    /// <summary>Fake Task Manager: process names and memory are real, the other figures are made up.</summary>
    public partial class TaskManagerWindow : Window
    {
        private readonly List<ProcessRow> _rows = new();
        private readonly Random _random = new();
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public TaskManagerWindow()
        {
            InitializeComponent();

            Nav.ItemsSource = new NavItem[]
            {
                new("", "Processes"),
                new("", "Performance"),
                new("", "App history"),
                new("", "Startup apps"),
                new("", "Users"),
                new("", "Details"),
                new("", "Services"),
            };
            Nav.SelectedIndex = 0;

            foreach (Process p in Process.GetProcesses().OrderByDescending(SafeWorkingSet).Take(40))
            {
                _rows.Add(new ProcessRow(p.ProcessName, SafeWorkingSet(p) / 1048576d, _random));
                p.Dispose();
            }

            ProcessGrid.ItemsSource = _rows;
            Tick(null, EventArgs.Empty);
            _timer.Tick += Tick;
            _timer.Start();
            Closed += (_, _) => _timer.Stop();
        }

        private static long SafeWorkingSet(Process p)
        {
            try
            {
                return p.WorkingSet64;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            foreach (ProcessRow row in _rows)
            {
                row.Update(_random);
            }

            CpuTotal.Text = $"{Math.Min(100, _rows.Sum(r => r.Cpu)):0}%";
            MemTotal.Text = $"{_rows.Sum(r => r.Memory) / 1024:0.0} GB";
            DiskTotal.Text = $"{_rows.Sum(r => r.Disk):0.0} MB/s";
        }
    }

    public sealed record NavItem(string Glyph, string Text);

    public sealed class ProcessRow : INotifyPropertyChanged
    {
        private readonly double _weight;
        private double _cpu, _disk, _network;

        public ProcessRow(string name, double memoryMb, Random random)
        {
            Name = name;
            Memory = memoryMb;
            _weight = random.NextDouble() < 0.15 ? 6 : 0.4;
        }

        public string Name { get; }

        public double Memory { get; }

        public double Cpu { get => _cpu; private set => Set(ref _cpu, value, nameof(Cpu)); }

        public double Disk { get => _disk; private set => Set(ref _disk, value, nameof(Disk)); }

        public double Network { get => _network; private set => Set(ref _network, value, nameof(Network)); }

        public void Update(Random random)
        {
            Cpu = Math.Clamp(_cpu + (random.NextDouble() - 0.45) * _weight, 0, 60);
            Disk = Math.Clamp(_disk + (random.NextDouble() - 0.5) * _weight * 0.8, 0, 80);
            Network = Math.Clamp(_network + (random.NextDouble() - 0.5) * _weight * 0.5, 0, 80);
        }

        private void Set(ref double field, double value, string name)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>Task Manager style heat map: the more intense the value, the stronger the amber tint. The parameter is the value for full intensity.</summary>
    public sealed class HeatBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double full = double.Parse((string)parameter, CultureInfo.InvariantCulture);
            double intensity = Math.Clamp(System.Convert.ToDouble(value, CultureInfo.InvariantCulture) / full, 0, 1);
            var brush = new SolidColorBrush(Color.FromArgb((byte)(intensity * 150), 0xFF, 0xB9, 0x00));
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}



