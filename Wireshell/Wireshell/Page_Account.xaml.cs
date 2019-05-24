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
using Wireshell.Properties;

namespace Wireshell
{
    /// <summary>
    /// Interaction logic for Page_Account.xaml
    /// </summary>
    public partial class Page_Account : Page
    {
        public Page_Account()
        {
            InitializeComponent();

            accountName.Text = Settings.Default.Name;
            accountFirstName.Text = Settings.Default.Firstname;
            accountMail.Text = Settings.Default.Email;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Logout();
        }
    }
}
