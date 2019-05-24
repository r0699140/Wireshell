using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Wireshell
{


    /// <summary>
    /// Interaction logic for Page_Connections.xaml
    /// </summary>
    public partial class Page_Connections : Page
    {
        public List<ProcessData> dataList;

        public Page_Connections()
        {
            InitializeComponent();

            MainWindow main = (MainWindow)Application.Current.MainWindow;
            DataContext = main.GetUIList();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //int PID = ((ProcessData)((Button)sender).DataContext).PID;
            //Process proc = Process.GetProcessById(PID);
            //proc.Kill();
        }
    }
}
