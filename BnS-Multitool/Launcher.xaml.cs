using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Management;
using System.Collections.ObjectModel;
using BnS_Multitool.Extensions;
using static BnS_Multitool.Functions.FileExtraction;
using static BnS_Multitool.Functions.Crypto;
using OtpNet;

namespace BnS_Multitool
{
    public partial class Launcher : Page
    {
        public class SESSION_LIST
        {
            public string EMAIL { get; set; }
            public int REGION { get; set; }
            public Process PROCESS { get; set; }
            public string DisplayName { get; set; }
        }

        public static int ACCOUNT_SELECTED_INDEX = -1;
        private static string bin_path = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64");
        private static string plugins_path = Path.Combine(bin_path, "plugins");
        public bool loginHelper_installed = File.Exists(Path.Combine(plugins_path, "loginhelper.dll"));
        private static string pluginPath = Path.Combine("BNSR", "Binaries", "Win64", "plugins");
        private bool loader3_installed = false;
        private bool bnsnogg_installed = false;
        private ProgressControl _progressControl;
        private static BackgroundWorker monitorProcesses = new BackgroundWorker();
        private static DispatcherTimer memoryTimer = new DispatcherTimer();
        private static bool ignoreRestOfSession = false;

        public ObservableCollection<SESSION_LIST> ActiveClientList { get; set; }

        public class SkillData
        {
            public int skillID { get; set; }
            public float skillvalue { get; set; }
            public int mode { get; set; }
            public string description { get; set; }
            public float recycleTime { get; set; }
            public int recycleMode { get; set; }
            public int ignoreAutoBias { get; set; }
        }

        private List<SkillData> LoadSkillDataCollection()
        {
            List<SkillData> skillData = new List<SkillData>();

            var nodes = MainWindow.qol_xml.Descendants("skill");

            foreach (var node in nodes)
                skillData.Add(new SkillData()
                {
                    skillID = int.Parse(node.Attribute("id").Value),
                    skillvalue = float.Parse(node.Attribute("value").Value),
                    mode = int.Parse(node.Attribute("mode").Value),
                    description = (node.Attribute("description") == null) ? "" : node.Attribute("description").Value,
                    recycleTime = float.Parse((node.Attribute("recycleTime") == null) ? "-0.015" : node.Attribute("recycleTime").Value),
                    recycleMode = int.Parse((node.Attribute("recycleMode") == null) ? "0" : node.Attribute("recycleMode").Value),
                    ignoreAutoBias = int.Parse((node.Attribute("ignoreAutoBias") == null) ? "0" : node.Attribute("ignoreAutoBias").Value)
                });

            return skillData;
        }

        public class ConsoleWriterEventArgs : EventArgs
        {
            public string Value { get; private set; }
            public ConsoleWriterEventArgs(string value)
            {
                Value = Value;
            }
        }

        public Launcher()
        {
            InitializeComponent();

            DataContext = this;
            ActiveClientList = new ObservableCollection<SESSION_LIST>();

            //Set our default selections if saved and other misc stuff
            try
            {
                if (ACCOUNT_CONFIG.ACCOUNTS.USE_ALL_CORES == 1)
                    USE_ALL_CORES.IsChecked = true;

                if (ACCOUNT_CONFIG.ACCOUNTS.USE_TEXTURE_STREAMING == 1)
                    NOTEXTURE_STREAMING.IsChecked = true;

                if (ACCOUNT_CONFIG.ACCOUNTS.ADDITIONAL_PARAMS != "")
                    cmdParams.Text = ACCOUNT_CONFIG.ACCOUNTS.ADDITIONAL_PARAMS;

                if (ACCOUNT_CONFIG.ACCOUNTS.AUTPATCH_QOL == 1)
                    AUTOPATCH_QOL.IsChecked = true;

                REGION_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.REGION;
                LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;
                MemoryCleanerBox.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.MEMORY_CLEANER;

                memoryTimer.IsEnabled = false;
                memoryTimer.Tick += new EventHandler(dispatchTimer_tick);

                if (ACCOUNT_CONFIG.ACCOUNTS.MEMORY_CLEANER != 0)
                {
                    memoryTimer.Interval = TimeSpan.FromMinutes(timerFromSelection());
                    memoryTimer.IsEnabled = true;
                    memoryTimer.Start();
                }

                foreach (var account in ACCOUNT_CONFIG.ACCOUNTS.Saved)
                    ACCOUNT_LIST_BOX.Items.Add(account.EMAIL);

                monitorProcesses.DoWork += new DoWorkEventHandler(monitorActiveProcesses);

                if (ACCOUNT_CONFIG.ACCOUNTS.Saved.Count > 0 && ACCOUNT_CONFIG.ACCOUNTS.LAST_USED_ACCOUNT != -1)
                    ACCOUNT_LIST_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LAST_USED_ACCOUNT;

            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::Initialize::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                var dialog = new ErrorPrompt("Something went wrong, accounts.json is probably corrupted. Check for syntax errors in accounts.json or delete entirely.\r\rAddition information: \r" + ex.Message);
                dialog.ShowDialog();
                Environment.Exit(0);
            }

            SetupQoLSettings:
            try
            {
                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useMarketplace']").Attribute("enable").Value == "1")
                    useMarketplace.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useAutoBait']").Attribute("enable").Value == "1")
                    useAutoBait.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useItemCap']").Attribute("enable").Value == "1")
                    useItemCap.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("enable").Value == "1")
                    AutoCombat.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("useRange").Value == "1")
                    autocombatrangeTOS.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useNoCameraLock']").Attribute("enable").Value == "1")
                    useNoCameraLock.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useDebug']") == null)
                {
                    var nodes = MainWindow.qol_xml.Descendants("options").Last();
                    nodes.Add(new XElement("option", new XAttribute("name", "useDebug"), new XAttribute("enable", "0")));
                    MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                }

                if(MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useWindowClipboard']") == null)
                {
                    var options = MainWindow.qol_xml.Descendants("options").Last();
                    options.Add(new XElement("option", new XAttribute("name", "useWindowClipboard"), new XAttribute("enable", "0")));
                    MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                }

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useWindowClipboard']").Attribute("enable").Value == "1")
                    useWindowClipboard.IsChecked = true;
                
                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useDebug']").Attribute("enable").Value == "1")
                    useDebug.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("TurnOffOnDeath") == null)
                {
                    var nodes = MainWindow.qol_xml.XPathSelectElement("/config/options/option[@name='AutoCombat']");
                    nodes.Add(new XAttribute("TurnOffOnDeath", "0"));
                    MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                }

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("TurnOffOnDeath").Value == "1")
                    autoCombatTurnOffOndeath.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useNoWallRunStamina']") == null)
                {
                    var nodes = MainWindow.qol_xml.Descendants("options").Last();
                    nodes.Add(new XElement("option", new XAttribute("name", "useNoWallRunStamina"), new XAttribute("enable", "0")));
                    MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                }

                if (MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='useNoWallRunStamina']").Attribute("enable").Value == "1")
                    useNoWallRunStamina.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("enable").Value == "1")
                    enableGCD.IsChecked = true;

                if (MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("ignorePing") == null)
                {
                    var nodes = MainWindow.qol_xml.XPathSelectElement("config/gcd");
                    nodes.Add(new XAttribute("ignorePing", "0"));
                    MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                }
                
                ignorePing.SelectedIndex = int.Parse(MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("ignorePing").Value);

                SkillDataGrid.ItemsSource = LoadSkillDataCollection();
                autoCombatRange.Text = MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value;

            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::Initialize::QoL::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                    File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

                using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                    output.Write(Properties.Resources.multitool_qol);

                Logger.log.Info("Loading multitool_qol.xml from Documents\\BnS");
                MainWindow.qol_xml = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
                new ErrorPrompt(string.Format("Could not parse multitool_qol.xml, it has been reset. If continue getting this report it.")).ShowDialog();
                goto SetupQoLSettings;
            }
        }

        private void monitorActiveProcesses(object sender, DoWorkEventArgs e)
        {
            while (ActiveClientList.Count() > 0)
            {
                try
                {
                    foreach (var session in ActiveClientList.Select((item, index) => new { index, item }).ToList())
                    {
                        if (session.item.PROCESS.HasExited)
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                ActiveClientList.RemoveAt(session.index);
                            });
                        }
                    }

                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Launcher::MonitorActiveProcesses::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                }
            }

            if (MainWindow.isMinimized)
                MainWindow.ChangeWindowState(true);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Task.Run(new Action(() =>
                {
                    if (ACCOUNT_CONFIG.ACCOUNTS.REGION < 2 || (Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.KR)
                    {
                        Globals.GameVersionCheck();
                        Globals.isLoginAvailable();
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            if (Globals.localBnSVersion != Globals.onlineBnSVersion || !Globals.loginAvailable)
                            {
                                var dialog = new ErrorPrompt(String.Format("{0}\n{1}", (!Globals.loginAvailable) ? "The server is currently undergoing maintenance." : "", (Globals.localBnSVersion != Globals.onlineBnSVersion) ? "A game update is available" : ""));
                                dialog.Owner = MainWindow.mainWindow;
                                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                dialog.ShowDialog();
                            }
                        });
                    }
                }));

                await CheckOnlineVersion();
            } catch (Exception ex)
            {
                Logger.log.Error("Launcher::Page_Loaded::Type: {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
            }
        }

        private void SaveAccount(object sender, RoutedEventArgs e)
        {
            if (BNS_USERNAME_BOX.Text == "")
            {
                classLabel.Text = "Email field cannot be left blank";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }
            else if (BNS_PASSWORD_BOX.Password == "")
            {
                classLabel.Text = "Password field cannot be left blank";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            List<ACCOUNT_CONFIG.BNS_SAVED_ACCOUNTS_STRUCT> SAVED_ACCOUNTS = ACCOUNT_CONFIG.ACCOUNTS.Saved;
            int HAS_INDEX = SAVED_ACCOUNTS.FindIndex(x => x.EMAIL == BNS_USERNAME_BOX.Text);

            if (HAS_INDEX == -1)
            {
                SAVED_ACCOUNTS.Add(new ACCOUNT_CONFIG.BNS_SAVED_ACCOUNTS_STRUCT()
                {
                    EMAIL = BNS_USERNAME_BOX.Text,
                    PASSWORD = BNS_PASSWORD_BOX.Password,
                    PINCODE = BNS_PINCODE_BOX.Text
                });
            }
            else
            {
                if (BNS_PASSWORD_BOX.Password != "")
                    SAVED_ACCOUNTS[HAS_INDEX].PASSWORD = BNS_PASSWORD_BOX.Password;

                if (BNS_PINCODE_BOX.Text != "")
                    SAVED_ACCOUNTS[HAS_INDEX].PINCODE = BNS_PINCODE_BOX.Text;
            }

            ACCOUNT_CONFIG.ACCOUNTS.Saved = SAVED_ACCOUNTS;
            ACCOUNT_CONFIG.appendChangesToConfig();

            ACCOUNT_LIST_BOX.Items.Clear(); //Flush the list
            foreach (var account in ACCOUNT_CONFIG.ACCOUNTS.Saved)
                ACCOUNT_LIST_BOX.Items.Add(account.EMAIL);

            new ErrorPrompt("Account Saved", true, true).ShowDialog();
            BNS_USERNAME_BOX.Text = string.Empty;
            BNS_PASSWORD_BOX.Password = string.Empty;
            BNS_PINCODE_BOX.Text = string.Empty;
        }

        private string languageFromSelection()
        {
            string lang;
            switch (LANGUAGE_BOX.SelectedIndex)
            {
                case 1:
                    lang = "BPORTUGUESE";
                    break;
                case 2:
                    lang = "GERMAN";
                    break;
                case 3:
                    lang = "FRENCH";
                    break;
                case 4:
                    lang = "CHINESET";
                    break;
                default:
                    lang = "English";
                    break;
            }
            return lang;
        }

        private async Task LaunchNewGameClient()
        {
            try
            {
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                var ActiveClient = ActiveClientList.FirstOrDefault(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);

                Process proc = new Process();
                proc.StartInfo.FileName = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe");

                // Arugments passed to BNSR.exe
                if (REGION_BOX.SelectedIndex >= 2)
                {
                    // Other Regions (TW / KR)
                    proc.StartInfo.Arguments = string.Format(@"/sesskey /LaunchByLauncher /Loginmode -FIXPROGRAMID -unattended {0} {1} {2}",
                      ((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMING " : "", ((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORES " : "", cmdParams.Text);
                }
                else
                {
                    // NA & EU Region
                    proc.StartInfo.Arguments = string.Format(@"/sesskey /LaunchByLauncher /Loginmode -FIXPROGRAMID -lang:{0} -region:{1} -unattended {2} {3} {4}",
                        languageFromSelection(), REGION_BOX.SelectedIndex, ((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMING " : "", ((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORES " : "", cmdParams.Text);
                }

                // We need to truncate the password down to 16 characters as NCSoft password limit is technically 16 and trims off everything after 16, only know of it being a problem in NA/EU not sure about TW
                string pw = (Globals.BnS_Region)REGION_BOX.SelectedIndex != Globals.BnS_Region.TW ? ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD.Truncate(16) : ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD;

                // Setup environment variables for loginhelper
                proc.StartInfo.UseShellExecute = false; // Required for setting environment variables to processes
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_USERNAME", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].EMAIL);
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_PASSWORD", pw);

                // Check if the pincode is numeric, if it's not all numeric values then it's possibly the OTP secret key
                // This was suggested / provided by dbnryanc92 on Github for TW specifically but I have adjusted it for all regions.
                if (int.TryParse(ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE, out _))
                    proc.StartInfo.EnvironmentVariables.Add("BNS_PINCODE", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE);
                else if (!ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE.IsNullOrEmpty())
                {
                    Totp totp = new Totp(Base32Encoding.ToBytes(ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE));
                    string otp = totp.ComputeTotp();
                    proc.StartInfo.EnvironmentVariables.Add("BNS_PINCODE", otp);
                }

                proc.StartInfo.RedirectStandardOutput = true;

                if (MainWindow.qol_xml.XPathSelectElements("config/options/option[@enable='1']").Count() > 0 || MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("enable").Value == "1")
                    await QOL_PLUGIN_CHECK(); // Check version on launch

                // All checks out, start process
                proc.Start();
                if (ActiveClient == null)
                {
                    ActiveClientList.Add(new SESSION_LIST() { EMAIL = EMAIL, REGION = REGION_BOX.SelectedIndex, PROCESS = proc, DisplayName = string.Format("{0} - {1}", EMAIL, getSelectedRegion(REGION_BOX.SelectedIndex)) });

                    if (ActiveClientList.Count == 1 && !monitorProcesses.IsBusy)
                        monitorProcesses.RunWorkerAsync(); // Start our worker thread.
                }
                else
                    ActiveClient.PROCESS = proc;

                // Wait for the client to be loaded and responding before minimizing to avoid confusion on long launch time.
                Task<bool> clientisLoaded = Task.Run(proc.WaitForInputIdle);
                await clientisLoaded;
                if (proc.HasExited) goto EndFunction;

                // New code for settings option.
                switch (SystemConfig.SYS.NEW_GAME_OPTION)
                {
                    case 1:
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        break;
                    case 2:
                        MainWindow.ChangeWindowState(false, true);
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::LaunchNewGameClient::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }

        EndFunction:
            GC.Collect();
        }

        private static string getSelectedRegion(int id)
        {
            string region;
            switch (id)
            {
                case 1:
                    region = "EU";
                    break;
                case 2:
                    region = "TW";
                    break;
                default:
                    region = "NA";
                    break;
            }

            return region;
        }

        private async void LaunchGameClientClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var processCount = Process.GetProcessesByName("BNSR").Count();
                // Some error checking for mentally challenged people that are in so much of a hurry they can't read
                if(MainPage.onlineJson.ANTI_CHEAT_ENABLED == 1 && (Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION != Globals.BnS_Region.TW)
                {
                    var dialog = new ErrorPrompt("The Anti-cheat has been enabled, launching the game will be not possible right now. Remove loader3 before launching the game and keep an eye out on the discord for more information.", false, true);
                    dialog.ShowDialog();
                    return;
                }

                if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
                {
                    classLabel.Text = "No account selected, select an account before attemping to launch a new client";
                    ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                    return;
                }
                else if (!loginHelper_installed && processCount == 0)
                {
                    var dialog = new ErrorPrompt("Loginhelper is missing, install loginhelper before continuing", false, true);
                    dialog.ShowDialog();
                    return;
                }
                else if (!loader3_installed && processCount == 0)
                {
                    var dialog = new ErrorPrompt("Loader3 is missing, You can click the install button below to get the required plugins", false, true);
                    dialog.ShowDialog();
                    return;
                }
                else if (!bnsnogg_installed && processCount == 0)
                {
                    var dialog = new ErrorPrompt("BNSNoGG is missing, You can click the install button below to get the required plugins", false, true);
                    dialog.ShowDialog();
                    return;
                }
                #region hellokittywarnings
                if ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION != Globals.BnS_Region.TW)
                {
                    var result = await Modpolice.HKCheck();
                    if (result == Modpolice.HK_State.Error && SystemConfig.SYS.HK_Installed)
                    {
                        new ErrorPrompt("There was an error getting your local client version, try doing a file check on the game.").ShowDialog();
                        return;
                    }
                    if (result == Modpolice.HK_State.Not_Installed) goto ContinueLaunching;
                    if (result == Modpolice.HK_State.Installed) goto ContinueLaunching;

                    // Check if datafile is missing but binloader is present
                    if (result == Modpolice.HK_State.Missing_datafile && SystemConfig.SYS.HK_Installed)
                    {
                        var drresult = WPFCustomMessageBox.CustomMessageBox.ShowYesNoCancel("You have binloader installed but are missing datafile64.bin, did you rename it? If you did then you know what you're doing and you can click Ignore.", "Binloader Error", "Install HK", "Remove Binloader", "Ignore Warning");
                        if (drresult == MessageBoxResult.No)
                        {
                            if (File.Exists(Path.Combine(plugins_path, "binloader.dll")))
                                File.Delete(Path.Combine(plugins_path, "binloader.dll"));
                            goto ContinueLaunching;
                        }
                        else if (drresult == MessageBoxResult.Cancel)
                        {
                            ignoreRestOfSession = true;
                            goto ContinueLaunching;
                        }
                        else if (drresult == MessageBoxResult.Yes)
                            goto InstallHKFromLauncher;
                        else
                            return;
                    }

                    // Alert that an update for HK is available
                    if (result == Modpolice.HK_State.Update_Available)
                    {
                        if (ignoreRestOfSession) goto ContinueLaunching;
                        var drresult = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel("An update for Hello Kitty is available\nIgnore will get rid of this message until you restart multi-tool", "HK Update", "Update", "Ignore Message");
                        if (drresult == MessageBoxResult.OK) goto InstallHKFromLauncher;
                        else
                        {
                            ignoreRestOfSession = true;
                            goto ContinueLaunching;
                        }
                    }

                    if (result == Modpolice.HK_State.Outdated)
                    {
                        if (ignoreRestOfSession) goto ContinueLaunching;
                        var dresult = WPFCustomMessageBox.CustomMessageBox.ShowYesNoCancel("It appears that your version of Hello Kitty does not match server, what would you like to do?\n\nNote that ignoring can cause your game to not function properly or at all if it's not compatible!\n\nIgnore will get rid of this message until you restart multi-tool",
                            "HK Warning", "Update", "Turn Off HK", "Ignore Warning");
                        if (dresult == MessageBoxResult.Cancel)
                        {
                            ignoreRestOfSession = true;
                            goto ContinueLaunching;
                        }
                        if (dresult == MessageBoxResult.No)
                        {
                            if (File.Exists(Path.Combine(plugins_path, "binloader.dll")))
                                File.Delete(Path.Combine(plugins_path, "binloader.dll"));
                            goto ContinueLaunching;
                        }

                        if (dresult == MessageBoxResult.Yes) goto InstallHKFromLauncher;
                    }

                    if(result == Modpolice.HK_State.Installed_ButBuildMismatch)
                    {
                        if(ignoreRestOfSession) goto ContinueLaunching;
                        var dresult = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel(
                            "No Hello Kitty update for this version of the game\nIt is recommended to turn HK off until a new version is released, ignoring can cause the game to not work at all or have broken functionality.\n\nIgnore will get rid of this message until you restart multi-tool",
                            "Build Mismatch",
                            "Ignore Warning",
                            "Turn Off HK"
                            );

                        if(dresult == MessageBoxResult.No)
                        {
                            ignoreRestOfSession = true;
                            goto ContinueLaunching;
                        }

                        if (dresult == MessageBoxResult.Cancel)
                        {
                            if (File.Exists(Path.Combine(plugins_path, "binloader.dll")))
                                File.Delete(Path.Combine(plugins_path, "binloader.dll"));
                            goto ContinueLaunching;
                        }

                        if(dresult == MessageBoxResult.OK) goto ContinueLaunching;
                    }
                    return; // The results we were looking for were not set.

                InstallHKFromLauncher:
                    _progressControl = new ProgressControl();
                    ProgressGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Collapsed;
                    ProgressPanel.Children.Add(_progressControl);

                    bool install_result = false;
                    try
                    {
                        ProgressControl.updateProgressLabel("Updating Hello Kitty");
                        await Task.Delay(TimeSpan.FromSeconds(1.5));
                        install_result = await Modpolice.InstallPlugin(Modpolice.Plugins.PluginInfo.FirstOrDefault(x => x.Name == "binloader"));
                        if (!install_result)
                            throw new Exception("Failed to install binloader, cancelling rest of installation");


                        install_result = await Modpolice.InstallHelloKitty();
                        if (!install_result)
                            throw new Exception("Failed to install binloader, cancelling rest of installation");

                    }
                    catch (Exception ex)
                    {
                        ProgressControl.updateProgressLabel(ex.Message);
                        await Task.Delay(TimeSpan.FromSeconds(2.5));
                    }

                    ProgressGrid.Visibility = Visibility.Hidden;
                    MainGrid.Visibility = Visibility.Visible;
                    ProgressPanel.Children.Clear();
                    _progressControl = null;

                    if (install_result) goto ContinueLaunching;
                    return;
                }
            #endregion

            ContinueLaunching:
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                var ActiveClient = ActiveClientList.Where(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex).FirstOrDefault();

                if (ActiveClient == null)
                    await LaunchNewGameClient();
                else
                {
                    if (ActiveClient.PROCESS.HasExited)
                    {
                        ActiveClient = null;
                        await LaunchNewGameClient();
                    }
                    else
                    {
                        ActiveClient.PROCESS.Kill();

                        await LaunchNewGameClient();
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void killGameProcess(object sender, RoutedEventArgs e)
        {
            if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
                return;

            try
            {
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                var ActiveClient = ActiveClientList.Where(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex).FirstOrDefault();

                if (ActiveClient != null)
                {
                    if (!ActiveClient.PROCESS.HasExited)
                    {
                        try
                        {
                            ActiveClient.PROCESS.Kill();
                            ActiveClient = null;
                        }
                        catch (Exception ex)
                        {
                            var dialog = new ErrorPrompt(ex.Message);
                            dialog.ShowDialog();
                        }
                    }
                    else
                    {
                        ActiveClientList = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::KillGameProcess::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }
        }

        private void ACCOUNT_LIST_BOX_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ACCOUNT_SELECTED_INDEX = ACCOUNT_LIST_BOX.SelectedIndex;
            ACCOUNT_CONFIG.ACCOUNTS.LAST_USED_ACCOUNT = ACCOUNT_SELECTED_INDEX;
            ACCOUNT_CONFIG.appendChangesToConfig();
        }

        private void LaunchInfoSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox currentComboBox = (ComboBox)sender;
            int currentIndex = currentComboBox.SelectedIndex;

            if (currentComboBox.Name == "BIT_BOX")
                ACCOUNT_CONFIG.ACCOUNTS.CLIENT_BIT = currentIndex;
            else if (currentComboBox.Name == "REGION_BOX")
                ACCOUNT_CONFIG.ACCOUNTS.REGION = currentIndex;
            else
                ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE = currentIndex;

            ACCOUNT_CONFIG.appendChangesToConfig();
        }

        private void LaunchInfoCheckStateChanged(object sender, RoutedEventArgs e)
        {
            CheckBox currentCheckBox = (CheckBox)sender;
            int currentState = ((bool)currentCheckBox.IsChecked) ? 1 : 0;

            if (currentCheckBox.Name == "NOTEXTURE_STREAMING")
            {
                ACCOUNT_CONFIG.ACCOUNTS.USE_TEXTURE_STREAMING = currentState;
                ACCOUNT_CONFIG.appendChangesToConfig();
            }
            else if (currentCheckBox.Name == "USE_ALL_CORES")
            {
                ACCOUNT_CONFIG.ACCOUNTS.USE_ALL_CORES = currentState;
                ACCOUNT_CONFIG.appendChangesToConfig();
            }
            else if (currentCheckBox.Name == "AUTOPATCH_QOL")
            {
                ACCOUNT_CONFIG.ACCOUNTS.AUTPATCH_QOL = currentState;
                ACCOUNT_CONFIG.appendChangesToConfig();
            }
            else if (currentCheckBox.Name == "useLastChar")
            {
                ACCOUNT_CONFIG.ACCOUNTS.SELECT_LAST_CHAR = currentState;
                ACCOUNT_CONFIG.appendChangesToConfig();
            }
            else if (currentCheckBox.Name == "enableGCD")
            {
                var gcd_node = MainWindow.qol_xml.XPathSelectElement("/config/gcd");
                gcd_node.Attribute("enable").Value = currentState.ToString();
                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            }
            else if (currentCheckBox.Name == "ignorePing")
            {
                var gcd_node = MainWindow.qol_xml.XPathSelectElement("/config/gcd");
                gcd_node.Attribute("ignorePing").Value = (sender as ComboBox).SelectedIndex.ToString();
                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            }
            else
            {
                string optionName = currentCheckBox.Name;
                if (currentCheckBox.Name == "autocombatrangeTOS")
                    optionName = "AutoCombat";
                else if (currentCheckBox.Name == "autoCombatTurnOffOndeath")
                    optionName = "AutoCombat";

                // Make sure it's not null
                if (MainWindow.qol_xml.XPathSelectElement("/config/options/option[@name='" + optionName + "']") == null)
                {
                    var elements = MainWindow.qol_xml.Descendants("options").Last();
                    elements.Add(new XElement("option", new XAttribute("name", optionName), new XAttribute("enable", "0")));
                }

                var option_node = MainWindow.qol_xml.XPathSelectElement("/config/options/option[@name='" + optionName + "']");
                if (currentCheckBox.Name == "autocombatrangeTOS")
                    option_node.Attribute("useRange").Value = currentState.ToString();
                else if (currentCheckBox.Name == "autoCombatTurnOffOndeath")
                    option_node.Attribute("TurnOffOnDeath").Value = currentState.ToString();
                else
                    option_node.Attribute("enable").Value = currentState.ToString();

                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            }
        }

        TaskCompletionSource<bool> downloadComplete = new TaskCompletionSource<bool>();

        #region pluginstuff

        class requiredList
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Hash { get; set; }
        }
        private async Task CheckOnlineVersion()
        {
            var plugins = await Modpolice.RetrieveOnlinePlugins();
            if (plugins == null)
            {
                new ErrorPrompt("Failed to retrieve pluginList from server", false, true);
                return;
            }

            Modpolice.Plugins = plugins;

            var loader3 = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("loader3"));
            var bnsnogg = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("bnsnogg"));
            var loginhelper = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("loginhelper"));

            if(loader3 == null || bnsnogg == null || loginhelper == null)
            {
                new ErrorPrompt("Null references to pluginInfo", false, true);
                return;
            }

            // Do not check if BnS is detected as running.
            if (ActiveClientList.Count > 0 || Process.GetProcessesByName("BNSR").Count() > 0) 
            {
                if (PluginInfoText.Text.IsNullOrEmpty())
                    PluginInfoText.Text = "Game is running, can't check plugins";

                return; 
            }

            loader3_installed = File.Exists(Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, loader3.FilePath)));
            bnsnogg_installed = File.Exists(Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, bnsnogg.FilePath)));
            loginHelper_installed = File.Exists(Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, loginhelper.FilePath)));
            var requiredList = new List<requiredList>
            {
                new requiredList { Name = "Loader3", Path = Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, loader3.FilePath)), Hash = loader3.Hash.ToLower()},
                new requiredList { Name = "GameGuard Bypass", Path = Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, bnsnogg.FilePath)), Hash = bnsnogg.Hash.ToLower()},
                new requiredList { Name = "LoginHelper", Path = Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, loginhelper.FilePath)), Hash = loginhelper.Hash.ToLower()}
            };

            PluginInfoText.Text = "";

            foreach(var item in requiredList)
            {
                if(File.Exists(item.Path))
                {
                    if(SHA1_File(item.Path) == item.Hash)
                    {
                        System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE10B");
                        text.Foreground = System.Windows.Media.Brushes.Green;
                        PluginInfoText.Inlines.Add(text);
                    } else
                    {
                        System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE118");
                        text.Foreground = System.Windows.Media.Brushes.Yellow;
                        PluginInfoText.Inlines.Add(text);
                    }
                } else
                {
                    System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE10A");
                    text.Foreground = System.Windows.Media.Brushes.Red;
                    PluginInfoText.Inlines.Add(text);
                }

                System.Windows.Documents.Run name = new System.Windows.Documents.Run(" " + item.Name + "\r\n");
                PluginInfoText.Inlines.Add(name);
            }
        }

        private async void installLoginHelperClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            try
            {
                PluginInfoText.Text = "";
                // Loop through all plugins and install the 3 required plugins to launch the game if they need to be installed
                var required = new List<string> { "loader3", "bnsnogg", "loginhelper" };
                foreach (var plugin in Modpolice.Plugins.PluginInfo)
                {
                    bool inList = required.Any(x => x.ToLower() == plugin.Name.ToLower());
                    if (inList)
                    {
                        ProgressControl.updateProgressLabel(string.Format("Validating {0}", plugin.Title));

                        // Check that it doesn't need to be updated first
                        var pluginPath = Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, plugin.FilePath));
                        if (File.Exists(pluginPath) && SHA1_File(pluginPath) == plugin.Hash.ToLower()) continue;

                        ProgressControl.updateProgressLabel(string.Format("Installing {0}", plugin.Title));
                        await Task.Delay(150);

                        // Download and install plugin, returns true if successful
                        bool result = await Modpolice.InstallPlugin(plugin);
                        ProgressControl.updateProgressLabel(string.Format("{0} {1}", plugin.Title, result ? "successfully installed" : "Failed to install"));
                        await Task.Delay(TimeSpan.FromSeconds(1.5));

                        if (plugin.Name == "loader3")
                            loader3_installed = result;

                        if(plugin.Name == "bnsnogg")
                            bnsnogg_installed = result;

                        if(plugin.Name == "loginhelper")
                            loginHelper_installed = result;

                    } else
                        continue;
                }
            } catch { }

            await CheckOnlineVersion();
            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }
        #endregion

        private void ActiveProcessesDblClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (ProcessInfo.SelectedIndex == -1) return;
                int savedIndex = ProcessInfo.SelectedIndex;

                if (!ActiveClientList[savedIndex].PROCESS.HasExited)
                    ActiveClientList[savedIndex].PROCESS.Kill();

                ActiveClientList.RemoveAt(savedIndex);

            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::ActiveProcessesDblClick::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
            }
        }

        private void MouseEnterSetFocus(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                ((ComboBox)sender).Focus();
            }
            catch { }
        }

        private int timerFromSelection()
        {
            int interval;
            switch (ACCOUNT_CONFIG.ACCOUNTS.MEMORY_CLEANER)
            {
                case 1:
                    interval = 1;
                    break;
                case 2:
                    interval = 5;
                    break;
                case 3:
                    interval = 10;
                    break;
                case 4:
                    interval = 15;
                    break;
                case 5:
                    interval = 20;
                    break;
                case 6:
                    interval = 25;
                    break;
                case 7:
                    interval = 30;
                    break;
                case 8:
                    interval = 35;
                    break;
                case 9:
                    interval = 40;
                    break;
                case 10:
                    interval = 45;
                    break;
                case 11:
                    interval = 50;
                    break;
                case 12:
                    interval = 55;
                    break;
                case 13:
                    interval = 60;
                    break;
                default:
                    interval = 10;
                    break;
            }
            return interval;
        }

        private void MemoryCleanerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ACCOUNT_CONFIG.ACCOUNTS.MEMORY_CLEANER = ((ComboBox)sender).SelectedIndex;
            ACCOUNT_CONFIG.appendChangesToConfig();

            if (((ComboBox)sender).SelectedIndex == 0)
            {
                if (memoryTimer.IsEnabled)
                {
                    memoryTimer.IsEnabled = false;
                    memoryTimer.Stop();
                }
                return;
            }

            if (memoryTimer.IsEnabled)
                memoryTimer.Stop();

            memoryTimer.Interval = TimeSpan.FromMinutes(timerFromSelection());
            memoryTimer.IsEnabled = true;
            memoryTimer.Start();
        }

        private void dispatchTimer_tick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            Process[] allProcesses = Process.GetProcessesByName("BNSR");
            if (allProcesses.Count() >= 0)
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        PoormanCleaner.EmptyWorkingSet(process.Handle);
                    }
                    catch { }
                }
            }
        }

        private void removeAccount(object sender, RoutedEventArgs e)
        {
            //Some error checking for retards
            if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
            {
                classLabel.Text = "No account selected, select an account before attemping to delete it!";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            List<ACCOUNT_CONFIG.BNS_SAVED_ACCOUNTS_STRUCT> SAVED_ACCOUNTS = ACCOUNT_CONFIG.ACCOUNTS.Saved;
            int HAS_INDEX = SAVED_ACCOUNTS.FindIndex(x => x.EMAIL == ACCOUNT_LIST_BOX.Text);
            if (HAS_INDEX == -1) return;

            SAVED_ACCOUNTS.RemoveAt(HAS_INDEX);
            ACCOUNT_CONFIG.ACCOUNTS.Saved = SAVED_ACCOUNTS;
            ACCOUNT_CONFIG.appendChangesToConfig();

            ACCOUNT_LIST_BOX.Items.Clear();
            foreach (var account in ACCOUNT_CONFIG.ACCOUNTS.Saved)
                ACCOUNT_LIST_BOX.Items.Add(account.EMAIL);
        }

        private void manualMemoryclean(object sender, RoutedEventArgs e)
        {
            Process[] allProcesses = Process.GetProcessesByName("BNSR");
            if (allProcesses.Count() >= 0)
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        PoormanCleaner.EmptyWorkingSet(process.Handle);
                    }
                    catch { }
                }
            }
        }

        private void isNumericInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void autoCombatRange_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == "")
            {
                classLabel.Text = "The textbox can't be empty";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            Regex regex = new Regex("[^0-9]");
            if (!regex.IsMatch(e.Text))
            {
                classLabel.Text = "Only numeric values!";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            if (int.Parse(e.Text) < 5)
            {
                classLabel.Text = "Value must be greater than 5";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value = e.Text;
            MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
        }

        private void autoCombatRange_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox currentBox = (TextBox)sender;
            if (currentBox.Text == "")
            {
                classLabel.Text = "The autocombat range cannot be empty! Setting default of 30m";
                currentBox.Text = "30";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
            }

            if (int.Parse(currentBox.Text) < 5)
            {
                classLabel.Text = "Value must be greater than 5 for auto combat range! Setting default of 30m";
                currentBox.Text = "30";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
            }

            MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value = currentBox.Text;
            MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
        }

        private void OpenOptionsMenu(object sender, RoutedEventArgs e) => ((Storyboard)FindResource("FadeIn")).Begin(LaunchOptions);

        // This is so bad on so many levels but i don't care, it works as is. Maybe one day i'll use a viewmodel which is the proper way to handle this.
        private void LaunchOptionsClose(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeOut")).Begin(LaunchOptions);

            try
            {
                ACCOUNT_CONFIG.ACCOUNTS.ADDITIONAL_PARAMS = cmdParams.Text;
                ACCOUNT_CONFIG.appendChangesToConfig();

                var dataList = SkillDataGrid.Items.OfType<SkillData>().ToList();
                var nodes = MainWindow.qol_xml.XPathSelectElement("config/gcd/skill");

                XDocument tempdoc = MainWindow.qol_xml;
                tempdoc.Descendants("skill").Where(x => true).Remove();
                var elements = tempdoc.Descendants("gcd").Last();

                foreach (SkillData skill in dataList)
                    elements.Add(
                        new XElement("skill", new XAttribute("id", skill.skillID.ToString()),
                        new XAttribute("value", skill.skillvalue),
                        new XAttribute("mode", skill.mode.ToString()),
                        new XAttribute("description", (skill.description == null) ? "" : skill.description),
                        new XAttribute("recycleTime", skill.recycleTime),
                        new XAttribute("recycleMode", skill.recycleMode.ToString()),
                        new XAttribute("ignoreAutoBias", skill.ignoreAutoBias.ToString())
                    ));

                //tempdoc.XPathSelectElement("config/gcd").Attribute("enable").Value = ((bool)enableGCD.IsChecked) ? "1" : "0";
                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::LaunchOptionsClose::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                var dialog = new ErrorPrompt(String.Format("Error saving to multitool_qol.xml, make sure everything is filled out correctly in the skill section and the file exists in documents.\r\rAddition Info:\n{0}", ex.Message));
                dialog.ShowDialog();
            }
        }

        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
                foreach (ManagementObject mo in processCollection)
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (Exception ex)
            {
                Logger.log.Error("Launcher::KillProcessAndChildrens::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
            }
        }

        private async Task QOL_PLUGIN_CHECK()
        {
            try
            {
                //Can't really do a check if a game instance is already running.
                if (ActiveClientList.Count > 0)
                    return;

                if ((bool)AUTOPATCH_QOL.IsChecked)
                    return;

                string _pluginPath = Path.Combine(plugins_path, "multitool_qol.dll");

                if (MainPage.onlineJson.QOL_HASH == null)
                    throw new Exception("QOL Hash was null");

                if (File.Exists(_pluginPath) && MD5_File(_pluginPath) == MainPage.onlineJson.QOL_HASH.ToLower())
                    return;
                else
                {
                    _progressControl = new ProgressControl();
                    ProgressGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Collapsed;
                    ProgressPanel.Children.Add(_progressControl);

                    var client = new GZipWebClient();

                    try
                    {
                        ProgressControl.updateProgressLabel("Downloading multitool_qol");

                        if (!Directory.Exists(@".\modpolice"))
                            Directory.CreateDirectory(@".\modpolice");

                        if (File.Exists(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE))
                            File.Delete(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE);

                        if (File.Exists(Path.Combine(@".\modpolice\", pluginPath, "multitool_qol.dll")))
                            File.Delete(Path.Combine(@".\modpolice\", pluginPath, "multitool_qol.dll"));

                        await Task.Delay(500);

                        client.DownloadFile(String.Format("{1}/plugin/{0}", MainPage.onlineJson.QOL_ARCHIVE, Globals.MAIN_SERVER_ADDR), @".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE);

                        ProgressControl.updateProgressLabel("Decompressing");
                        ExtractZipFileToDirectory(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE, @".\modpolice", true);
                        await Task.Delay(500);

                        ProgressControl.updateProgressLabel("Installing");
                        if (File.Exists(_pluginPath))
                            File.Delete(_pluginPath);

                        File.Move(Path.Combine(@".\modpolice\", pluginPath, "multitool_qol.dll"), _pluginPath);

                        ProgressControl.updateProgressLabel("Verifying anti-virus didn't clap it");
                        await Task.Delay(3000);

                        if (!File.Exists(_pluginPath))
                        {
                            ProgressControl.updateProgressLabel("multitool_qol.dll is missing");
                            await Task.Delay(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error("Launcher::QOL_PLUGIN_CHECK::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                        ProgressControl.updateProgressLabel(ex.Message);
                        await Task.Delay(5000);
                    }
                    finally
                    {
                        client.Dispose();
                    }

                    ProgressGrid.Visibility = Visibility.Hidden;
                    MainGrid.Visibility = Visibility.Visible;
                    ProgressPanel.Children.Clear();
                    _progressControl = null;
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("{0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void KillAllProcs(object sender, RoutedEventArgs e)
        {
            foreach (Process proc in Process.GetProcessesByName("BNSR"))
                KillProcessAndChildrens(proc.Id);
        }

        private void CloseGcdInfo(object sender, RoutedEventArgs e)
        {
            OptionsInfo.Visibility = Visibility.Hidden;
            MainOptions.Visibility = Visibility.Visible;
        }

        private void ShowGcdInfo(object sender, RoutedEventArgs e)
        {
            OptionsInfo.Visibility = Visibility.Visible;
            MainOptions.Visibility = Visibility.Hidden;
        }

        private void PingReductionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;
            var item = (sender as ComboBox);
            var gcd_node = MainWindow.qol_xml.XPathSelectElement("/config/gcd");
            gcd_node.Attribute("ignorePing").Value = item.SelectedIndex.ToString();
            MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
        }
    }
}
