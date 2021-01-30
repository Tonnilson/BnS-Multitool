using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Net;
using System.IO.Compression;
using System.Threading;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Media.Animation;
using CG.Web.MegaApiClient;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Management;
using System.Globalization;

namespace BnS_Multitool
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
    public partial class Launcher : Page
    {
        public class SESSION_LIST
        {
            public string EMAIL { get; set; }
            public int REGION { get; set; }
            public Process PROCESS { get; set; }
        }

        public static List<SESSION_LIST> ACTIVE_SESSIONS = new List<SESSION_LIST>();
        public static int ACCOUNT_SELECTED_INDEX = -1;
        private static string bin_x86 = Path.Combine(SystemConfig.SYS.BNS_DIR,"bin");
        private static string bin_x64 = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64");
        private static string plugins_x86 = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins");
        private static string plugins_x64 = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins");
        public bool loginHelper_installed = (File.Exists(Path.Combine(plugins_x86,"loginhelper.dll")) && File.Exists(Path.Combine(plugins_x64,"loginhelper.dll")));
        public bool charselect_installed = (File.Exists(Path.Combine(plugins_x86, "charselect.dll")) && File.Exists(Path.Combine(plugins_x64, "charselect.dll")));
        private bool modpolice_installed = false;
        private static string login_xml = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "patches", "use-ingame-login.xml");
        private ProgressControl _progressControl;
        private static BackgroundWorker monitorProcesses = new BackgroundWorker();
        private static bool removalInProgress = false;
        private static DispatcherTimer memoryTimer = new DispatcherTimer();
        private static Modpolice.pluginFileInfo loginhelperPlugin;
        private static Modpolice.pluginFileInfo charselectPlugin;

        public class SkillData
        {
            public int skillID { get; set; }
            public float skillvalue { get; set; }
            public int mode { get; set; }
        }

        private List<SkillData> LoadSkillDataCollection()
        {
            List<SkillData> skillData = new List<SkillData>();

            var nodes = MainWindow.qol_xml.Descendants("skill");

            foreach(var node in nodes)
                skillData.Add(new SkillData()
                {
                    skillID = int.Parse(node.Attribute("id").Value),
                    skillvalue = float.Parse(node.Attribute("value").Value),
                    mode = int.Parse(node.Attribute("mode").Value)
                });

            return skillData;
        }

        public Launcher()
        {
            InitializeComponent();

            //Set our default selections if saved and other misc stuff
            try
            {
                if (ACCOUNT_CONFIG.ACCOUNTS.USE_ALL_CORES == 1)
                    USE_ALL_CORES.IsChecked = true;

                if (ACCOUNT_CONFIG.ACCOUNTS.USE_TEXTURE_STREAMING == 1)
                    NOTEXTURE_STREAMING.IsChecked = true;

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

                if (MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("enable").Value == "1")
                    enableGCD.IsChecked = true;

                if (ACCOUNT_CONFIG.ACCOUNTS.SELECT_LAST_CHAR == 1)
                    useLastChar.IsChecked = true;

                if (ACCOUNT_CONFIG.ACCOUNTS.ADDITIONAL_PARAMS != "")
                    cmdParams.Text = ACCOUNT_CONFIG.ACCOUNTS.ADDITIONAL_PARAMS;

                SkillDataGrid.ItemsSource = LoadSkillDataCollection();

                autoCombatRange.Text = MainWindow.qol_xml.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value;

                BIT_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.CLIENT_BIT;
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

                if (ACCOUNT_SELECTED_INDEX != -1)
                    ACCOUNT_LIST_BOX.SelectedIndex = ACCOUNT_SELECTED_INDEX;

                monitorProcesses.DoWork += new DoWorkEventHandler(monitorActiveProcesses);
            } catch (Exception ex)
            {
                var dialog = new ErrorPrompt("Something went wrong, accounts.json is probably corrupted. Check for syntax errors in accounts.json or delete entirely.\r\rAddition information: \r" + ex.Message);
                dialog.ShowDialog();
                Environment.Exit(0);
            }
        }

        private void monitorActiveProcesses(object sender, DoWorkEventArgs e)
        {
            while(ACTIVE_SESSIONS.Count() > 0)
            {
                try
                {
                    foreach (var session in ACTIVE_SESSIONS.Select((item, index) => new { index, item }).ToList())
                    {
                        if (session.item.PROCESS.HasExited)
                        {
                            ACTIVE_SESSIONS.RemoveAt(session.index);
                            removeFromActiveInvoke(session.index);
                        }
                    }

                    Thread.Sleep(200);
                } catch (Exception)
                {

                }
            }

            //Lets make sure the list is cleared out.
            ProcessInfo.Dispatcher.BeginInvoke(new Action(() =>{ ProcessInfo.Items.Clear(); }));

            if (MainWindow.isMinimized)
                MainWindow.changeWindowState(true);
        }

        private void removeFromActiveInvoke(int i)
        {
            try
            {
                this.ProcessInfo.Dispatcher.Invoke(new Action(() =>
                {
                    if (i >= ProcessInfo.Items.Count - 1)
                        return;

                    this.ProcessInfo.Items.RemoveAt(i);
                }));
            } catch (Exception)
            {

            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Check if loginhelper is installed
            loginHelper_installed = (File.Exists(Path.Combine(plugins_x86,"loginhelper.dll")) && File.Exists(Path.Combine(plugins_x64,"loginhelper.dll")));
            charselect_installed = (File.Exists(Path.Combine(plugins_x86, "charselect.dll")) && File.Exists(Path.Combine(plugins_x64, "charselect.dll")));
            modpolice_installed = (File.Exists(Path.Combine(bin_x86,"winmm.dll")) && File.Exists(Path.Combine(bin_x64,"winmm.dll")) && File.Exists(Path.Combine(plugins_x86,"bnspatch.dll")) && File.Exists(Path.Combine(plugins_x64,"bnspatch.dll")));
            
            Task.Run(new Action(() =>
            {
                 if (ACCOUNT_CONFIG.ACCOUNTS.REGION < 2 || (Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.KR)
                 {
                     Globals.GameVersionCheck();
                     Globals.isLoginAvailable();
                     Application.Current.Dispatcher.Invoke((Action)delegate
                     {
                         Debug.WriteLine("{0}", Globals.onlineBnSVersion);
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

            await Task.Run(async () => await checkOnlineVersion());
        }

        private void saveAccount(object sender, RoutedEventArgs e)
        {
            if(BNS_USERNAME_BOX.Text == "")
            {
                classLabel.Text = "Email field cannot be left blank";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            } else if (BNS_PASSWORD_BOX.Password == "")
            {
                classLabel.Text = "Password field cannot be left blank";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            List<ACCOUNT_CONFIG.BNS_SAVED_ACCOUNTS_STRUCT> SAVED_ACCOUNTS = ACCOUNT_CONFIG.ACCOUNTS.Saved;
            int HAS_INDEX = SAVED_ACCOUNTS.FindIndex(x => x.EMAIL == BNS_USERNAME_BOX.Text);

            if(HAS_INDEX == -1)
            {
                SAVED_ACCOUNTS.Add(new ACCOUNT_CONFIG.BNS_SAVED_ACCOUNTS_STRUCT()
                {
                    EMAIL = BNS_USERNAME_BOX.Text,
                    PASSWORD = BNS_PASSWORD_BOX.Password,
                    PINCODE = BNS_PINCODE_BOX.Text
                });
            } else
            {
                if(BNS_PASSWORD_BOX.Password != "")
                    SAVED_ACCOUNTS[HAS_INDEX].PASSWORD = BNS_PASSWORD_BOX.Password;

                if (BNS_PINCODE_BOX.Text != "")
                    SAVED_ACCOUNTS[HAS_INDEX].PINCODE = BNS_PINCODE_BOX.Text;
            }

            ACCOUNT_CONFIG.ACCOUNTS.Saved = SAVED_ACCOUNTS;
            ACCOUNT_CONFIG.appendChangesToConfig();

            ACCOUNT_LIST_BOX.Items.Clear(); //Flush the list
            foreach (var account in ACCOUNT_CONFIG.ACCOUNTS.Saved)
                ACCOUNT_LIST_BOX.Items.Add(account.EMAIL);
        }

        private string languageFromSelection()
        {
            string lang;
            switch(LANGUAGE_BOX.SelectedIndex)
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

        private async Task launchNewGameClient()
        {
            try
            {
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                int HAS_INDEX = ACTIVE_SESSIONS.FindIndex(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);
                bool is_x86 = (BIT_BOX.SelectedIndex == 0) ? true : false;

                Process proc = new Process();
                if (is_x86)
                    proc.StartInfo.FileName = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "Client.exe");
                else
                    proc.StartInfo.FileName = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "Client.exe");

                //Arugments passed to Client.exe
                if (REGION_BOX.SelectedIndex >= 2)
                {
                    //Other Regions (TW / KR)
                    proc.StartInfo.Arguments = String.Format(@"/sesskey /LaunchByLauncher -unattended {0} {1} {2} {3}",
                      (((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMING " : ""), (((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORES " : ""), ((bool)useLastChar.IsChecked) ? "-CHARLAST" : "", cmdParams.Text);
                } else
                {
                    //NA & EU Region
                    proc.StartInfo.Arguments = String.Format(@"/sesskey /LaunchByLauncher -lang:{0} -region:{1} -unattended {2} {3} {4} {5}",
                        languageFromSelection(), REGION_BOX.SelectedIndex, (((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMING " : ""), (((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORES " : ""),((bool)useLastChar.IsChecked) ? "-CHARLAST" : "", cmdParams.Text);
                }

                //We need to limit the password down to 16 characters as NCSoft password limit is technically 16 and trims off everything after 16, only know of it being a problem in NA/EU not sure about TW
                string pw = REGION_BOX.SelectedIndex != 2 ? ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD.Truncate(16) : ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD;

                //Setup environment variables for loginhelper
                proc.StartInfo.UseShellExecute = false; //Required for setting environment variables to processes
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_USERNAME", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].EMAIL);
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_PASSWORD", pw);
                proc.StartInfo.EnvironmentVariables.Add("BNS_PINCODE", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE);

                if(MainWindow.qol_xml.XPathSelectElements("config/options/option[@enable='1']").Count() > 0 || MainWindow.qol_xml.XPathSelectElement("config/gcd").Attribute("enable").Value == "1")
                    await QOL_PLUGIN_CHECK(); //Check version on launch

                //Check if region is not NA / EU if so check if bnsnogg.dll is installed
                if (REGION_BOX.SelectedIndex >= 2)
                    if (!File.Exists(Path.Combine(is_x86 ? plugins_x86 : plugins_x64, "bnsnogg.dll")))
                        throw new Exception(String.Format("Your region has an anti-cheat and requires bnsnogg.dll, it is missing from {0} version", is_x86 ? "32-bit" : "64-bit"));

                //All checks out, start process
                proc.Start();

                //New code for settings option.
                switch(SystemConfig.SYS.NEW_GAME_OPTION)
                {
                    case 1:
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        break;
                    case 2:
                        MainWindow.changeWindowState(false, true);
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                }

                if (HAS_INDEX == -1)
                {
                    ACTIVE_SESSIONS.Add(new SESSION_LIST() { EMAIL = EMAIL, REGION = REGION_BOX.SelectedIndex, PROCESS = proc });

                    if (ACTIVE_SESSIONS.Count == 1 && !monitorProcesses.IsBusy)
                        monitorProcesses.RunWorkerAsync(); //Start our worker thread.

                    ProcessInfo.Items.Add(String.Format("{0} - {1}", EMAIL, getSelectedRegion(REGION_BOX.SelectedIndex)));
                } 
                else
                    ACTIVE_SESSIONS[HAS_INDEX].PROCESS = proc;

            } catch (Exception ex)
            {
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }

            GC.Collect();
        }

        private static string getSelectedRegion(int id)
        {
            string region;
            switch(id)
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

        private async void launchGameClientClick(object sender, RoutedEventArgs e)
        {
            bool is_x64 = (BIT_BOX.SelectedIndex == 1) ? true : false;

            //Some error checking for retards
            bool loginhelperBit = File.Exists(Path.Combine(is_x64 ? plugins_x64 : plugins_x86, "loginhelper.dll"));
            bool bnspatchBit = File.Exists(Path.Combine(is_x64 ? plugins_x64 : plugins_x86, "bnspatch.dll"));
            bool pluginloaderBit = File.Exists(Path.Combine(is_x64 ? bin_x64 : bin_x86, "winmm.dll"));

            if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
            {
                classLabel.Text = "No account selected, select an account before attemping to launch a new client";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            } else if (!loginhelperBit)
            {
                var dialog = new ErrorPrompt(String.Format("Loginhelper is missing for {0}. Install it for {0} before launching a new client", is_x64 ? "64-bit" : "32-bit"), false, true);
                dialog.ShowDialog();
                return;
            }
            else if (!bnspatchBit)
            {
                var dialog = new ErrorPrompt(String.Format("BNSPatch is missing for {0}. Install it for {0} before launching a new client", is_x64 ? "64-bit" : "32-bit"), false, true);
                dialog.ShowDialog();
                return;
            } else if(!pluginloaderBit)
            {
                var dialog = new ErrorPrompt(String.Format("Pluginloader is missing for {0}. Install it for {0} before launching a new client", is_x64 ? "64-bit" : "32-bit"), false, true);
                dialog.ShowDialog();
                return;
            }

            string EMAIL = ACCOUNT_LIST_BOX.Text;
            int HAS_INDEX = ACTIVE_SESSIONS.FindIndex(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);

            if (HAS_INDEX == -1)
                await launchNewGameClient();
            else
            {
                if (ACTIVE_SESSIONS[HAS_INDEX].PROCESS.HasExited)
                {
                    ACTIVE_SESSIONS.RemoveAt(HAS_INDEX);
                   await launchNewGameClient();
                }
                else
                {
                    ACTIVE_SESSIONS[HAS_INDEX].PROCESS.Kill();
                    await launchNewGameClient();
                }
            }
        }

        private void killGameProcess(object sender, RoutedEventArgs e)
        {
            if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
                return;

            try
            {
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                int HAS_INDEX = ACTIVE_SESSIONS.FindIndex(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);

                if(HAS_INDEX != -1)
                {
                    if(!ACTIVE_SESSIONS[HAS_INDEX].PROCESS.HasExited)
                    {
                        try
                        {
                            ACTIVE_SESSIONS[HAS_INDEX].PROCESS.Kill();
                            ACTIVE_SESSIONS.RemoveAt(HAS_INDEX);
                            ProcessInfo.Items.RemoveAt(HAS_INDEX);
                        }
                        catch (Exception ex)
                        {
                            var dialog = new ErrorPrompt(ex.Message);
                            dialog.ShowDialog();
                        }
                    } else
                    {
                        ACTIVE_SESSIONS.RemoveAt(HAS_INDEX);
                        ProcessInfo.Items.RemoveAt(HAS_INDEX);
                    }
                }
            } catch (Exception ex)
            {
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }
        }

        private void ACCOUNT_LIST_BOX_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ACCOUNT_SELECTED_INDEX = ACCOUNT_LIST_BOX.SelectedIndex;
        }

        private void launchInfoSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void launchInfoCheckStateChanged(object sender, RoutedEventArgs e)
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
            else
            {
                string optionName = currentCheckBox.Name;
                if (currentCheckBox.Name == "autocombatrangeTOS")
                    optionName = "AutoCombat";

                var option_node = MainWindow.qol_xml.XPathSelectElement("/config/options/option[@name='" + optionName + "']");
                if (currentCheckBox.Name == "autocombatrangeTOS")
                    option_node.Attribute("useRange").Value = currentState.ToString();
                else
                    option_node.Attribute("enable").Value = currentState.ToString();

                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            }
        }

        TaskCompletionSource<bool> downloadComplete = new TaskCompletionSource<bool>();

        #region pluginstuff
        private async Task checkOnlineVersion()
        {
            loginhelperPlugin = null;
            if (loginHelper_installed)
                loginhelperPlugin = new Modpolice.pluginFileInfo(Path.Combine(plugins_x86, "loginhelper.dll"));

            charselectPlugin = null;
            if (charselect_installed)
                charselectPlugin = new Modpolice.pluginFileInfo(Path.Combine(plugins_x86, "charselect.dll"));

            //Hotfix for missing use-ingame-login.xml
            if (loginHelper_installed && !File.Exists(login_xml))
                File.WriteAllText(login_xml, Properties.Resources.use_ingame_login);

            Dispatchers.labelContent(loginhelperLocalLbl, String.Format("Current: {0}", (loginhelperPlugin != null) ? loginhelperPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            Dispatchers.labelContent(charSelectLocalLbl, String.Format("Current: {0}", (charselectPlugin != null) ? charselectPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

            try
            {
                var client = new MegaApiClient();
                await client.LoginAnonymousAsync();

                if (!client.IsLoggedIn)
                    throw new Exception("Timed out"); //Throw exception for not logging in anonymously

                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/4EUF2IhL#Ci1Y-sbbyw7nwwMGvHV2_w"));
                INode loginhelper_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("loginhelper")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode charselect_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("charselect")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                if (loginhelper_node != null)
                    Dispatchers.labelContent(loginhelperOnlineLbl, String.Format("Online: {0}", loginhelper_node.ModificationDate.Value.ToString("MM-dd-yy")));

                if (charselect_node != null)
                    Dispatchers.labelContent(charSelectOnlineLbl, String.Format("Online: {0}", charselect_node.ModificationDate.Value.ToString("MM-dd-yy")));
            }
            catch (Exception ex)
            {
                Dispatchers.labelContent(loginhelperOnlineLbl, ex.Message == "Timed out" ? "Online: " + ex.Message : "Online: Error!");
                Dispatchers.labelContent(charSelectOnlineLbl, ex.Message == "Timed out" ? "Online: " + ex.Message : "Online: Error!");
            }
        }

        private async void installLoginHelperClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            string pluginName = "loginhelper";
            if (((Button)sender).Name.Contains("CHARSELECT"))
                pluginName = "charselect";

            await Task.Run(async () =>
            {
                try
                {
                    if (!Directory.Exists("modpolice"))
                        Directory.CreateDirectory("modpolice");

                    ProgressControl.updateProgressLabel("Logging into Mega anonymously...");
                    var client = new MegaApiClient();
                    await client.LoginAnonymousAsync();

                    if (!Directory.Exists("modpolice"))
                        Directory.CreateDirectory("modpolice");

                    ProgressControl.updateProgressLabel("Retrieving file list...");
                    IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/4EUF2IhL#Ci1Y-sbbyw7nwwMGvHV2_w"));

                    INode currentNode = null;
                    IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(String.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));

                    //Find our latest nodes for download
                    INode loginhelper_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains(pluginName)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                    if(loginhelper_node == null)
                    {
                        ProgressControl.errorSadPeepo(Visibility.Visible);
                        ProgressControl.updateProgressLabel("Something went wrong getting the node");
                        await Task.Delay(7000);
                        return;
                    }

                    if (File.Exists(@"modpolice\" + loginhelper_node.Name))
                        File.Delete(@"modpolice\" + loginhelper_node.Name);

                    currentNode = loginhelper_node;
                    await client.DownloadFileAsync(currentNode, @"modpolice\" + loginhelper_node.Name, progress);

                    ProgressControl.updateProgressLabel("Unzipping: " + loginhelper_node.Name);
                    await Task.Delay(750);
                    Modpolice.ExtractZipFileToDirectory(@".\modpolice\" + loginhelper_node.Name, @".\modpolice", true);

                    ProgressControl.updateProgressLabel("Installing " + pluginName + " x86");
                    await Task.Delay(750);

                    if (!Directory.Exists(plugins_x86))
                        Directory.CreateDirectory(plugins_x86);

                    ProgressControl.updateProgressLabel("Installing " + pluginName + " x86");
                    if (File.Exists(Path.Combine(plugins_x86,pluginName + ".dll")))
                        File.Delete(Path.Combine(plugins_x86,pluginName + ".dll"));

                    File.Move(@".\modpolice\bin\plugins\" + pluginName + ".dll", Path.Combine(plugins_x86,pluginName + ".dll"));

                    ProgressControl.updateProgressLabel("Installing " + pluginName + " x64");
                    await Task.Delay(750);

                    if (!Directory.Exists(plugins_x64))
                        Directory.CreateDirectory(plugins_x64);

                    ProgressControl.updateProgressLabel("Installing " + pluginName + " x64");
                    if (File.Exists(Path.Combine(plugins_x64, pluginName + ".dll")))
                        File.Delete(Path.Combine(plugins_x64, pluginName + ".dll"));

                    File.Move(@".\modpolice\bin64\plugins\" + pluginName + ".dll", Path.Combine(plugins_x64,pluginName + ".dll"));

                    if (pluginName == "loginhelper")
                    {
                        ProgressControl.updateProgressLabel("Searching for use-ingame-login.xml");
                        await Task.Delay(750);

                        if (!File.Exists(login_xml))
                        {
                            ProgressControl.updateProgressLabel("patches.xml not found, installing...");
                            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "patches")))
                                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "patches"));

                            File.WriteAllText(login_xml, Properties.Resources.use_ingame_login);
                        }
                    }

                    ProgressControl.updateProgressLabel("All done, just tidying up.");
                    await client.LogoutAsync();
                } catch (Exception ex)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel(ex.Message);
                    await Task.Delay(7000);
                }
            });

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;

            //Check if loginhelper is installed
            loginHelper_installed = (File.Exists(Path.Combine(plugins_x86,pluginName + ".dll")) && File.Exists(Path.Combine(plugins_x64,pluginName + ".dll")));
            charselect_installed = (File.Exists(Path.Combine(plugins_x86, "charselect.dll")) && File.Exists(Path.Combine(plugins_x64, "charselect.dll")));
            await Task.Run(async () => await checkOnlineVersion());
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ProgressControl.updateProgressLabel("Download Completed...");
            downloadComplete.SetResult(true);
        }
        #endregion

        private void ActiveProcessesDblClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProcessInfo.SelectedIndex == -1) return;
            if (removalInProgress) return;

            removalInProgress = true;
            if(!ACTIVE_SESSIONS[ProcessInfo.SelectedIndex].PROCESS.HasExited)
                ACTIVE_SESSIONS[ProcessInfo.SelectedIndex].PROCESS.Kill();
            ACTIVE_SESSIONS.RemoveAt(ProcessInfo.SelectedIndex);
            ProcessInfo.Items.RemoveAt(ProcessInfo.SelectedIndex);
            removalInProgress = false;
        }

        private void MouseEnterSetFocus(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                ((ComboBox)sender).Focus();
            } catch  (Exception) { }
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
            Debug.WriteLine("Cleaning Memory...");
            Process[] allProcesses = Process.GetProcessesByName("Client");
            if (allProcesses.Count() >= 0)
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        PoormanCleaner.EmptyWorkingSet(process.Handle);
                    }
                    catch (Exception) { }
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
            Process[] allProcesses = Process.GetProcessesByName("Client");
            if (allProcesses.Count() >= 0)
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        PoormanCleaner.EmptyWorkingSet(process.Handle);
                    }
                    catch (Exception) { }
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
        
        //This is so bad on so many levels but i don't care, it works as is. Maybe one day i'll use a viewmodel which is the proper way to handle this.
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
                    elements.Add(new XElement("skill", new XAttribute("id", skill.skillID.ToString()), new XAttribute("value", skill.skillvalue), new XAttribute("mode", skill.mode.ToString())));

                //tempdoc.XPathSelectElement("config/gcd").Attribute("enable").Value = ((bool)enableGCD.IsChecked) ? "1" : "0";
                MainWindow.qol_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));

            } catch(Exception ex)
            {
                var dialog = new ErrorPrompt(String.Format("Error saving to multitool_qol.xml, make sure everything is filled out correctly in the skill section and the file exists in documents.\r\rAddition Info:\n{0}", ex.Message));
                dialog.ShowDialog();
            }
        }

        static string CalculateMD5(string fileName)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
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
            catch (ArgumentException)
            {
            }
        }

        private async Task QOL_PLUGIN_CHECK()
        {
            //Can't really do a check if a game instance is already running.
            if (ACTIVE_SESSIONS.Count > 0)
                return;

            string x86Path = Path.Combine(plugins_x86, "multitool_qol.dll");
            string x64Path = Path.Combine(plugins_x64, "multitool_qol.dll");

            if ((File.Exists(x64Path) && File.Exists(x86Path)) && CalculateMD5(x86Path) == MainPage.onlineJson.QOL_HASH[0].ToLower() && CalculateMD5(x64Path) == MainPage.onlineJson.QOL_HASH[1].ToLower())
                return;
            else
            {
                _progressControl = new ProgressControl();
                ProgressGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Collapsed;
                ProgressPanel.Children.Add(_progressControl);

                System.Net.WebClient client = new System.Net.WebClient();

                try
                {
                    ProgressControl.updateProgressLabel("Downloading multitool_qol");

                    if (!Directory.Exists(@".\modpolice"))
                        Directory.CreateDirectory(@".\modpolice");

                    if (File.Exists(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE))
                        File.Delete(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE);

                    if (File.Exists(@".\modpolice\bin\plugins\multitool_qol.dll"))
                        File.Delete(@".\modpolice\bin\plugins\multitool_qol.dll");
                    if (File.Exists(@".\modpolice\bin64\plugins\multitool_qol.dll"))
                        File.Delete(@".\modpolice\bin64\plugins\multitool_qol.dll");

                    await Task.Delay(500);

                    client.DownloadFile(String.Format("http://tonic.pw/files/bnsmultitool/{0}", MainPage.onlineJson.QOL_ARCHIVE), @".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE);

                    ProgressControl.updateProgressLabel("Decompressing");
                    Modpolice.ExtractZipFileToDirectory(@".\modpolice\" + MainPage.onlineJson.QOL_ARCHIVE, @".\modpolice", true);
                    await Task.Delay(500);

                    ProgressControl.updateProgressLabel("Installing");
                    if (File.Exists(x86Path))
                        File.Delete(x86Path);
                    if (File.Exists(x64Path))
                        File.Delete(x64Path);

                    File.Move(@".\modpolice\bin\plugins\multitool_qol.dll", x86Path);
                    File.Move(@".\modpolice\bin64\plugins\multitool_qol.dll", x64Path);

                    ProgressControl.updateProgressLabel("Verifying anti-virus didn't clap it");
                    await Task.Delay(3000);

                    if(!File.Exists(x86Path) || !File.Exists(x64Path))
                    {
                        ProgressControl.updateProgressLabel(String.Format("multitool_qol.dll missing {0} {1}", !File.Exists(x86Path) ? "x86 " : "", !File.Exists(x64Path) ? "x64" : ""));
                        await Task.Delay(5000);
                    }

                } catch (Exception ex)
                {
                    Debug.WriteLine("{0}", ex.Message);
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

        private void KillAllProcs(object sender, RoutedEventArgs e)
        {
            ACTIVE_SESSIONS.Clear();
            foreach (Process proc in Process.GetProcessesByName("Client"))
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
    }
}
