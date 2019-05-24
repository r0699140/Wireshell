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
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.ObjectModel;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Runtime.InteropServices;
using Wireshell;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Wireshell.Properties;
using LiveCharts;
using LiveCharts.Wpf;
using System.Configuration;

namespace Wireshell
{
    
    public partial class MainWindow : Window
    {
        private static List<ProcessData> activeDataList = new List<ProcessData>();
        private static List<ProcessData> currentDataList = new List<ProcessData>();
        private static ObservableCollection<ProcessData> uiDataList = new ObservableCollection<ProcessData>();

        internal List<ProcessData> GetDataList()
        {
            return activeDataList;
        }

        internal List<ProcessData> GetCurrentList()
        {
            return currentDataList;
        }

        public ObservableCollection<ProcessData> GetUIList()
        {
            return uiDataList;
        }

        private string connectionString = "";

        private int userID;

        private static readonly int refreshTime = 3;
        private static bool loggedIn = false;
        internal static bool running = true;

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //Als de nieuwe lijn leeg is zal er niets worden gedaan
            if (String.IsNullOrEmpty(outLine.Data))
                return;

            try
            {
                //Controleert wanneer netstat opnieuw controleert naar connecties
                if(outLine.Data.Equals("Active Connections"))
                {
                    //Console.WriteLine("----- new list -----\n old list length: " + currentDataList.Count);
                    try
                    {
                        for(int i = activeDataList.Count-1; i >= 0; i--)
                        {
                            //Console.Write(i + " " + activeDataList[i].Name + " \t ------\t");
                            //activeDataList[i].WriteData();

                            ProcessData currentData = activeDataList[i];
                            bool connected = false;

                            foreach (ProcessData oldData in currentDataList)
                            {
                                //Console.Write("\t" + oldData.Name + " is being checked -----\t");
                                //oldData.WriteData();
                                if (currentData.Equals(oldData))
                                {
                                    connected = true;
                                    break;
                                }
                            }

                            if (!connected)
                            {
                                if (loggedIn)
                                {
                                    currentData.Connected = false;
                                    currentData.Uploaded = false;
                                    currentData.TimeStamp = DateTime.Now;
                                }
                                else
                                {
                                    activeDataList.RemoveAt(i);
                                }
                                
                            }

                        }

                        for (int i = uiDataList.Count - 1; i >= 0; i--)
                        {
                            ProcessData currentData = uiDataList[i];
                            bool connected = false;

                            foreach (ProcessData oldData in currentDataList)
                            {
                                if (currentData.Equals(oldData))
                                {
                                    connected = true;
                                    break;
                                }
                            }

                            if (!connected)
                            {
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    uiDataList.RemoveAt(i);
                                }));
                                

                                Console.WriteLine(currentData.Name + " disconected");
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        //Ververst de lijst van actieve connecties
                        currentDataList.Clear();
                    }
                    
                }
                else
                {
                    string[] tokens = Regex.Split(outLine.Data, "\\s+");

                    //Filtert alle onnodige verbindingen
                    //94.227.224.119 is het ip van sql server waarop wireshell draait
                    if (tokens[1].Equals("TCP") && !tokens[3].Split(':')[0].Equals("0.0.0.0") && !tokens[3].Split(':')[0].Equals("94.227.224.119") && !tokens[3].Split(':')[0].Equals("127.0.0.1") && !tokens[5].Equals("0") && tokens[4].Equals("ESTABLISHED"))
                    {
                        Process p;
                        string pName = "";
                        try
                        {
                            p = Process.GetProcessById(Convert.ToInt32(tokens[5]));
                            pName = p.ProcessName;
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine("Process doesn't exists: " + tokens[5] + "\n" + ex.Message);
                            return;
                        }

                        if (pName == "svchost")
                            return;
                        

                        //Creert nieuwe data adv van de output van netstat
                        ProcessData newData = new ProcessData(tokens[3].Split(':')[0], Convert.ToInt64(tokens[3].Split(':')[1]), Convert.ToInt32(tokens[5]), pName, DateTime.Now);

                        //Voegt de nieuwe data toe aan de actieve connecties
                        currentDataList.Add(newData);
                        
                        //Controleert of een gelijkaardige connectie al eerder verbonden is
                        bool newCon = true;
                        foreach(ProcessData data in activeDataList)
                        {
                            if (data.Equals(newData))
                            {
                                newCon = false;
                                break;
                            }
                                
                        }

                        //Als het een nieuwe verbinding is zal deze worden teogevoegd aan activeDataList
                        if (newCon)
                        {
                            activeDataList.Add(newData);
                        }

                        newCon = true;
                        foreach (ProcessData data in uiDataList)
                        {
                            if (data.Equals(newData))
                            {
                                newCon = false;
                                break;
                            }
                        }
                        
                        if (newCon)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                uiDataList.Add(newData);
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ReadActiveTCPConnection()
        {
            try
            {
                using (Process p = new Process())
                {
                    //Maakt een nieuw process aan van netstat dat om de 3 seconden de huidige TPC verbindingen weergeeft
                    //Als er een nieuwe lijn wordt aangemaakt in de stdout van netstat zal dit worden gelezen door OutputHandler die de lijn omzet naar een object
                    ProcessStartInfo ps = new ProcessStartInfo
                    {
                        Arguments = "-a -n -o -p TCP " + refreshTime.ToString(),
                        FileName = "netstat.exe",

                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    p.StartInfo = ps;

                    p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);

                    p.Start();
                    p.BeginOutputReadLine();
                }
            }
            catch (Exception ex){
                Console.WriteLine("Error: " + ex.ToString());
            }
        }


        //Zal alle onafgemaakte lijnen in de databank opvullen
        public void CleanRows()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    MySqlCommand cmd = new MySqlCommand("sp_FixOpenConnections", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("userID", userID);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //Zal aan de hand van een query true of false geven als de gegeven data overeen komt met data in de databank
        //Als de login sucsesvol is zal CleanRows ook worden uitgevoerd
        internal bool LoginUser(string email, string password)
        {
            userID = 0;

            try
            {        
                using(MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("sp_getUserMail", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("mail", email);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows) return false;
                        
                        reader.Read();
                        if (!BCrypt.Net.BCrypt.Verify(password, reader["PasswordHash"].ToString())) return false;
                        
                        if (!reader.GetBoolean("Activated"))
                        {
                            userID = -1;
                            return false;
                        }

                        userID = Int16.Parse(reader["AccountID"].ToString());

                        Settings.Default.UserLoggedOn = true;
                        Settings.Default.Email = email;
                        Settings.Default.Password = password;
                        Settings.Default.Name = reader["Name"].ToString();
                        Settings.Default.Firstname = reader["FirstName"].ToString();

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            LogoutButton.Content = "Logout";
                            UserName.Text = Settings.Default.Firstname + " " + Settings.Default.Name;
                            AccountButton.Text = "ACCOUNT";
                        }));
                        
                        Timer uploadTimer = new System.Timers.Timer(refreshTime * 7000);

                        uploadTimer.Elapsed += UploadData;
                        uploadTimer.AutoReset = true;
                        uploadTimer.Enabled = true;
                        loggedIn = true;

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return true;
            }
            finally
            {
                Settings.Default.Save();
            }
        }




        public MainWindow()
        {

            InitializeComponent();

            connectionString = ConfigurationManager.ConnectionStrings["Databank"].ConnectionString;

            new Thread(() =>
            {
                if (Settings.Default.UserLoggedOn)
                {
                    string email = Settings.Default.Email;
                    string passwd = Settings.Default.Password;

                    if (!LoginUser(email, passwd))
                    {
                        if (!running) return;

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            UserName.Text = "Failed to login";
                        }));
                    }
                }

                Thread tcpRead = new Thread(new ThreadStart(ReadActiveTCPConnection));
                tcpRead.Start();
            }).Start();
            
        }

        internal void UploadData(Object source, ElapsedEventArgs e)
        {
            if (!loggedIn)
                return;

            Console.WriteLine("checking for data change");
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    MySqlCommand addData = new MySqlCommand("sp_AddNewData", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    MySqlCommand finishData = new MySqlCommand("sp_FinishData", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    MySqlCommand tinyData = new MySqlCommand("sp_AddFinishedData", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    for (int i = activeDataList.Count - 1; i >= 0; i--)
                    {
                        ProcessData data = activeDataList[i];
                        if (data.Uploaded)
                            continue;

                        Console.WriteLine(data.PID + " " + data.Ip + " " + data.Name + " " + data.Port + " " + data.TimeStamp);
                        if (data.Connected)
                        {
                            Console.WriteLine("\tconnected");
                            addData.Parameters.AddWithValue("userID", userID);
                            addData.Parameters.AddWithValue("ip", data.Ip);
                            addData.Parameters.AddWithValue("name", data.Name);
                            addData.Parameters.AddWithValue("startDate", data.TimeStamp);
                            addData.Parameters.AddWithValue("port", data.Port);
                            using (MySqlDataReader reader = addData.ExecuteReader())
                            {
                                reader.Read();
                                data.Pointer = reader.GetInt16("DataID");
                                Console.WriteLine("\tDone ID: " + data.Pointer);
                            }
                            addData.Parameters.Clear();

                            activeDataList[i].Uploaded = true;
                        }
                        else
                        {
                            if (data.Pointer == -1)
                            {
                                Console.WriteLine("\tstart and ended");
                                tinyData.Parameters.AddWithValue("userID", userID);
                                tinyData.Parameters.AddWithValue("ip", data.Ip);
                                tinyData.Parameters.AddWithValue("name", data.Name);
                                tinyData.Parameters.AddWithValue("startDate", data.TimeStamp);
                                tinyData.Parameters.AddWithValue("endDate", data.TimeStamp);
                                tinyData.Parameters.AddWithValue("port", data.Port);

                                tinyData.ExecuteNonQuery();
                                tinyData.Parameters.Clear();
                                Console.WriteLine("\tDone");

                                activeDataList.RemoveAt(i);
                            }
                            else
                            {
                                Console.WriteLine("\tdisconnected");
                                finishData.Parameters.AddWithValue("prevDataID", data.Pointer);
                                finishData.Parameters.AddWithValue("finishDate", data.TimeStamp);
                                finishData.ExecuteNonQuery();
                                finishData.Parameters.Clear();
                                Console.WriteLine("\tDone");

                                activeDataList.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Check done");
            }
        }

        public void Logout()
        {
            running = false;

            Settings.Default.UserLoggedOn = false;
            Settings.Default.Name = "";
            Settings.Default.Firstname = "";
            Settings.Default.Email = "";
            Settings.Default.Save();

            UserName.Text = "";
            LogoutButton.Content = "Login";

            AccountButton.Text = "LOGIN";

            CleanRows();
            MainFrame.Content = new Page_Home();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadData(sender, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            new Thread(() =>
            {
                CleanRows();
                Settings.Default.Save();
            }).Start();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void Account_Click(object sender, RoutedEventArgs e)
        {
            if (loggedIn)
            {
                MainFrame.Content = new Page_Account();
            }
            else
            {
                MainFrame.Content = new Page_Loggedout();
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new Page_Home();
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new Page_Upload();
        }

        private void Connections_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = new Page_Connections();
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost/");
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.UserLoggedOn)
            {
                Logout();
            }
            else
            {
                MainFrame.Content = new Page_Loggedout();
            }

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
