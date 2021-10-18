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
        public static string Fingerprint;
        public static Frame mainWindowFrame;
        public static Button UpdateButtonObj;
        public static MainWindow mainWindow { get; private set; }
        public static List<Page_Navigation> navigationPages = new List<Page_Navigation>();
        public static Page currentPage = null;
        public static string currentPageText = "";
        public static List<CHANGELOG_STRUCT> ONLINE_CHANGELOG;
        public static bool isMinimized = false;
        public static System.Windows.Forms.NotifyIcon notifyIcon;
        public class ONLINE_VERSION_STRUCT
        {
            public string VERSION { get; set; }
            public string QOL_HASH { get; set; }
            public string QOL_ARCHIVE { get; set; }
            public int ANTI_CHEAT_ENABLED { get; set; }
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
            //Janky check to make sure the application is running as Administrator
            bool runAsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            if(!runAsAdmin)
            {
                var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };

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

            CultureInfo ci = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name);
            if (ci.NumberFormat.NumberDecimalSeparator != ".")
            {
                // Forcing use of decimal separator for numerical values
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
            }

            //Construct our taskbar icon
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = false,
                Icon = Properties.Resources.AppIcon,
                Text = "BnS Multi Tool"
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            Application.Current.Exit += Current_Exit;
            UpdateButtonObj = MultiTool_UPDATE;

            this.MouseDown += delegate { try { DragMove(); } catch (Exception) { } }; //Mousedown event to enable dragging of the window
            ICON_THEME_CB.SelectedIndex = SystemConfig.SYS.THEME; //Set our selected theme for menu icons

            //Required enforcement for MegaAPIClient
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            try
            {
                //Check for .NET Framework 4.7.2 or higher
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

                //Check if multitool_qol.xml exists in Documents\BnS if not create it and write our default-template
                if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                {
                    using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                        output.Write(Properties.Resources.multitool_qol);
                }

                //Load XML contents into memory
                qol_xml = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

                //Make the tooltip stay on screen till hover is over.
                ToolTipService.ShowDurationProperty.OverrideMetadata(
                    typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

                FTHRelativeCheck(); //Will eventually delete this just a temporary call
            }
            catch (Exception) { }

            //Remove updater executable if it exists
            if (File.Exists("BnS-Multi-Tool-Updater.exe"))
                File.Delete("BnS-Multi-Tool-Updater.exe");

            //Check if game path was set (first-time setup) if it was not check known registry points for a game version
            if (string.IsNullOrEmpty(SystemConfig.SYS.BNS_DIR))
            {
                try
                {
                    RegistryKey BNS_REGISTRY = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\NCWest\BnS_UE4"); //NAEU version

                    if (BNS_REGISTRY != null)
                    {
                        SystemConfig.SYS.BNS_DIR = BNS_REGISTRY.GetValue("BaseDir").ToString();
                        if (SystemConfig.SYS.BNS_DIR.Last() != '\\')
                            SystemConfig.SYS.BNS_DIR += "\\";
                    } else
                    {
                        BNS_REGISTRY = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\plaync\BNS_LIVE"); //KR version

                        if (BNS_REGISTRY == null)
                            throw new Exception("No registry entries for client");

                        SystemConfig.SYS.BNS_DIR = BNS_REGISTRY.GetValue("BaseDir").ToString();
                        if (SystemConfig.SYS.BNS_DIR.Last() != '\\')
                            SystemConfig.SYS.BNS_DIR += "\\";

                        ACCOUNT_CONFIG.ACCOUNTS.REGION = (int)Globals.BnS_Region.KR;
                        ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE = 5;
                    }

                    SystemConfig.appendChangesToConfig();
                }
                catch (Exception)
                {
                    MessageBox.Show("The path for Blade and Soul could not be found, manually select the game path.", "COULD NOT FIND GAME");
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
            DeltaPatching_Checkbox.IsChecked = SystemConfig.SYS.DELTA_PATCHING == 1;
            PingCheckTick.IsChecked = SystemConfig.SYS.PING_CHECK == 1;
            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;

            //Cheap lazy fix because I'm done really doing anything with this.
            if(SystemConfig.SYS.patch64 == 0)
            {
                SystemConfig.SYS.patch64 = 1;
                SystemConfig.SYS.patch32 = 1;
                SystemConfig.appendChangesToConfig();
            }
        }

        private void Current_Exit(object sender, ExitEventArgs e) => notifyIcon.Dispose();
        private void NotifyIcon_DoubleClick(object sender, EventArgs e) => changeWindowState(true, true);
        private void EXIT_BTN_Click(object sender, RoutedEventArgs e) => this.Close();

        private void SetCurrentPage(string pageName)
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
            SetCurrentPage("MainPage");

            VERSION_LABEL.Text = string.Format("BnS Multi Tool Version: {0}", FileVersion());

            //Check version in settings.json and overwrite if they don't match, means they probably updated manually...
            if(SystemConfig.SYS.VERSION != FileVersion())
            {
                SystemConfig.SYS.VERSION = FileVersion();
                SystemConfig.appendChangesToConfig();
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => this.Close();

        public static string FileVersion() =>
             FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        private void OnNotifyDoubleClick(object sender, RoutedEventArgs e) =>
            changeWindowState(true,true);

        private void MAIN_CLICK(object sender, RoutedEventArgs e) =>
            SetCurrentPage("MainPage");

        private void LAUNCHER_CLICK(object sender, RoutedEventArgs e)
        {
            PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            SetCurrentPage("Launcher");
        }

        public static void changeWindowState(bool state, bool overwrite = false)
        {
            if (SystemConfig.SYS.MINIMZE_ACTION == 0 && !overwrite)
                mainWindow.WindowState = WindowState.Minimized;
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
                        notifyIcon.Visible = false;
                    }
                    else
                    {
                        notifyIcon.Visible = true;
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

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
        }

        //Navigation Button Clicks
        private void Button_Click(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Patches");

        private void Button_Click_1(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Effects");

        private void Button_Click_3(object sender, RoutedEventArgs e) =>
            changeWindowState(false);

        private void Button_Click_2(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Mods");

        private void Button_Click_4(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Modpolice");

        private void GameUpdaterButton(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Gameupdater");

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
                //Get rid of compatibility options on BNSR.exe
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                var keys = key.GetValueNames().Where(x => x.Contains(@"BNSR\Binaries\Win64\BNSR.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

                key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                keys = key.GetValueNames().Where(x => x.Contains(@"BNSR\Binaries\Win64\BNSR.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

                new ErrorPrompt("Compatibility Options have been cleared for BNSR.exe", true).ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void FTHExclusion(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey FTH = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH", true);
                var exclusionObj = FTH.GetValue("ExclusionList");
                var exclusionList = new List<string>(exclusionObj as string[]);

                if (!exclusionList.Any(exe => exe == "BNSR.exe"))
                {
                    exclusionList.Add("BNSR.exe");
                    FTH.SetValue("ExclusionList", exclusionList.ToArray());

                    //Purge the state for BNSR.exe (Requires full path to exe)
                    var state = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH\State", true);
                    var stateobj = state.GetValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                    if (stateobj != null)
                        state.DeleteValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));

                    //Prompt user that an exclusion was set, pass true for Poggies dialog
                    new ErrorPrompt("FTH Exclusion set for BNSR.exe", true).ShowDialog();
                }
            } catch (Exception ex)
            {
                new ErrorPrompt(ex.Message).ShowDialog();
            }
        }

        //Convert current FTH exclusions for BNSR.exe from exact path to relative name
        private void FTHRelativeCheck()
        {
            RegistryKey FTH = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH", true);
            var exclusionObj = FTH.GetValue("ExclusionList");
            var exclusionList = new List<string>(exclusionObj as string[]);

            if(exclusionList.Any(exe => exe == Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe")))
            {
                exclusionList.Remove(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                exclusionList.Add("BNSR.exe");
                FTH.SetValue("ExclusionList", exclusionList.ToArray());
            }
        }

        private void ClearFTHState(object sender, RoutedEventArgs e)
        {
            try
            {
                var state = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH\State", true);
                var stateobj = state.GetValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                if (stateobj != null)
                    state.DeleteValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));

                new ErrorPrompt("Any FTH State for BNSR.exe has been cleared", true).ShowDialog();
            } catch (Exception)
            {

            }
        }

        private class MENU_THEME_STRUCT
        {
            public string PATH { get; set; }
            public string IMAGE_CONTROL { get; set; }
        }

        private static readonly List<MENU_THEME_STRUCT> AGON_THEME = new List<MENU_THEME_STRUCT>()
        {
            new MENU_THEME_STRUCT { PATH = "Images/agon/ue4agon.png", IMAGE_CONTROL = "BANNER_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonDepressed.png", IMAGE_CONTROL = "MENU_MAIN_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonWicked.png", IMAGE_CONTROL = "MENU_LAUNCHER_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonKnife.png", IMAGE_CONTROL = "MENU_PATCHES_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonDColon.png", IMAGE_CONTROL = "MENU_EFFECTS_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonWokeage.png", IMAGE_CONTROL = "MENU_MODS_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonModMan.png", IMAGE_CONTROL = "MENU_MODPOLICE_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonCopium.png", IMAGE_CONTROL = "MENU_BNSUPDATER_ICON" }
        };

        private static readonly List<MENU_THEME_STRUCT> WORRY_THEME = new List<MENU_THEME_STRUCT>()
        {
            new MENU_THEME_STRUCT { PATH = "BnS_LOGO.png", IMAGE_CONTROL = "BANNER_ICON" },
            new MENU_THEME_STRUCT { PATH = "peepoWtf.png", IMAGE_CONTROL = "MENU_MAIN_ICON" },
            new MENU_THEME_STRUCT { PATH = "467303591695089664.png", IMAGE_CONTROL = "MENU_LAUNCHER_ICON" },
            new MENU_THEME_STRUCT { PATH = "worryDealWithIt.png", IMAGE_CONTROL = "MENU_PATCHES_ICON" },
            new MENU_THEME_STRUCT { PATH = "worryDodge.png", IMAGE_CONTROL = "MENU_EFFECTS_ICON" },
            new MENU_THEME_STRUCT { PATH = "worryYay.png", IMAGE_CONTROL = "MENU_MODS_ICON" },
            new MENU_THEME_STRUCT { PATH = "modpolice_btn.png", IMAGE_CONTROL = "MENU_MODPOLICE_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/BnSIcon.png", IMAGE_CONTROL = "MENU_BNSUPDATER_ICON" }
        };

        private void THEME_CHANGED(object sender, SelectionChangedEventArgs e)
        {
            int current_selection = ((ComboBox)sender).SelectedIndex;
            List<MENU_THEME_STRUCT> theme = current_selection == 0 ? WORRY_THEME : AGON_THEME;
            SystemConfig.SYS.THEME = current_selection;
            SystemConfig.appendChangesToConfig(); 

            foreach(var menu in theme)
            {
                Image control = (Image)this.FindName(menu.IMAGE_CONTROL);
                if (control != null)
                    control.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(menu.PATH, UriKind.Relative));
            }
        }
    }
}
