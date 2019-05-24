using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Wireshell;

namespace Wireshell
{

    internal class Graph
    {
        private List<string> _labels;
        private SeriesCollection _serie { get; set; }

        public List<string> Labels
        {
            get { return _labels; }
            set
            {
                _labels = value;
                NotifyPropertyChanged("Labels");
            }
        }
        public SeriesCollection Serie
        {
            get { return _serie; }
            set
            {
                _serie = value;
                NotifyPropertyChanged("Serie");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private CartesianChart Chart { get; set; }

        private ObservableCollection<ProcessData> PData { get; set; }
        public Graph(CartesianChart chart, ObservableCollection<ProcessData> data, string serieTitle)
        {
            this.Chart = chart;

            Chart.AxisY[0].Separator.Step = 1;
            Chart.AxisY[0].Separator.IsEnabled = true;

            Chart.AxisX[0].Separator.Step = 1;
            
            Labels = new List<string>();
            PData = data;
            PData.CollectionChanged += PData_CollectionChanged;

            FillChart();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ProcessData changedData = null;
            int amount = 0;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                changedData = (ProcessData)e.NewItems[e.NewItems.Count - 1];
                amount = 1;
            }
            else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                changedData = (ProcessData)e.OldItems[0];
                amount = -1;
            }
            ChangeValue(changedData.Name, amount);
        }

        public void FillChart()
        {
            Serie = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Amount",
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6DAC39"),
                    Values = new ChartValues<int>()
                }
            };

            foreach (ProcessData process in PData)
            {
                SetValue(process.Name, process.CountByName(PData.ToList()));
            }


            Chart.DataContext = this;
        }

        internal void RemoveData(string name)
        {
            if (!Labels.Contains(name))
            {
                Console.WriteLine("Index not found with name: " + name);
                return;
            }

            int index = Labels.IndexOf(name);

            Labels.Remove(name);
            Serie[0].Values.RemoveAt(index);           
        }

        internal void ChangeValue(string name, int amount)
        {
            int index = -1;
            if (Labels.Contains(name))
            {
                index = Labels.IndexOf(name);
                Serie[0].Values[index] = (int)Serie[0].Values[index] + amount;
            }
            else
            {
                if (amount < 0) amount = 0;

                Serie[0].Values.Add(amount);
                Labels.Add(name);
                index = Labels.IndexOf(name);
            }

            if((int)Serie[0].Values[index] <= 0)
            {
                RemoveData(name);
            }
        }

        internal void SetValue(string name, int amount)
        {
            int index = -1;
            if (Labels.Contains(name))
            {
                index = Labels.IndexOf(name);
                Serie[0].Values[index] = amount;
            }
            else
            {
                if (amount < 0) amount = 0;

                Serie[0].Values.Add(amount);
                Labels.Add(name);
                index = Labels.IndexOf(name);
            }

            if ((int)Serie[0].Values[index] <= 0)
            {
                RemoveData(name);
            }
        }
    }

    internal class PreformanceMonitor : INotifyPropertyChanged
    {
        internal PerformanceCounter totalCPUCounter = new PerformanceCounter();
        internal PerformanceCounter totalMemCounter = new PerformanceCounter();
        internal PerformanceCounter totalDisk = new PerformanceCounter();

        public event PropertyChangedEventHandler PropertyChanged;

        private string _diskUsage;
        public string DiskUsage {
            get { return _diskUsage; }
            set
            {
                _diskUsage = value;
                NotifyPropertyChanged("DiskUsage");
            }
        }

        private string _memoryUsage;
        public string MemoryUsage
        {
            get { return _memoryUsage; }
            set
            {
                _memoryUsage = value;
                NotifyPropertyChanged("MemoryUsage");
            }
        }

        private string _cPUUsage;
        public string CPUUsage
        {
            get { return _cPUUsage; }
            set
            {
                _cPUUsage = value;
                NotifyPropertyChanged("CPUUsage");
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public PreformanceMonitor()
        {
            totalDisk.CategoryName = "PhysicalDisk";
            totalDisk.CounterName = "% Idle Time";
            totalDisk.InstanceName = "_Total";

            totalMemCounter.CategoryName = "Memory";
            totalMemCounter.CounterName = "% Committed Bytes In Use";

            totalCPUCounter.CategoryName = "Processor";
            totalCPUCounter.CounterName = "% Processor Time";
            totalCPUCounter.InstanceName = "_Total";

            DiskUsage = "0%";
            MemoryUsage = "0%";
            CPUUsage = "0%";

            Timer prefTimer = new System.Timers.Timer(1000);

            prefTimer.Elapsed += ReadPerformance;
            prefTimer.AutoReset = true;
            prefTimer.Enabled = true;
        }

        private void ReadPerformance(object sender, ElapsedEventArgs e)
        {
            CPUUsage = Math.Round(totalCPUCounter.NextValue(), 2) + "%";
            MemoryUsage = Math.Round(totalMemCounter.NextValue(), 2) + "%";

            float usedDisk = (100 - totalDisk.NextValue());
            DiskUsage = Math.Round((usedDisk > 0) ? usedDisk : 0, 2) + "%";
        }
    }

    /// <summary>
    /// Interaction logic for Page_Home.xaml
    /// </summary>
    public partial class Page_Home : Page
    {
        internal Graph graph;
        internal PreformanceMonitor performanceMonitor;

        public Page_Home()
        {
            InitializeComponent();
            graph = new Graph(chart, ((MainWindow)Application.Current.MainWindow).GetUIList(), "Amount");
            performanceMonitor = new PreformanceMonitor();

            RamUsage.DataContext = performanceMonitor;
            CPUUsage.DataContext = performanceMonitor;
            DiskUsage.DataContext = performanceMonitor;
        }


    }
}
