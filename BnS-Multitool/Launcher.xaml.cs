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
        private static string bin_x86 = SystemConfig.SYS.BNS_DIR + @"\bin\";
        private static string bin_x64 = SystemConfig.SYS.BNS_DIR + @"\bin64\";
        private static string plugins_x86 = SystemConfig.SYS.BNS_DIR + @"\bin\plugins\";
        private static string plugins_x64 = SystemConfig.SYS.BNS_DIR + @"\bin64\plugins\";
        public bool loginHelper_installed = (File.Exists(plugins_x86 + "loginhelper.dll") && File.Exists(plugins_x64 + "loginhelper.dll"));
        private bool modpolice_installed = false;
        private static string login_xml = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\patches\use-ingame-login.xml";
        private ProgressControl _progressControl;
        private static BackgroundWorker monitorProcesses = new BackgroundWorker();
        private static bool removalInProgress = false;
        private static DispatcherTimer memoryTimer = new DispatcherTimer();

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

                if(ACCOUNT_CONFIG.ACCOUNTS.TOS_AUTO_BAIT == 1)
                {
                    BaitBasketToS.IsChecked = true;
                    ToS_Button.Visibility = Visibility.Hidden;
                    ToSPicture.Visibility = Visibility.Hidden;
                }

                if (ACCOUNT_CONFIG.ACCOUNTS.TOS_RAISE_CAP == 1)
                {
                    RaiseCapToS.IsChecked = true;
                    ToS_Button.Visibility = Visibility.Hidden;
                    ToSPicture.Visibility = Visibility.Hidden;
                }

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
            do
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
                        Thread.Sleep(500);
                    }
                } catch (Exception ex)
                {
                    var dialog = new ErrorPrompt(ex.Message);
                    dialog.ShowDialog();
                }
            } while (ACTIVE_SESSIONS.Count > 0);

            if (MainWindow.isMinimized)
                MainWindow.changeWindowState(true);
        }

        private void removeFromActiveInvoke(int i)
        {
            this.ProcessInfo.Dispatcher.Invoke(new Action(() =>
            {
                this.ProcessInfo.Items.RemoveAt(i);
            }));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Check if loginhelper is installed
            loginHelper_installed = (File.Exists(plugins_x86 + "loginhelper.dll") && File.Exists(plugins_x64 + "loginhelper.dll"));
            modpolice_installed = (File.Exists(bin_x86 + "winmm.dll") && File.Exists(bin_x64 + "winmm.dll") && File.Exists(plugins_x86 + "bnspatch.dll") && File.Exists(plugins_x64 + "bnspatch.dll"));

            if (loginHelper_installed)
            {
                LoginHelper_Lbl.Text = "Installed: Yes";
                LoginHelper_Lbl.Foreground = System.Windows.Media.Brushes.Green;
                //LOGINHELPER_INSTALL.IsEnabled = false;
            }
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

        private void launchNewGameClient()
        {
            try
            {
                string EMAIL = ACCOUNT_LIST_BOX.Text;
                int HAS_INDEX = ACTIVE_SESSIONS.FindIndex(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);
                bool is_x86 = (BIT_BOX.SelectedIndex == 0) ? true : false;

                Process proc = new Process();
                if (is_x86)
                    proc.StartInfo.FileName = SystemConfig.SYS.BNS_DIR + @"\bin\Client.exe";
                else
                    proc.StartInfo.FileName = SystemConfig.SYS.BNS_DIR + @"\bin64\Client.exe";

                //Arugments passed to Client.exe
                if (REGION_BOX.SelectedIndex == 2)
                {
                    //TW version, This shit is a mess I'll clean it up if i add more regions
                    proc.StartInfo.Arguments = String.Format(@"/sesskey /LaunchByLauncher -unattended {0} {1}",
                       (((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMIN " : ""), (((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORE " : ""));
                } else
                {
                    //NA & EU Region
                    proc.StartInfo.Arguments = String.Format(@"/sesskey /LaunchByLauncher -lang:{0} -region:{1} -unattended {2} {3}",
                        languageFromSelection(), REGION_BOX.SelectedIndex, (((bool)NOTEXTURE_STREAMING.IsChecked) ? "-NOTEXTURESTREAMIN " : ""), (((bool)USE_ALL_CORES.IsChecked) ? "-USEALLAVAILABLECORE " : ""));
                }

                //We need to limit the password down to 16 characters as NCSoft password limit is technically 16 and trims off everything after 16, only know of it being a problem in NA/EU not sure about TW
                string pw = REGION_BOX.SelectedIndex != 2 ? ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD.Truncate(16) : ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PASSWORD;

                //Setup environment variables for loginhelper
                proc.StartInfo.UseShellExecute = false; //Required for setting environment variables to processes
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_USERNAME", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].EMAIL);
                proc.StartInfo.EnvironmentVariables.Add("BNS_PROFILE_PASSWORD", pw);
                proc.StartInfo.EnvironmentVariables.Add("BNS_PINCODE", ACCOUNT_CONFIG.ACCOUNTS.Saved[ACCOUNT_SELECTED_INDEX].PINCODE);
                proc.StartInfo.EnvironmentVariables.Add("AUTO_BAIT", ((bool)BaitBasketToS.IsChecked) ? "1" : "0");
                proc.StartInfo.EnvironmentVariables.Add("RAISE_LIMIT", ((bool)RaiseCapToS.IsChecked) ? "1" : "0");
                proc.StartInfo.EnvironmentVariables.Add("MP_CROSS", ((bool)marketplace_TOS.IsChecked) ? "1" : "0");
                proc.Start();

                if ((bool)BaitBasketToS.IsChecked || (bool)RaiseCapToS.IsChecked || (bool)marketplace_TOS.IsChecked)
                {
                    //We need this check for TW because it has GameGuard, need to make sure they have bnsnogg plugin by Pilao
                    if (REGION_BOX.SelectedIndex == 2 && (!File.Exists(plugins_x86 + @"bnsnogg.dll") && !File.Exists(plugins_x64 + "bnsnogg.dll")))
                    {
                        var dialog = new ErrorPrompt("You have the TW version selected and we cannot detect bnsnogg.dll, install bnsnogg before using the QoL options");
                        dialog.ShowDialog();
                    }
                    else
                    {
                        if (is_x86)
                        {
                            try
                            {
                                File.WriteAllBytes(Path.GetTempPath() + @"\BnS-ToS-x86.exe", Properties.Resources.BnS_ToS_x86);
                            }
                            catch (Exception)
                            {

                            }

                            ProcessStartInfo tosproc = new ProcessStartInfo();
                            tosproc.FileName = Path.GetTempPath() + @"\BnS-ToS-x86.exe";
                            tosproc.Arguments = proc.Id.ToString();
                            tosproc.Verb = "runas";
                            Process.Start(tosproc);
                        }
                        else
                        {
                            try
                            {
                                File.WriteAllBytes(Path.GetTempPath() + @"\BnS-ToS-x64.exe", Properties.Resources.BnS_ToS_x64);
                            }
                            catch (Exception)
                            {

                            }

                            ProcessStartInfo tosproc = new ProcessStartInfo();
                            tosproc.FileName = Path.GetTempPath() + @"\BnS-ToS-x64.exe";
                            tosproc.Arguments = proc.Id.ToString();
                            tosproc.Verb = "runas";
                            Process.Start(tosproc);
                        }
                    }
                }

                if (HAS_INDEX == -1)
                {
                    ACTIVE_SESSIONS.Add(new SESSION_LIST() { EMAIL = EMAIL, REGION = REGION_BOX.SelectedIndex, PROCESS = proc });

                    if (ACTIVE_SESSIONS.Count == 1 && !monitorProcesses.IsBusy)
                        monitorProcesses.RunWorkerAsync(); //Start our worker thread.

                    ProcessInfo.Items.Add(String.Format("{0} - {1}", EMAIL, getSelectedRegion(REGION_BOX.SelectedIndex)));
                } else
                {
                    ACTIVE_SESSIONS[HAS_INDEX].PROCESS = proc;
                }
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

        private void launchGameClientClick(object sender, RoutedEventArgs e)
        {
            //Some error checking for retards
            if(!loginHelper_installed)
            {
                classLabel.Text = "Loginhelper is not installed on either 32-bit or 64-bit. Both need to be installed before launching a new client";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            } else if (ACCOUNT_LIST_BOX.SelectedIndex == -1)
            {
                classLabel.Text = "No account selected, select an account before attemping to launch a new client";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }
            else if (!modpolice_installed)
            {
                var dialog = new ErrorPrompt("Pluginloader / BNSPatch is missing from either 32 or 64-bit clients. Install these before launching a new client.");
                dialog.ShowDialog();
                return;
            }

            string EMAIL = ACCOUNT_LIST_BOX.Text;
            int HAS_INDEX = ACTIVE_SESSIONS.FindIndex(x => x.EMAIL == EMAIL && x.REGION == REGION_BOX.SelectedIndex);

            if (HAS_INDEX == -1)
                launchNewGameClient();
            else
            {
                if (ACTIVE_SESSIONS[HAS_INDEX].PROCESS.HasExited)
                {
                    ACTIVE_SESSIONS.RemoveAt(HAS_INDEX);
                    launchNewGameClient();
                }
                else
                {
                    ACTIVE_SESSIONS[HAS_INDEX].PROCESS.Kill();
                    launchNewGameClient();
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

            Debug.WriteLine(String.Format("Check State: {0}", currentState));

            if (currentCheckBox.Name == "NOTEXTURE_STREAMING")
                ACCOUNT_CONFIG.ACCOUNTS.USE_TEXTURE_STREAMING = currentState;
            else if (currentCheckBox.Name == "USE_ALL_CORES")
                ACCOUNT_CONFIG.ACCOUNTS.USE_ALL_CORES = currentState;
            else if (currentCheckBox.Name == "BaitBasketToS")
                ACCOUNT_CONFIG.ACCOUNTS.TOS_AUTO_BAIT = currentState;
            else
                ACCOUNT_CONFIG.ACCOUNTS.TOS_RAISE_CAP = currentState;

            ACCOUNT_CONFIG.appendChangesToConfig();
        }

        TaskCompletionSource<bool> downloadComplete = new TaskCompletionSource<bool>();

        private async void installLoginHelperClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

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
                    INode loginhelper_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("loginhelper")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

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

                    ProgressControl.updateProgressLabel("Installing loginhelper x86");
                    await Task.Delay(750);

                    if (!Directory.Exists(plugins_x86))
                        Directory.CreateDirectory(plugins_x86);

                    ProgressControl.updateProgressLabel("Installing loginhelper x86");
                    if (File.Exists(plugins_x86 + "loginhelper.dll"))
                        File.Delete(plugins_x86 + "loginhelper.dll");

                    File.Move(@".\modpolice\bin\plugins\loginhelper.dll", plugins_x86 + "loginhelper.dll");

                    ProgressControl.updateProgressLabel("Installing loginhelper x64");
                    await Task.Delay(750);

                    if (!Directory.Exists(plugins_x64))
                        Directory.CreateDirectory(plugins_x64);

                    ProgressControl.updateProgressLabel("Installing loginhelper x64");
                    if (File.Exists(plugins_x64 + "loginhelper.dll"))
                        File.Delete(plugins_x64 + "loginhelper.dll");

                    File.Move(@".\modpolice\bin64\plugins\loginhelper.dll", plugins_x64 + "loginhelper.dll");

                    ProgressControl.updateProgressLabel("Searching for use-ingame-login.xml");
                    await Task.Delay(750);

                    if (!File.Exists(login_xml))
                    {
                        ProgressControl.updateProgressLabel("patches.xml not found, installing...");
                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\patches"))
                            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\patches");

                        File.WriteAllText(login_xml, Properties.Resources.use_ingame_login);
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
            loginHelper_installed = (File.Exists(plugins_x86 + "loginhelper.dll") && File.Exists(plugins_x64 + "loginhelper.dll"));
            if (loginHelper_installed)
            {
                LoginHelper_Lbl.Text = "Installed: Yes";
                LoginHelper_Lbl.Foreground = System.Windows.Media.Brushes.Green;
                //LOGINHELPER_INSTALL.IsEnabled = false;
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressControl.updateProgressLabel(String.Format("Downloading loginhelper_2020.07.18.zip ({0}%)", e.ProgressPercentage));
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ProgressControl.updateProgressLabel("Download Completed...");
            downloadComplete.SetResult(true);
        }

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

        private void ToSBtn(object sender, RoutedEventArgs e)
        {
            ToS_Button.Visibility = Visibility.Hidden;
            ToSPicture.Visibility = Visibility.Hidden;
        }
    }
}
