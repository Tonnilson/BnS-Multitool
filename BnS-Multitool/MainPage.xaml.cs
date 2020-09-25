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

        public MainPage()
        {
            InitializeComponent();

            pingWorker.WorkerSupportsCancellation = true;
            pingWorker.WorkerReportsProgress = true;
            pingWorker.DoWork += new DoWorkEventHandler(monitorPing);
            pingWorker.ProgressChanged += new ProgressChangedEventHandler(pingProgress);
        }

        private void monitorPing(object sender, DoWorkEventArgs e)
        {
            string regionIP = (ACCOUNT_CONFIG.ACCOUNTS.REGION == 0) ? "184.73.104.101" : "18.194.180.254";

            while (!pingWorker.CancellationPending && MainWindow.currentPageText == "MainPage")
            {
                regionIP = (ACCOUNT_CONFIG.ACCOUNTS.REGION == 0) ? "184.73.104.101" : "18.194.180.254";
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
            if (currentPing != -1)
                PingLabel.Content = String.Format("Ping ({0}): {1}ms", (ACCOUNT_CONFIG.ACCOUNTS.REGION == 0) ? "NA" : "EU", currentPing);
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
                while (MainWindow.ONLINE_CHANGELOG == null) { Thread.Sleep(50); }

                foreach (var version in MainWindow.ONLINE_CHANGELOG)
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