﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        //private static string loginServer = "updater.nclauncher.ncsoft.com";
        private static DispatcherTimer onlineUsersTimer = new DispatcherTimer();
        public static MainWindow.ONLINE_VERSION_STRUCT onlineJson;

        public MainPage()
        {
            InitializeComponent();

            pingWorker.WorkerSupportsCancellation = true;
            pingWorker.WorkerReportsProgress = true;
            pingWorker.DoWork += new DoWorkEventHandler(monitorPing);
            pingWorker.ProgressChanged += new ProgressChangedEventHandler(pingProgress);

            //Tick Timer for getting Online Users
            onlineUsersTimer.Tick += new EventHandler(onlineUsers_Tick);
            onlineUsersTimer.Interval = TimeSpan.FromMinutes(10);
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
                        dialog.ShowDialog();
                    });

                    SystemConfig.SYS.VERSION = MainWindow.FileVersion();
                    SystemConfig.appendChangesToConfig();

                    Dispatchers.buttonVisibility(MainWindow.UpdateButtonObj, Visibility.Visible);
                }

            } catch (WebException ex)
            {
                onlineJson = new MainWindow.ONLINE_VERSION_STRUCT();
                onlineJson.CHANGELOG = new List<MainWindow.CHANGELOG_STRUCT>();
                onlineJson.CHANGELOG.Add(new MainWindow.CHANGELOG_STRUCT() { VERSION = "ERROR", NOTES =  ex.Message});
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
            Debug.WriteLine("Getting Tick");
            try
            {
                string stringnumber = client.DownloadString(String.Format("http://tonic.pw/files/bnsmultitool/usersOnline.php?UID={0}", SystemConfig.SYS.FINGERPRINT));
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
            switch((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case Globals.BnS_Region.EU:
                    regionIP = "18.194.180.254";
                    break;
                case Globals.BnS_Region.TW:
                    regionIP = "203.67.68.227";
                    break;
                case Globals.BnS_Region.KR:
                    regionIP = "222.122.231.3";
                    break;
                default:
                    regionIP = "184.73.104.101";
                    break;
            }

            while (!pingWorker.CancellationPending && MainWindow.currentPageText == "MainPage")
            {
                if (SystemConfig.SYS.PING_CHECK == 1)
                {
                    try
                    {
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.Blocking = true;
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();

                        socket.Connect(regionIP, 10100);
                        stopwatch.Stop();

                        currentPing = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
                        socket.Close();

                    }
                    catch (SocketException)
                    {
                        currentPing = -1;
                    }
                }
                else
                    currentPing = -2;

                pingWorker.ReportProgress(0);
                Thread.Sleep(1000);
            }
            Debug.WriteLine("Stopping worker");
        }

        private void pingProgress(object sender, ProgressChangedEventArgs e)
        {
            string regionID;
            switch ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case Globals.BnS_Region.EU:
                    regionID = "EU";
                    break;
                case Globals.BnS_Region.TW:
                    regionID = "TW";
                    break;
                case Globals.BnS_Region.KR:
                    regionID = "KR";
                    break;
                default:
                    regionID = "NA";
                    break;
            }

            if (currentPing == -2)
                PingLabel.Content = "Ping: Turned Off";
            else if (currentPing != -1)
                PingLabel.Content = String.Format("Ping ({0}): {1}ms", regionID, currentPing);
            else
                PingLabel.Content = "Ping: Offline";
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=DZ8KL2ZDS44JC&source=url");

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pingWorker.IsBusy && SystemConfig.SYS.PING_CHECK == 1)
                pingWorker.RunWorkerAsync();
            else
                PingLabel.Content = "Ping: Turned Off";

            ChangeLog.Document.Blocks.Clear();
            await Task.Run(async () =>
            {
                checkForUpdate();

                while (onlineJson.CHANGELOG == null) { await Task.Delay(50); }
                
                foreach (var version in onlineJson.CHANGELOG)
                    appendToChangelog(string.Format("Version: {0}\r{1}\r\r", version.VERSION, version.NOTES));

                if (!onlineUsersTimer.IsEnabled)
                {
                    /*
                     * Generate a unique 'Fingerprint' for our user based off hardware
                     * Used for creating a unique total user count
                     * Store the unique ID in settings to speed up the process on next boot
                     */
                    if (SystemConfig.SYS.FINGERPRINT == null)
                    {
                        SystemConfig.SYS.FINGERPRINT = Security.FingerPrint.Value();
                        SystemConfig.appendChangesToConfig();
                    }

                    onlineUsersTimer.IsEnabled = true;
                    onlineUsers_Tick(sender, new EventArgs());
                }
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