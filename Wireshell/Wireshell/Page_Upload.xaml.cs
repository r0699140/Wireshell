using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wireshell
{
    /// <summary>
    /// Interaction logic for Page_Upload.xaml
    /// </summary>
    public partial class Page_Upload : Page
    {
        public Page_Upload()
        {
            InitializeComponent();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = (MainWindow)Application.Current.MainWindow;
            main.UploadData(this, null);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder csv = new StringBuilder();
            csv.Append("IP;Port;Name;Start;End");

        }
    }
}
