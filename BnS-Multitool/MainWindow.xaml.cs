using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Reflection;
using System.Security.Principal;

namespace BnS_Multitool
{
    public partial class MainWindow : Window
    {
        public static TaskbarIcon taskBar = new TaskbarIcon();
        public static Frame mainWindowFrame;
        public static Button UpdateButtonObj;
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
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            bool runAsAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);

            if(!runAsAdmin)
            {
                var processInfo =new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase);

                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";

                // Start the new process
                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception)
                {
                    var dialog = new ErrorPrompt("Failed to auto start as admin, please launch with administrator rights.");
                    dialog.ShowDialog();
                }

                // Shut down the current process
                Environment.Exit(0);
            }
            
            InitializeComponent();
            UpdateButtonObj = MultiTool_UPDATE;

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

                    if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\"))
                        throw new Exception("Directory doesn't eixst");
                }
                catch (Exception)
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
            GC.WaitForPendingFinalizers();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowFrame = MainFrame;
            mainWindow = this;
            //Load our main page.
            setCurrentPage("MainPage");

            VERSION_LABEL.Text = String.Format("BnS Multi Tool Version: {0}", FileVersion());

            //Check version in settings.json and overwrite if they don't match, means they probably updated manually...
            if(SystemConfig.SYS.VERSION != FileVersion())
            {
                SystemConfig.SYS.VERSION = FileVersion();
                SystemConfig.appendChangesToConfig();
            }

            //Construct our taskbar icon
            taskBar.Icon = Properties.Resources.AppIcon;
            taskBar.ToolTipText = "BnS Multi Tool";
            taskBar.TrayMouseDoubleClick += new RoutedEventHandler(OnNotifyDoubleClick);
            taskBar.Visibility = Visibility.Hidden;
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        public static string FileVersion()
        {
            return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        }

        private void OnNotifyDoubleClick(object sender, RoutedEventArgs e)
        {
            changeWindowState(true);
        }

        private void MAIN_CLICK(object sender, RoutedEventArgs e)
        {
            //PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            setCurrentPage("MainPage");
        }

        private void LAUNCHER_CLICK(object sender, RoutedEventArgs e)
        {
            PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
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
                    taskBar.Visibility = Visibility.Hidden;
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
            //PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            setCurrentPage("Patches");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
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
            //PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            setCurrentPage("Mods");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            //PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            setCurrentPage("Modpolice");
        }
    }
}
