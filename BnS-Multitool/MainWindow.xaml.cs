using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Reflection;
using System.Security.Principal;
using System.Xml.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using BnS_Multitool.Functions;

namespace BnS_Multitool
{
    public partial class MainWindow : Window
    {
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)]
                                         string lpFileName);

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
        private void RestartAsAdmin()
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

        public MainWindow()
        {
            // Check if application is running as administrator, if not restart it as admin.
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                RestartAsAdmin();

            // Check if Segoe MDL2 Assets or Segoe UI is installed, if not install and restart the application. I have no idea if this actually works lmao
            /*
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegMDL2.tff")) ||
                !File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeuib.ttf"))
                )
            {
                string[] fontList = { "SegMDL2", "segoeui", "segoeuib", "segoeuil", "segoeuisl", "seguisb" }; // List of fonts we're installing
                int fontsinstalled = 0;
                foreach(var font in fontList)
                {
                    var result = AddFontResource(string.Format("Resources/Fonts/{0}.ttf", font));
                    var errno = Marshal.GetLastWin32Error();
                    // If no error reported to windows and result > 0 = installed so increment installed fonts
                    if (errno == 0 && result != 0)
                        fontsinstalled++;
                }
                // If fonts were installed then do a restart.
                if (fontsinstalled > 0)
                    RestartAsAdmin();
            }
            */

            // Setup NLog Configuration
            string LogFileName = Path.Combine("logs", $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
            var config = new NLog.Config.LoggingConfiguration();
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new NLog.Targets.ConsoleTarget("logconsole"));
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, new NLog.Targets.FileTarget("logfile") { FileName = LogFileName});
            NLog.LogManager.Configuration = config;

            Logger.log.Info("Initialized Logger");

            InitializeComponent();

            CultureInfo ci = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name);
            if (ci.NumberFormat.NumberDecimalSeparator != ".")
            {
                // Forcing use of decimal separator for numerical values
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
            }

            // Construct our taskbar icon
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

            // Required enforcement for MegaAPIClient
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            try
            {
                // Check for .NET Framework 4.7.2 or higher
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

                // Check if multitool_qol.xml exists in Documents\BnS if not create it and write our default-template
                if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                {
                    Logger.log.Info("multitool_qol.xml does not exist, writing file");
                    using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                        output.Write(Properties.Resources.multitool_qol);
                }

                Logger.log.Info("Loading multitool_qol.xml from Documents\\BnS");
                // Load XML contents into memory
                qol_xml = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

                Logger.log.Info("Adjusting Tool-tip Service");
                // Make the tooltip stay on screen till hover is over.
                ToolTipService.ShowDurationProperty.OverrideMetadata(
                    typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

                Logger.log.Info("Validating Sync AUTH_KEY");
                new SyncConfig();
                if (string.IsNullOrEmpty(SyncConfig.AUTH_KEY))
                    SyncConfig.AUTH_KEY = "";

                // Code that needs to be removed after next patch, meant to move directories for testers
                if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "sync")))
                    Globals.MoveDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "sync"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync"));
            }
            catch (Exception ex)
            {
                Logger.log.Error("Error occured in initialization\nType: {0}\n{1}\n{2}", ex.ToString(), ex.GetType().Name, ex.StackTrace);
            }

            // Remove updater executable if it exists
            if (File.Exists("BnS-Multi-Tool-Updater.exe"))
                File.Delete("BnS-Multi-Tool-Updater.exe");

            // Check if game path was set (first-time setup) if it was not check known registry points for a game version
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
                    }
                    else
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

                    SystemConfig.Save();
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
                            SystemConfig.Save();
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

            try
            {
                if (!Directory.Exists("modpolice"))
                    Directory.CreateDirectory("modpolice");

                // Extract 7za.dll from resources if it does not exist
                if (!File.Exists("7za.dll"))
                    File.WriteAllBytes("7za.dll", Properties.Resources._7za);

                // Set the library path that we'll be using
                SevenZip.SevenZipBase.SetLibraryPath("7za.dll");
            } catch (Exception ex)
            {
                Logger.log.Error(ex.ToString());
            }
            Logger.log.Info("Initialized");

            // Get rid of this next patch
            if(SystemConfig.SYS.THEME == 2)
            {
                ICON_THEME_CB.SelectedIndex = 1;
            }
        }

        private void Current_Exit(object sender, ExitEventArgs e) => notifyIcon.Dispose(); // Dispose of task tray icon on app exit
        private void NotifyIcon_DoubleClick(object sender, EventArgs e) => ChangeWindowState(true, true); // Unminimize app when task tray double click triggered
        private void EXIT_BTN_Click(object sender, RoutedEventArgs e) => this.Close(); // Close application

        public void SetCurrentPage(string pageName, bool cleanMemory = false)
        {
            // Clear working-set for app
            if(cleanMemory) PoormanCleaner.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            var cachedPage = navigationPages.FirstOrDefault(x => x.PageName == pageName);

            if (cachedPage == null)
            {
                switch (pageName)
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
                    case "Sync":
                        navigationPages.Add(new Page_Navigation() { PageName = pageName, PageSource = new Sync() });
                        break;
                }
                cachedPage = navigationPages.FirstOrDefault(x => x.PageName == pageName);
            }

            MainFrame.Content = cachedPage.PageSource;
            currentPageText = pageName;
            GC.WaitForPendingFinalizers(); // Force garbage collection
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowFrame = MainFrame;
            mainWindow = this;
            // Load our main page.
            SetCurrentPage("MainPage");

            VERSION_LABEL.Text = string.Format("BnS Multi Tool Version: {0}", FileVersion());

            // Check version in settings.json and overwrite if they don't match, why the fuck do I still do this? will probably remove later
            if (SystemConfig.SYS.VERSION != FileVersion())
            {
                SystemConfig.SYS.VERSION = FileVersion();
                SystemConfig.Save();
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) => this.Close();

        public static string FileVersion() =>
             FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        private void OnNotifyDoubleClick(object sender, RoutedEventArgs e) =>
            ChangeWindowState(true, true);

        private void MAIN_CLICK(object sender, RoutedEventArgs e) =>
            SetCurrentPage("MainPage");

        private void LAUNCHER_CLICK(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Launcher", true);

        public static void ChangeWindowState(bool state, bool overwrite = false)
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
            // Updater code
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

        // Navigation Button Clicks
        private void CustomPatchesClick(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Patches");

        private void EffectsClick(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Effects");

        private void Button_Click_3(object sender, RoutedEventArgs e) =>
            ChangeWindowState(false);

        private void ModsClick(object sender, RoutedEventArgs e) =>
            SetCurrentPage("Mods");

        private void PluginsClick(object sender, RoutedEventArgs e) =>
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

            SystemConfig.Save();
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
                // Get rid of compatibility options on BNSR.exe
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
                Logger.log.Error("CompatibilityOptionS_Click\n{0}", ex.ToString());
            }
        }

        private void FTHExclusion(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey FTH = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH", true);
                var exclusionObj = FTH.GetValue("ExclusionList");
                var exclusionList = new List<string>(exclusionObj as string[]);

                if (!exclusionList.Any(exe => exe.Contains("BNSR.exe")))
                {
                    exclusionList.Add(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                    FTH.SetValue("ExclusionList", exclusionList.ToArray());

                    // Purge the state for BNSR.exe (Requires full path to exe)
                    var state = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH\State", true);
                    var stateobj = state.GetValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                    if (stateobj != null)
                        state.DeleteValue(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));

                    // Prompt user that an exclusion was set, pass true for Poggies dialog
                    new ErrorPrompt("FTH Exclusion set for BNSR.exe", true).ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("FTHExclusion\nType: {0}\n{0}", ex.GetType().Name, ex.ToString());
                new ErrorPrompt(ex.Message).ShowDialog();
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
            }
            catch (Exception ex)
            {
                Logger.log.Error("ClearFTHState\nType: {0}\n{0}", ex.GetType().Name, ex.ToString());
            }
        }

        private class MENU_THEME_STRUCT
        {
            public string PATH { get; set; }
            public string IMAGE_CONTROL { get; set; }
        }

        private static readonly List<MENU_THEME_STRUCT> AGON_THEME = new List<MENU_THEME_STRUCT>()
        {
            new MENU_THEME_STRUCT { PATH = "Images/agon/ue4agonhigh.png", IMAGE_CONTROL = "BANNER_ICON" },
            new MENU_THEME_STRUCT { PATH = "Images/agon/agonHype.png", IMAGE_CONTROL = "MENU_MAIN_ICON" },
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
            List<MENU_THEME_STRUCT> theme = null;

            switch(current_selection)
            {
                case 0:
                    theme = WORRY_THEME;
                    break;
                case 1:
                    theme = AGON_THEME;
                    break;
                case 2:
                    theme = AGON_THEME;
                    break;
            }
            SystemConfig.SYS.THEME = current_selection;
            SystemConfig.Save();

            foreach (var menu in theme)
            {
                Image control = (Image)this.FindName(menu.IMAGE_CONTROL);
                if (control != null)
                {
                    if (SystemConfig.SYS.THEME == 2 && menu.IMAGE_CONTROL == "BANNER_ICON")
                        control.Stretch = System.Windows.Media.Stretch.None;
                    else if (menu.IMAGE_CONTROL == "BANNER_ICON")
                        control.Stretch = System.Windows.Media.Stretch.Uniform;

                    control.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(menu.PATH, UriKind.Relative));
                }
            }
        }

        private void NavigationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var control = sender as ListView;
                if (control != null && control.SelectedIndex == -1) return;
                if (control == null) return;
                control.SelectedIndex = -1;
            } catch { }
        }
    }
}
