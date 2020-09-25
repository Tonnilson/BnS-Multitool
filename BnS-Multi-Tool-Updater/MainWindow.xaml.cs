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
            public string[] ADDITIONAL_UPKS { get; set; }
        }

        public struct SYSConfig
        {
            public string VERSION { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public string BNS_DIR { get; set; }
            public string UPK_DIR { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public string[] ADDITIONAL_UPKS { get; set; }
            public List<ANIMATION_UPKS_STRUCT> ANIMATION_UPKS { get; set; }
        }

        public class ANIMATION_UPKS_STRUCT
        {
            public string CLASS { get; set; }
            public string[] UPK_FILES { get; set; }
        }

        private static string WEB_VERSION = "";
        private static string[] MAIN_UPKS = new string[] { };
        private static string[] ADDITIONAL_UPKS = new string[] { };

        public MainWindow()
        {
            InitializeComponent();
            this.MouseDown += delegate { try { DragMove(); } catch (Exception ex) { } };

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
                var tmp = JsonConvert.DeserializeObject<WEB_VERSION_CLASS>(json);
                WEB_VERSION = tmp.VERSION;
                MAIN_UPKS = tmp.MAIN_UPKS;
                ADDITIONAL_UPKS = tmp.ADDITIONAL_UPKS;
            }
            catch (Exception ex) { }

            if (WEB_VERSION == "")
            {
                OnlineVersion.Content = "Online: OFFLINE";
                downloadBtn.Visibility = Visibility.Hidden;
            }
            else
                OnlineVersion.Content = "Online: " + WEB_VERSION;
        }

        TaskCompletionSource<bool> downloadComplete = new TaskCompletionSource<bool>();
        private async void downloadClick(object sender, RoutedEventArgs e)
        {
            downloadBtn.Visibility = Visibility.Hidden;
            VersionGrid.Visibility = Visibility.Hidden;
            ProgressControl.Value = 0;
            ProgressText.Text = "0%";
            ProgressGrid.Visibility = Visibility.Visible;

            await Task.Run(async () =>
            {
                try
                {
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DL_PROGRESS_CHANGED);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DL_COMPLETED);
                    client.DownloadFileAsync(new Uri("http://tonic.pw/files/bnsmultitool/BnS-Multi-Tool.exe"), "BnS-Multi-Tool.exe");

                    await downloadComplete.Task;

                    string jsonString = File.ReadAllText("settings.json");
                    var SYS = JsonConvert.DeserializeObject<SYSConfig>(jsonString);

                    SYS.VERSION = WEB_VERSION;

                    //Append new UPKs to our main list
                    if (MAIN_UPKS.Count() > 0)
                    {
                        List<string> _MAIN_UPKS = SYS.MAIN_UPKS.ToList();
                        foreach (string UPK in MAIN_UPKS)
                        {
                            if (!SYS.MAIN_UPKS.Contains(UPK))
                                _MAIN_UPKS.Add(UPK);
                        }

                        SYS.MAIN_UPKS = _MAIN_UPKS.ToArray();
                    }

                    //Append new UPKs to our additional list
                    if (ADDITIONAL_UPKS.Count() > 0)
                    {
                        List<string> _ADDITIONAL_UPKS = SYS.ADDITIONAL_UPKS.ToList();
                        foreach (string UPK in ADDITIONAL_UPKS)
                        {
                            if (!SYS.ADDITIONAL_UPKS.Contains(UPK))
                                _ADDITIONAL_UPKS.Add(UPK);
                        }
                        SYS.ADDITIONAL_UPKS = _ADDITIONAL_UPKS.ToArray();
                    }

                    jsonString = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                    File.WriteAllText("settings.json", jsonString);
                    Thread.Sleep(1200);

                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.Verb = "runas";
                    proc.FileName = "BnS-Multi-Tool.exe";
                    Process.Start(proc);

                    Environment.Exit(0);

                } catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
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
    }
}
