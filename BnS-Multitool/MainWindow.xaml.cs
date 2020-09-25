using Hardcodet.Wpf.TaskbarNotification;
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
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Threading;

namespace BnS_Multitool
{
    public partial class MainWindow : Window
    {
        public static BackgroundWorker versionWorker = new BackgroundWorker();
        public static string ONLINE_VERSION = "";
        public static TaskbarIcon taskBar = new TaskbarIcon();
        public static Frame mainWindowFrame;

        public static MainWindow mainWindow { get; private set; }

        public static List<Page_Navigation> navigationPages = new List<Page_Navigation>();
        public static Page currentPage = null;
        public static string currentPageText = "";
        public static List<CHANGELOG_STRUCT> ONLINE_CHANGELOG;
        public static bool isMinimized = false;
        public class ONLINE_VERSION_STRUCT
        {
            public string VERSION { get; set; }
            public List<CHANGELOG_STRUCT> CHANGELOG { get; set; }
        }

        public class CHANGELOG_STRUCT
        {
            public string VERSION { get; set; }
            public string NOTES { get; set; }
        }

        public class Page_Navigation
        {
            public string PageName { get; set; }
            public Page PageSource { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.MouseDown += delegate { try { DragMove(); } catch (Exception) { } };

            if (File.Exists("BnS-Multi-Tool-Updater.exe"))
                File.Delete("BnS-Multi-Tool-Updater.exe");

            if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + @"\bin\"))
            {
                //First check the registry, see if we can find the correct path, if not prompt the user when catching the error.
                try
                {
                    RegistryKey BNS_REGISTRY = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\NCWest\BnS");
                    SystemConfig.SYS.BNS_DIR = BNS_REGISTRY.GetValue("BaseDir").ToString();
                    SystemConfig.appendChangesToConfig();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The path for Blade and Soul is not valid, please set the proper path for your game installation", "INVALID PATH");
                    using (var FOLDER = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        System.Windows.Forms.DialogResult RESULT = FOLDER.ShowDialog();

                        if (RESULT == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                        {
                            //BNS_DIR_TEXTBOX.Text = FOLDER.SelectedPath;
                            SystemConfig.SYS.BNS_DIR = FOLDER.SelectedPath;
                            SystemConfig.appendChangesToConfig();
                        }
                    }
                }
            }
        }

        private void EXIT_BTN_Click(object sender, RoutedEventArgs e)
        {
            taskBar.Dispose();
            this.Close();
        }

        private void setCurrentPage(string pageName)
        {
            var cachedPage = navigationPages.FirstOrDefault(x => x.PageName == pageName);

            if(cachedPage == null)
            {
                switch(pageName)
                {
                    case "MainPage":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new MainPage() });
                        break;
                    case "Launcher":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Launcher() });
                        break;
                    case "Patches":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Patches() });
                        break;
                    case "Effects":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Effects() });
                        break;
                    case "Mods":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Mods() });
                        break;
                    case "Modpolice":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Modpolice() });
                        break;
                }
                cachedPage = navigationPages.FirstOrDefault(x => x.PageName == pageName);
            }

            MainFrame.Content = cachedPage.PageSource;
            currentPageText = pageName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowFrame = MainFrame;
            mainWindow = this;
            //Load our main page.
            setCurrentPage("MainPage");

            VERSION_LABEL.Text = String.Format("BnS Multi Tool Version: {0}", SystemConfig.SYS.VERSION);

            //Launch our version checker worker
            versionWorker.DoWork += new DoWorkEventHandler(checkOnlineVersion);
            versionWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(versionCheckFinished);
            versionWorker.RunWorkerAsync();

            //Construct our taskbar icon
            taskBar.Icon = Properties.Resources.AppIcon;
            taskBar.ToolTipText = "BnS Multi Tool";
            taskBar.TrayMouseDoubleClick += new RoutedEventHandler(OnNotifyDoubleClick);
            taskBar.Visibility = Visibility.Collapsed;
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private static void checkOnlineVersion(object sender, DoWorkEventArgs e)
        {
            Debug.Write("Retrieving online version \n");
            WebClient client = new WebClient();
            try
            {
                var json = client.DownloadString("http://tonic.pw/files/bnsmultitool/version.json");
                var dummyData = JsonConvert.DeserializeObject<ONLINE_VERSION_STRUCT>(json);
                ONLINE_VERSION = dummyData.VERSION;
                ONLINE_CHANGELOG = dummyData.CHANGELOG;

            } catch (WebException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                client.Dispose();
            }
        }

        private void versionCheckFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine(String.Format("Online Version: {0} \n", ONLINE_VERSION));

            if (ONLINE_VERSION == "")
            {
                var dialog = new ErrorPrompt("Could not fetch online version..?");
                dialog.ShowDialog();
                return;
            }

            if (ONLINE_VERSION == SystemConfig.SYS.VERSION)
                MultiTool_UPDATE.Visibility = Visibility.Hidden;
            else
                MultiTool_UPDATE.Visibility = Visibility.Visible;
        }

        private void OnNotifyDoubleClick(object sender, RoutedEventArgs e)
        {
            changeWindowState(true);
        }

        private void MAIN_CLICK(object sender, RoutedEventArgs e)
        {
            setCurrentPage("MainPage");
        }

        private void LAUNCHER_CLICK(object sender, RoutedEventArgs e)
        {
            setCurrentPage("Launcher");
        }

        public static void changeWindowState(bool state)
        {
            mainWindow.Dispatcher.Invoke(new Action(() => {
                if(state)
                {
                    mainWindow.ShowInTaskbar = true;
                    mainWindow.Visibility = Visibility.Visible;
                    isMinimized = false;
                    Thread.Sleep(150);
                    taskBar.Visibility = Visibility.Collapsed;
                } else
                {
                    taskBar.Visibility = Visibility.Visible;
                    Application.Current.MainWindow.ShowInTaskbar = false;
                    Application.Current.MainWindow.Visibility = Visibility.Hidden;
                    isMinimized = true;
                }
            }));
        }

        private void UPDATE_BTN_CLICK(object sender, RoutedEventArgs e)
        {
            //Updater code
            File.WriteAllBytes(Environment.CurrentDirectory + @"\BnS-Multi-Tool-Updater.exe", Properties.Resources.BnS_Multi_Tool_Updater);

            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = "BnS-Multi-Tool-Updater.exe";
            proc.Verb = "runas";
            Process.Start(proc);
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            setCurrentPage("Patches");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            setCurrentPage("Effects");
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            changeWindowState(false);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            setCurrentPage("Mods");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            setCurrentPage("Modpolice");
        }
    }
}
