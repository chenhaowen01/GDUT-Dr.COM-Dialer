using System;
using System.Linq;
using System.Configuration;
using System.Windows;
using DotRas;
using System.Diagnostics;

namespace pppoe_dialer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        Process process = new Process();
        public MainWindow()
        {
            InitializeComponent();
            CreateConnect("gdut drcom");
            hangup.IsEnabled = false;
            //process = Process.Start(".\\gdut-drcom.exe", "--remote-ip=10.0.3.2");
            //process.Kill();
            tb_username.Text = ReadSetting("username");
            if (ReadSetting("remember_password") == "true")
            {
                remember_checkbox.IsChecked = true;
                pb_password.Password = ReadSetting("password");
            }
            if (ReadSetting("autologin") == "true")
            {
                autolog_checkbox.IsChecked = true;
                login();
            }
            if (remember_checkbox.IsChecked.Value)
            {
                
            }
        }

        public void CreateConnect(string ConnectName)
        {
            RasDialer dialer = new RasDialer();
            RasPhoneBook book = new RasPhoneBook();
            try
            {
                book.Open(RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User));
                if (book.Entries.Contains(ConnectName))
                {
                    book.Entries[ConnectName].PhoneNumber = " ";
                    book.Entries[ConnectName].Update();
                }
                else {
                    System.Collections.ObjectModel.ReadOnlyCollection<RasDevice> readOnlyCollection = RasDevice.GetDevices();
                    RasDevice device = RasDevice.GetDevices().Where(o => o.DeviceType == RasDeviceType.PPPoE).First();
                    RasEntry entry = RasEntry.CreateBroadbandEntry(ConnectName, device);
                    entry.PhoneNumber = " ";
                    book.Entries.Add(entry);
                }
            }
            catch (Exception)
            {
                lb_status.Content = "创建PPPoE连接失败";
            }
        }

        private void dial_Click(object sender, RoutedEventArgs e)
        {
            var appSettings = ConfigurationManager.AppSettings;

            AddUpdateAppSettings("username", tb_username.Text);
            if (remember_checkbox.IsChecked.Value)
            {
                AddUpdateAppSettings("password", pb_password.Password);
                AddUpdateAppSettings("remember_password", "true");
            }
            else
            {
                AddUpdateAppSettings("password", "");
                AddUpdateAppSettings("remember_password","false");
            }

            if (autolog_checkbox.IsChecked.Value)
            {
                AddUpdateAppSettings("autologin", "true");
            }
            else
            {
                AddUpdateAppSettings("autologin", "false");
            }

            login();
        }

        private void hangup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Collections.ObjectModel.ReadOnlyCollection<RasConnection> conList = RasConnection.GetActiveConnections();
                foreach (RasConnection con in conList)
                {
                    con.HangUp();
                }
                System.Threading.Thread.Sleep(1000);
                lb_status.Content = "注销成功";
                lb_message.Content = "已注销";
                process.Kill();
                dial.IsEnabled = true;
                remember_checkbox.IsEnabled = true;
                autolog_checkbox.IsEnabled = true;
                hangup.IsEnabled = false;
            }
            catch (Exception)
            {
                lb_status.Content = "注销出现异常";
            }
        }

        private void login()
        {
            try
            {
                string username = tb_username.Text.Replace("\\r", "\r").Replace("\\n", "\n");
                username = "\r\n" + username;
                string password = pb_password.Password.ToString();
                RasDialer dialer = new RasDialer();
                dialer.EntryName = "gdut drcom";
                dialer.PhoneNumber = " ";
                dialer.AllowUseStoredCredentials = true;
                dialer.PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);
                dialer.Credentials = new System.Net.NetworkCredential(username, password);
                dialer.Timeout = 500;
                RasHandle myras = null;
                if (username.Length != 0 && password.Length != 0)
                {
                    myras = dialer.Dial();
                }
                while (myras.IsInvalid)
                {
                    lb_status.Content = "拨号失败";
                }
                if (!myras.IsInvalid)
                {
                    lb_status.Content = "拨号成功! ";
                    RasConnection conn = RasConnection.GetActiveConnectionByHandle(myras);
                    RasIPInfo ipaddr = (RasIPInfo)conn.GetProjectionInfo(RasProjectionType.IP);
                    lb_message.Content = "获得IP： " + ipaddr.IPAddress.ToString();

                    ProcessStartInfo startinfo = new ProcessStartInfo();
                    //startinfo.RedirectStandardOutput = true;
                    //startinfo.RedirectStandardError = true;
                    startinfo.FileName = ".\\gdut-drcom.exe";
                    startinfo.Arguments = "-c gdut-drcom.conf";
                    startinfo.UseShellExecute = false;
                    startinfo.CreateNoWindow = true;

                    process.StartInfo = startinfo;
                    process.Start();

                    dial.IsEnabled = false;
                    remember_checkbox.IsEnabled = false;
                    autolog_checkbox.IsEnabled = false;
                    hangup.IsEnabled = true;
                }
            }
            catch (Exception)
            {
                lb_status.Content = "拨号出现异常";
            }
        }

        private void FollowMe(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                Console.WriteLine(result);
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return "";
            }
        }


        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); var settings = configFile.AppSettings.Settings; if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else { settings[key].Value = value; }
                configFile.Save(ConfigurationSaveMode.Modified); ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException) { Console.WriteLine("Error writing app settings"); }
        }
    }
}
