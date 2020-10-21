using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private static BackgroundWorker pingWorker = new BackgroundWorker();
        private static int currentPing = -1;
        private static string loginServer = "updater.nclauncher.ncsoft.com";
        private static DispatcherTimer onlineUsersTimer = new DispatcherTimer();
        private static MainWindow.ONLINE_VERSION_STRUCT onlineJson;

        public MainPage()
        {
            InitializeComponent();

            pingWorker.WorkerSupportsCancellation = true;
            pingWorker.WorkerReportsProgress = true;
            pingWorker.DoWork += new DoWorkEventHandler(monitorPing);
            pingWorker.ProgressChanged += new ProgressChangedEventHandler(pingProgress);

            //Tick Timer for getting Online Users
            onlineUsersTimer.IsEnabled = true;
            onlineUsersTimer.Tick += new EventHandler(onlineUsers_Tick);
            onlineUsersTimer.Interval = TimeSpan.FromMinutes(10);
            onlineUsersTimer.Start();

            //MainWindow.versionWorker.RunWorkerAsync();
        }

        private static void checkForUpdate()
        {
            WebClient client = new WebClient();
            try
            {
                var json = client.DownloadString("http://tonic.pw/files/bnsmultitool/version.json");
                onlineJson = JsonConvert.DeserializeObject<MainWindow.ONLINE_VERSION_STRUCT>(json);

                if (onlineJson.VERSION != MainWindow.FileVersion())
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate
                    {
                        var dialog = new ErrorPrompt("Update available, please be sure to read the change log for any critical changes.\r\rOnline Version: " + onlineJson.VERSION + "\rLocal: " + SystemConfig.SYS.VERSION, true);
                        dialog.Owner = MainWindow.mainWindow;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        dialog.ShowDialog();
                    });

                    SystemConfig.SYS.VERSION = MainWindow.FileVersion();
                    SystemConfig.appendChangesToConfig();

                    MainWindow.UpdateButtonObj.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainWindow.UpdateButtonObj.Visibility = Visibility.Visible;
                    }));
                }

            } catch (WebException)
            {

            }
            finally
            {
                client.Dispose();
            }
        }

        private void onlineUsers_Tick(object sender, EventArgs e)
        {
            WebClient client = new WebClient();
            int usersOnline = 0;
            try
            {
                string stringnumber = client.DownloadString("http://tonic.pw/files/bnsmultitool/usersOnline.php");
                if (!(int.TryParse(stringnumber, out usersOnline)))
                    usersOnline = 0;

            }
            catch (WebException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                client.Dispose();
            }

            Dispatchers.labelContent(usersOnlineLbl, String.Format("Users Online: {0}", (usersOnline != 0) ? usersOnline.ToString() : "Error"));
        }

        private void monitorPing(object sender, DoWorkEventArgs e)
        {
            string regionIP;
            switch(ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case 1:
                    regionIP = "18.194.180.254";
                    break;
                case 2:
                    regionIP = "203.67.68.227";
                    break;
                default:
                    regionIP = "184.73.104.101";
                    break;
            }

            while (!pingWorker.CancellationPending && MainWindow.currentPageText == "MainPage")
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Blocking = true;
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    socket.Connect(regionIP,10100);
                    stopwatch.Stop();

                    currentPing = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
                    socket.Close();

                }
                catch (SocketException)
                {
                    currentPing = -1;
                }

                pingWorker.ReportProgress(0);
                Thread.Sleep(1000);
            }
            Debug.WriteLine("Stopping worker");
        }

        private void pingProgress(object sender, ProgressChangedEventArgs e)
        {
            string regionID;
            switch (ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case 1:
                    regionID = "EU";
                    break;
                case 2:
                    regionID = "TW";
                    break;
                default:
                    regionID = "NA";
                    break;
            }

            if (currentPing != -1)
                PingLabel.Content = String.Format("Ping ({0}): {1}ms", regionID, currentPing);
            else
                PingLabel.Content = "Ping: Offline";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=DZ8KL2ZDS44JC&source=url");
        }

        /*
        public bool isLoginAvailable ()
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)4);
                bw.Write((byte)10);
                bw.Write((byte)"BnS".Length);
                bw.Write(Encoding.ASCII.GetBytes("BnS"));
                bw.BaseStream.Position = 0L;
                bw.Write((short)ms.Length);

                ns.Write(ms.ToArray(), 0, (int)ms.Length);
                bw.Dispose();
                ms.Dispose();

                ms = new MemoryStream();
                BinaryReader br = new BinaryReader(ms);

                byte[] byte_array = new byte[1024];
                int num = 0;

                do
                {
                    num = ns.Read(byte_array, 0, byte_array.Length);
                    if (num > 0)
                        ms.Write(byte_array, 0, num);
                }
                while (num == byte_array.Length);

                ms.Position = 9L;
                br.ReadBytes(br.ReadByte() + 1);
                return br.ReadBoolean();


            } catch (Exception ex)
            {
                return false;
            }
        }
        */

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(!pingWorker.IsBusy)
                pingWorker.RunWorkerAsync();

            ChangeLog.Document.Blocks.Clear();
            await Task.Run(() =>
            {
                checkForUpdate();

                while (onlineJson.CHANGELOG == null) { Thread.Sleep(50); }

                foreach (var version in onlineJson.CHANGELOG)
                    appendToChangelog(String.Format("Version: {0}\r{1}\r\r", version.VERSION, version.NOTES));
            });
        }

        private void appendToChangelog(string msg)
        {
            this.ChangeLog.Dispatcher.Invoke(new Action(() =>
            {
                ChangeLog.AppendText(msg);
            }));
        }
    }
}