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
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;

namespace BnS_Multitool
{
    public partial class MainWindow : Window
    {
        public static TaskbarIcon taskBar = new TaskbarIcon();
        public static string Fingerprint;
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
            public List<string> QOL_HASH { get; set; }
            public string QOL_ARCHIVE { get; set; }
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

        public static XDocument qol_xml;

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

            string CultureName = Thread.CurrentThread.CurrentCulture.Name;
            CultureInfo ci = new CultureInfo(CultureName);
            if (ci.NumberFormat.NumberDecimalSeparator != ".")
            {
                // Forcing use of decimal separator for numerical values
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
            }

            //Construct our taskbar icon
            taskBar.Icon = Properties.Resources.AppIcon;
            taskBar.ToolTipText = "BnS Multi Tool";
            taskBar.TrayMouseDoubleClick += new RoutedEventHandler(OnNotifyDoubleClick);
            taskBar.Visibility = Visibility.Hidden;

            UpdateButtonObj = MultiTool_UPDATE;

            this.MouseDown += delegate { try { DragMove(); } catch (Exception) { } };

            try
            {
                const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if ((int)ndpKey.GetValue("Release") < 461808)
                    {
                        var dialog = new ErrorPrompt("It seems you do not have .NET Framework 4.7.2 or higher, this is required for certain features to work properly.\nPlease install it and try launching again.");
                        dialog.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                {
                    using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                        output.Write(Properties.Resources.multitool_qol);
                }

                qol_xml = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

                ToolTipService.ShowDurationProperty.OverrideMetadata(
                    typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            }
            catch (Exception) { }

            if (File.Exists("BnS-Multi-Tool-Updater.exe"))
                File.Delete("BnS-Multi-Tool-Updater.exe");

            if (String.IsNullOrEmpty(SystemConfig.SYS.BNS_DIR) || !Directory.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR,"bin")))
            {
                //First check the registry, see if we can find the correct path, if not prompt the user when catching the error.
                try
                {
                    RegistryKey BNS_REGISTRY = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\NCWest\BnS");

                    if (BNS_REGISTRY != null)
                    {
                        SystemConfig.SYS.BNS_DIR = BNS_REGISTRY.GetValue("BaseDir").ToString();
                        //This is a slight correction for some systems, for whatever reason they are not registering the path correctly so I have to force this backslash onto it.
                        if (SystemConfig.SYS.BNS_DIR.Last() != '\\')
                            SystemConfig.SYS.BNS_DIR += "\\";
                    } else
                    {
                        BNS_REGISTRY = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\plaync\BNS_KOR");

                        if (BNS_REGISTRY == null)
                            throw new Exception("No registry entries for client");

                        SystemConfig.SYS.BNS_DIR = BNS_REGISTRY.GetValue("BaseDir").ToString();
                        if (SystemConfig.SYS.BNS_DIR.Last() != '\\')
                            SystemConfig.SYS.BNS_DIR += "\\";

                        ACCOUNT_CONFIG.ACCOUNTS.REGION = (int)Globals.BnS_Region.KR;
                        ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE = 5;
                    }

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
                            SystemConfig.SYS.BNS_DIR = FOLDER.SelectedPath + "\\";
                            SystemConfig.appendChangesToConfig();
                        }
                    }
                }
            }

            lstBoxUpdaterThreads.SelectedIndex = SystemConfig.SYS.UPDATER_THREADS;
            lstBoxNewGame.SelectedIndex = SystemConfig.SYS.NEW_GAME_OPTION;
            lstBoxLauncherX.SelectedIndex = SystemConfig.SYS.MINIMZE_ACTION;
            DeltaPatching_Checkbox.IsChecked = (SystemConfig.SYS.DELTA_PATCHING == 1) ? true : false;
            PingCheckTick.IsChecked = (SystemConfig.SYS.PING_CHECK == 1) ? true : false;
            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;

            //Cheap lazy fix because I'm done really doing anything with this.
            if(SystemConfig.SYS.patch64 == 0)
            {
                SystemConfig.SYS.patch64 = 1;
                SystemConfig.SYS.patch32 = 1;
                SystemConfig.appendChangesToConfig();
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
                    case "Gameupdater":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new GameUpdater() });
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

           // await Task.Run(() => { });
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
            changeWindowState(true,true);
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

        public static void changeWindowState(bool state, bool overwrite = false)
        {
            if (SystemConfig.SYS.MINIMZE_ACTION == 0 && !overwrite)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
            else
            {
                mainWindow.Dispatcher.Invoke(new Action(() =>
                {
                    if (state)
                    {
                        mainWindow.ShowInTaskbar = true;
                        mainWindow.Visibility = Visibility.Visible;
                        isMinimized = false;
                        Thread.Sleep(150);
                        taskBar.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        taskBar.Visibility = Visibility.Visible;
                        Application.Current.MainWindow.ShowInTaskbar = false;
                        Application.Current.MainWindow.Visibility = Visibility.Hidden;
                        isMinimized = true;
                    }
                }));
            }
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

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDarken.Visibility = Visibility.Visible;
            SettingsMenu.Visibility = Visibility.Visible;
        }

        public void Settings_Closed() => SettingsDarken.Dispatcher.BeginInvoke(new Action(() => { SettingsDarken.Visibility = Visibility.Hidden; SettingsMenu.Visibility = Visibility.Hidden; }));

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            SystemConfig.SYS.UPDATER_THREADS = lstBoxUpdaterThreads.SelectedIndex;
            SystemConfig.SYS.NEW_GAME_OPTION = lstBoxNewGame.SelectedIndex;
            SystemConfig.SYS.MINIMZE_ACTION = lstBoxLauncherX.SelectedIndex;
            SystemConfig.SYS.DELTA_PATCHING = ((bool)DeltaPatching_Checkbox.IsChecked) ? 1 : 0;
            SystemConfig.SYS.PING_CHECK = ((bool)PingCheckTick.IsChecked) ? 1 : 0;
            SystemConfig.SYS.BNS_DIR = BNS_LOCATION_BOX.Text;

            SystemConfig.appendChangesToConfig();
            Settings_Closed();
        }

        private void SettingsCancel(object sender, RoutedEventArgs e)
        {
            lstBoxUpdaterThreads.SelectedIndex = SystemConfig.SYS.UPDATER_THREADS;
            lstBoxNewGame.SelectedIndex = SystemConfig.SYS.NEW_GAME_OPTION;
            lstBoxLauncherX.SelectedIndex = SystemConfig.SYS.MINIMZE_ACTION;
            DeltaPatching_Checkbox.IsChecked = (SystemConfig.SYS.DELTA_PATCHING == 1) ? true : false;
            PingCheckTick.IsChecked = (SystemConfig.SYS.PING_CHECK == 1) ? true : false;
            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;

            Settings_Closed();
        }

        private void GameUpdaterButton(object sender, RoutedEventArgs e)
        {
            if((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW)
            {
                var dialog = new ErrorPrompt("This feature is only available for NA, EU and KR region");
                dialog.ShowDialog();
            }
            else
                setCurrentPage("Gameupdater");
        }

        private void SettingsBrowse(object sender, RoutedEventArgs e)
        {
            using (var FOLDER = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                    BNS_LOCATION_BOX.Text = FOLDER.SelectedPath + ((FOLDER.SelectedPath.Last() != '\\') ? "\\" : "");
            }
        }

        private void CompatibilityOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get rid of compatibility options on Client.exe for both bin & bin64
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                var keys = key.GetValueNames().Where(x => x.Contains(@"bin\Client.exe") || x.Contains(@"bin64\Client.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

                key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                keys = key.GetValueNames().Where(x => x.Contains(@"bin\Client.exe") || x.Contains(@"bin64\Client.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
