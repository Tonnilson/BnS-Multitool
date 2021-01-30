using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
using Newtonsoft.Json;
using System.IO;
using System.CodeDom;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using MiscUtil.Compression.Vcdiff;

namespace BnS_Multi_Tool_Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class WEB_VERSION_CLASS
        {
            public string VERSION { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public string HASH { get; set; }
        }

        public struct SYSConfig
        {
            public string VERSION { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public string BNS_DIR { get; set; }
            public string UPK_DIR { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public List<BNS_CLASS_STRUCT> CLASSES { get; set; }
        }

        public class BNS_CLASS_STRUCT
        {
            public string CLASS { get; set; }
            public string[] EFFECTS { get; set; }
            public string[] ANIMATIONS { get; set; }
        }

        private static WEB_VERSION_CLASS onlineJson;

        public MainWindow()
        {
            InitializeComponent();
            this.MouseDown += delegate { try { DragMove(); } catch (Exception) { } };

            //Find all instances of BnS-Multi-Tool and close.
            try
            {
                Process[] processes = Process.GetProcessesByName("BnS-Multi-Tool");

                foreach (Process proc in processes)
                    proc.Kill();
            }
            catch (Exception) { }

            try
            {
                string settingsText = File.ReadAllText("settings.json");
                var SYS = JsonConvert.DeserializeObject<SYSConfig>(settingsText);

                LocalVersion.Content = "Local: " + SYS.VERSION;

                WebClient client = new WebClient();
                var json = client.DownloadString("http://tonic.pw/files/bnsmultitool/version.json");
                onlineJson = JsonConvert.DeserializeObject<WEB_VERSION_CLASS>(json);
            }
            catch (Exception) { }

            if (onlineJson.VERSION == "")
            {
                OnlineVersion.Content = "Online: OFFLINE";
                downloadBtn.Visibility = Visibility.Hidden;
            }
            else
                OnlineVersion.Content = "Online: " + onlineJson.VERSION;
        }

        TaskCompletionSource<bool> downloadComplete = new TaskCompletionSource<bool>();
        private async void downloadClick(object sender, RoutedEventArgs e)
        {
            while (Process.GetProcessesByName("BnS-Multi-Tool").Length >= 1)
            {
                //Find all instances of BnS-Multi-Tool and close.
                try
                {
                    Process[] processes = Process.GetProcessesByName("BnS-Multi-Tool");

                    foreach (Process proc in processes)
                        proc.Kill();

                    Thread.Sleep(50);
                }
                catch (Exception) { }
            }

            downloadComplete = new TaskCompletionSource<bool>();

            downloadBtn.Visibility = Visibility.Hidden;
            VersionGrid.Visibility = Visibility.Hidden;
            ProgressControl.Value = 0;
            ProgressText.Text = "0%";
            downloadingLbl.Content = "Downloading...";
            ProgressGrid.Visibility = Visibility.Visible;
            bool failedToUpdate = false;

            await Task.Run(async () =>
            {
                try
                {
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DL_PROGRESS_CHANGED);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DL_COMPLETED);
                    client.DownloadFileAsync(new Uri("http://tonic.pw/files/bnsmultitool/BnS-Multi-Tool.exe.dlt"), "BnS-Multi-Tool.exe.dlt");

                    await downloadComplete.Task;

                    await downloadingLbl.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        downloadingLbl.Content = "Patching....";
                    }));

                    using (FileStream original = File.OpenRead("BnS-Multi-Tool.exe"))
                    using (FileStream patch = File.OpenRead("BnS-Multi-Tool.exe.dlt"))
                    using (FileStream target = File.Open("BnS-Multi-Tool-New.exe", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        VcdiffDecoder.Decode(original, patch, target);
                    }

                    await Task.Delay(1000);

                    //Hash check
                    string localHash = CalculateMD5("BnS-Multi-Tool-New.exe");
                    await downloadingLbl.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        downloadingLbl.Content = "Verifying....";
                    }));

                    await Task.Delay(1000);
                    if (localHash == onlineJson.HASH)
                    {
                        string jsonString = File.ReadAllText("settings.json");
                        var SYS = JsonConvert.DeserializeObject<SYSConfig>(jsonString);

                        SYS.VERSION = onlineJson.VERSION;

                        //Append new UPKs to our main list
                        if (onlineJson.MAIN_UPKS.Count() > 0)
                        {
                            List<string> _MAIN_UPKS = SYS.MAIN_UPKS.ToList();
                            foreach (string UPK in onlineJson.MAIN_UPKS)
                            {
                                if (!SYS.MAIN_UPKS.Contains(UPK))
                                    _MAIN_UPKS.Add(UPK);
                            }

                            SYS.MAIN_UPKS = _MAIN_UPKS.ToArray();
                        }

                        jsonString = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                        File.WriteAllText("settings.json", jsonString);
                        await downloadingLbl.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            downloadingLbl.Content = "Launching....";
                        }));

                        await Task.Delay(500);

                        if (File.Exists("BnS-Multi-Tool.exe.dlt"))
                            File.Delete("BnS-Multi-Tool.exe.dlt");

                        if (File.Exists("BnS-Multi-Tool.exe"))
                            File.Delete("BnS-Multi-Tool.exe");

                        File.Move("BnS-Multi-Tool-New.exe", "BnS-Multi-Tool.exe");

                        ProcessStartInfo proc = new ProcessStartInfo();
                        proc.Verb = "runas";
                        proc.FileName = "BnS-Multi-Tool.exe";
                        Process.Start(proc);
                        Environment.Exit(0);
                    }
                    else
                        failedToUpdate = true;

                } catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    failedToUpdate = true;
                }
            });

            //This is redundant but just in case
            if(failedToUpdate)
            {
                ProgressText.Text = "FAILED";
                downloadBtn.Visibility = Visibility.Visible;
                //VersionGrid.Visibility = Visibility.Visible;
                //ProgressGrid.Visibility = Visibility.Hidden;
                ProgressControl.Value = 0;
                downloadingLbl.Content = "Incorrect Hash";
                MessageBox.Show("Are you sure the multi tool is closed and an anti-virus is not blocking the download? You can try again", "Failed to update");
                downloadFull.Visibility = Visibility.Visible;
                downloadBtn.Visibility = Visibility.Hidden;
            }
        }

        private void DL_PROGRESS_CHANGED(object sender, DownloadProgressChangedEventArgs e)
        {
            this.ProgressControl.Dispatcher.Invoke(new Action(() =>
            {
                ProgressText.Text = String.Format("{0}%", e.ProgressPercentage);
                ProgressControl.Value = e.ProgressPercentage;
            }));
        }

        private void DL_COMPLETED(object sender, AsyncCompletedEventArgs e)
        {
            downloadComplete.SetResult(true);
        }

        static string CalculateMD5(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void close(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private async void downloadFullClick(object sender, RoutedEventArgs e)
        {
            downloadComplete = new TaskCompletionSource<bool>();

            downloadBtn.Visibility = Visibility.Hidden;
            VersionGrid.Visibility = Visibility.Hidden;
            ProgressControl.Value = 0;
            ProgressText.Text = "0%";
            downloadingLbl.Content = "Downloading...";
            ProgressGrid.Visibility = Visibility.Visible;
            bool failedToUpdate = false;

            await Task.Run(async () =>
            {
                try
                {
                    WebClient client = new WebClient();
                    if (File.Exists("BnS-Multi-Tool-New.exe"))
                        File.Delete("BnS-Multi-Tool-New.exe");

                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DL_PROGRESS_CHANGED);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DL_COMPLETED);
                    client.DownloadFileAsync(new Uri("http://tonic.pw/files/bnsmultitool/BnS-Multi-Tool.exe"), "BnS-Multi-Tool-New.exe");

                    await downloadComplete.Task;
                    await Task.Delay(1000);

                    //Hash check
                    string localHash = CalculateMD5("BnS-Multi-Tool-New.exe");
                    await downloadingLbl.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        downloadingLbl.Content = "Verifying....";
                    }));

                    await Task.Delay(1000);
                    if (localHash == onlineJson.HASH)
                    {
                        string jsonString = File.ReadAllText("settings.json");
                        var SYS = JsonConvert.DeserializeObject<SYSConfig>(jsonString);

                        SYS.VERSION = onlineJson.VERSION;

                        //Append new UPKs to our main list
                        if (onlineJson.MAIN_UPKS.Count() > 0)
                        {
                            List<string> _MAIN_UPKS = SYS.MAIN_UPKS.ToList();
                            foreach (string UPK in onlineJson.MAIN_UPKS)
                            {
                                if (!SYS.MAIN_UPKS.Contains(UPK))
                                    _MAIN_UPKS.Add(UPK);
                            }

                            SYS.MAIN_UPKS = _MAIN_UPKS.ToArray();
                        }

                        jsonString = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                        File.WriteAllText("settings.json", jsonString);
                        await downloadingLbl.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            downloadingLbl.Content = "Launching....";
                        }));

                        await Task.Delay(500);

                        if (File.Exists("BnS-Multi-Tool.exe"))
                            File.Delete("BnS-Multi-Tool.exe");

                        File.Move("BnS-Multi-Tool-New.exe", "BnS-Multi-Tool.exe");

                        ProcessStartInfo proc = new ProcessStartInfo();
                        proc.Verb = "runas";
                        proc.FileName = "BnS-Multi-Tool.exe";
                        Process.Start(proc);
                        Environment.Exit(0);
                    }
                    else
                        failedToUpdate = true;

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    failedToUpdate = true;
                }
            });

            //This is redundant but just in case
            if (failedToUpdate)
            {
                ProgressText.Text = "FAILED";
                downloadBtn.Visibility = Visibility.Visible;
                //VersionGrid.Visibility = Visibility.Visible;
                //ProgressGrid.Visibility = Visibility.Hidden;
                ProgressControl.Value = 0;
                downloadingLbl.Content = "Incorrect Hash";
                MessageBox.Show("Are you sure the multi tool is closed and an anti-virus is not blocking the download? You can try again", "Failed to update");
                downloadFull.Visibility = Visibility.Visible;
                downloadBtn.Visibility = Visibility.Hidden;
            }
        }
    }
}
