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
    /// Interaction logic for Page_Loggedout.xaml
    /// </summary>
    public partial class Page_Loggedout : Page
    {
        public Page_Loggedout()
        {
            InitializeComponent();
        }

        
        private void PasswordInput_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordInputPlaceholder.Visibility = Visibility.Hidden;
        }

        private void PasswordPLInput_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordInputPlaceholder.Visibility = Visibility.Hidden;
            password.Focus();
        }

        private void PasswordInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(password.Password))
            {
                passwordInputPlaceholder.Visibility = Visibility.Visible;
            }
        }
        

        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(((TextBox)sender).Text) && ((TextBox)sender).Foreground != Brushes.Black)
            {
                ((TextBox)sender).Text = string.Empty;
                ((TextBox)sender).Foreground = Brushes.Black;
            }
        }

        private void Input_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
            {
                ((TextBox)sender).Text = ((TextBox)sender).Tag.ToString();
                ((TextBox)sender).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFABADB3"));
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = (MainWindow)Application.Current.MainWindow;
            if (main.LoginUser(email.Text, password.Password))
            {
                main.MainFrame.Content = new Page_Home();
            }
            else
            {
                errorLogin.Content = "Failed to login";
            }
        }

        private void Input_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoginBtn_Click(sender, e);
        }
    }
}
