using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using System.Xml.XPath;
using ToggleSwitch;
using System.Windows.Input;
using static BnS_Multitool.Functions.FileExtraction;
using System.Text.RegularExpressions;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Effects.xaml
    /// </summary>
    /// 

    public enum EUI_Slot { None, Buff, Debuff, System, Longterm, BuffDisable };
    public enum EUI_Category { None, Attraction, ItemEvent, CombatCommon, CombatClass, Skill };

    public class EffectsList
    {
        public string Alias { get; set; }
        public EUI_Slot UI_Slot { get; set; }
        public EUI_Category UI_Category { get; set; }
    }
    public partial class Effects : Page
    {
        private ProgressControl _progressControl;
        private static bool _isInitialized = false; //logic for toggles cause there isn't a fucking click event
        private static string removalDirectory = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Removes");
        private static string removalPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Removes");

        public class toggleStruct
        {
            public string className { get; set; }
            public HorizontalToggleSwitch animToggle { get; set; }
            public HorizontalToggleSwitch fxToggle { get; set; }
        }

        public class ConsoleCommands
        {
            public string command { get; set; }
        }

        

        private static List<toggleStruct> systemToggles;

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        private bool DoesFilesExist(string className, bool section)
        {
            //true for effects, false for animations
            if (section)
                return SystemConfig.SYS.CLASS.Where(x => x.CLASS == className).SelectMany(upk => upk.EFFECTS).Any(file => File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content","Paks","Removes", file)));
            else
                return SystemConfig.SYS.CLASS.Where(x => x.CLASS == className).SelectMany(upk => upk.ANIMATIONS).Any(file => File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Removes", file)));
        }

        public Effects()
        {
            InitializeComponent();
            systemToggles = new List<toggleStruct>()
            {
                new toggleStruct() {className = "Assassin", animToggle = sin_anim_toggle, fxToggle = sin_fx_toggle},
                new toggleStruct() {className = "Summoner", animToggle = sum_anim_toggle, fxToggle = sum_fx_toggle},
                new toggleStruct() {className = "KungFuMaster", animToggle = kfm_anim_toggle, fxToggle = kfm_fx_toggle},
                new toggleStruct() {className = "Gunslinger", animToggle = gun_anim_toggle, fxToggle = gun_fx_toggle},
                new toggleStruct() {className = "Destroyer", animToggle = des_anim_toggle, fxToggle = des_fx_toggle},
                new toggleStruct() {className = "Forcemaster", animToggle = fm_anim_toggle, fxToggle = fm_fx_toggle},
                new toggleStruct() {className = "Soulfighter", animToggle = sf_anim_toggle, fxToggle = sf_fx_toggle},
                new toggleStruct() {className = "Blademaster", animToggle = bm_anim_toggle, fxToggle = bm_fx_toggle},
                new toggleStruct() {className = "Bladedancer", animToggle = bd_anim_toggle, fxToggle = bd_fx_toggle},
                new toggleStruct() {className = "Astromancer", animToggle = astro_anim_toggle, fxToggle = astro_fx_toggle},
                new toggleStruct() {className = "Warlock", animToggle = wl_anim_toggle, fxToggle = wl_fx_toggle},
                new toggleStruct() {className = "Warden", animToggle = wd_anim_toggle, fxToggle = wd_fx_toggle},
                new toggleStruct() {className = "Archer", animToggle = arc_anim_toggle, fxToggle = arc_fx_toggle},
                new toggleStruct() {className = "Dualblade", animToggle = dualblade_anim_toggle, fxToggle = dualblade_fx_toggle}
            };
        }

        public static void ExtractPakFiles()
        {
            try
            {
                // Cleanup the removal directory incase we change file names later on.
                if (Directory.Exists(removalPath))
                    Directory.Delete(removalPath, true);

                using (var memoryStream = new MemoryStream(Properties.Resources.class_removes))
                    ExtractZipFileToDirectory(memoryStream, removalDirectory, true, true);
            } catch (Exception ex)
            {
                Logger.log.Error("Effects::ExtractPackFiles\n{0}", ex.ToString());
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content")))
                {
                    removalDirectory = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Removes");
                    removalPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Removes");

                    if (!Directory.Exists(removalPath)) Directory.CreateDirectory(removalPath);
                    if (!Directory.Exists(removalDirectory))
                    {
                        Directory.CreateDirectory(removalDirectory);
                        ExtractPakFiles();
                    }

                    if (Directory.GetFiles(removalDirectory).Length == 0)
                        ExtractPakFiles();
                }

                refreshToggles();

                if (Directory.GetFiles(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "plugins"), "extendedoptions.dll").FirstOrDefault() != null)
                    ExtendedOptionsBtn.IsEnabled = true;
                else
                    ExtendedOptionsBtn.IsEnabled = false;

                GC.Collect();
            }
            catch (Exception ex)
            {
                new ErrorPrompt("There was an error, it has been logged in the log file.").ShowDialog();
                Logger.log.Error("Effects::Page_Loaded\n{0}", ex.ToString());
            }
        }

        private void refreshToggles()
        {
            _isInitialized = false;
            //Loop through individual classes and check if their animation upk's and effect upk's exist, if so flip their toggles to on.
            foreach (var classData in SystemConfig.SYS.CLASS)
            {
                var curClass = systemToggles.Where(x => x.className == classData.CLASS).FirstOrDefault();

                curClass.fxToggle.IsChecked = !DoesFilesExist(classData.CLASS, true);
                curClass.animToggle.IsChecked = !DoesFilesExist(classData.CLASS, false);
            }

            _isInitialized = true;
        }

        private async void handleToggleChange(object sender, RoutedEventArgs e)
        {
            try
            {
                HorizontalToggleSwitch currentToggle = (HorizontalToggleSwitch)sender;

                if (!_isInitialized)
                    return;

                if (Process.GetProcessesByName("BNSR").Where(proc => !proc.HasExited).Count() >= 1)
                {
                    peepoWtfText.Text = "You can't do that when Blade & Soul is already running!";
                    ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                    return;
                }

                string sourceDirectory;
                string destinationDirectory;

                //Check if it is an individual class toggle
                if (systemToggles.Any(toggle => toggle.animToggle == currentToggle || toggle.fxToggle == currentToggle))
                {
                    string[] upkfiles;
                    toggleStruct sysToggle;

                    if (currentToggle.Name.Contains("_fx_"))
                    {
                        sysToggle = systemToggles.Where(x => x.fxToggle.Name == currentToggle.Name).FirstOrDefault();
                        upkfiles = SystemConfig.SYS.CLASS.Where(x => x.CLASS == sysToggle.className).Select(upk => upk.EFFECTS).FirstOrDefault();
                    }
                    else
                    {
                        sysToggle = systemToggles.Where(x => x.animToggle.Name == currentToggle.Name).FirstOrDefault();
                        upkfiles = SystemConfig.SYS.CLASS.Where(x => x.CLASS == sysToggle.className).Select(upk => upk.ANIMATIONS).FirstOrDefault();
                    }

                    sourceDirectory = removalPath;
                    destinationDirectory = removalDirectory;


                    // _isInitialized = false;
                    if (upkfiles.Count() > 0)
                    {
                        foreach (string file in upkfiles)
                        {
                            try
                            {
                                //Move our target file to our new destination
                                if (File.Exists(Path.Combine(destinationDirectory, file)))
                                {
                                    if(!File.Exists(Path.Combine(sourceDirectory, file)))
                                    {
                                        CreateSymbolicLink(Path.Combine(sourceDirectory, file), Path.Combine(destinationDirectory, file), SymbolicLink.File);
                                        CreateSymbolicLink(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), Path.Combine(destinationDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), SymbolicLink.File);
                                    } else
                                    {
                                        if (File.Exists(Path.Combine(sourceDirectory, file))) File.Delete(Path.Combine(sourceDirectory, file));
                                        if (File.Exists(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))))) File.Delete(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.log.Error("Effects::handleToggleChange\n{0}", ex.ToString());
                                //ProgressControl.updateProgressLabel(ex.Message);
                            }
                        }
                    }

                    //_isInitialized = true;
                    Dispatchers.labelContent(classLabel, String.Format("{0} {1}", sysToggle.className, (currentToggle.IsChecked) ? "Restored" : "Removed"));
                    ((Storyboard)FindResource("animate")).Begin(classLabel);
                    ((Storyboard)FindResource("animate")).Begin(successStatePicture);
                }
                else
                {
                    string[] upkFiles;
                    upkFiles = new string[] { };

                    _progressControl = new ProgressControl();
                    MainWindow.mainWindowFrame.RemoveBackEntry();
                    ProgressGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Collapsed;

                    ProgressPanel.Children.Add(_progressControl);

                    //Turning whatever the fuck it is on
                    if (currentToggle.IsChecked)
                    {
                        ProgressControl.errorSadPeepo(Visibility.Hidden);
                        ProgressControl.updateProgressLabel("Enabling Effects");
                        await Task.Delay(150);
                    }
                    else
                    {
                        ProgressControl.errorSadPeepo(Visibility.Hidden);
                        ProgressControl.updateProgressLabel("Removing Effects");
                        await Task.Delay(150);
                    }

                    sourceDirectory = removalPath;
                    destinationDirectory = removalDirectory;

                    if (upkFiles.Count() > 0)
                    {
                        foreach (string file in upkFiles)
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(destinationDirectory, file)))
                                {
                                    if (!File.Exists(Path.Combine(sourceDirectory, file)))
                                    {
                                        CreateSymbolicLink(Path.Combine(sourceDirectory, file), Path.Combine(destinationDirectory, file), SymbolicLink.File);
                                        CreateSymbolicLink(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), Path.Combine(destinationDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), SymbolicLink.File);
                                    }
                                    else
                                    {
                                        if (File.Exists(Path.Combine(sourceDirectory, file))) File.Delete(Path.Combine(sourceDirectory, file));
                                        if (File.Exists(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))))) File.Delete(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.log.Error("Effects::handleToggleChange\n{0}", ex.ToString());
                                ProgressControl.updateProgressLabel(ex.Message);
                                await Task.Delay(500);
                            }
                            await Task.Delay(50);
                        }
                    }

                    ProgressGrid.Visibility = Visibility.Hidden;
                    MainGrid.Visibility = Visibility.Visible;
                    ProgressPanel.Children.Clear();
                    _progressControl = null;
                }
            } catch (Exception ex)
            {
                Logger.log.Error("Effects::handleToggleChange\n{0}", ex.ToString());
                var dialog = new ErrorPrompt("Something went wrong, \r\rAddition information: \r" + ex.Message);
                dialog.ShowDialog();
            }

            GC.WaitForPendingFinalizers();
        }

        private async void handleToggle(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            if (Process.GetProcessesByName("Client").Length >= 1)
            {
                peepoWtfText.Text = "You can't do that when Blade & Soul is already running!";
                ((Storyboard)FindResource("animate")).Begin(ErrorPromptGrid);
                return;
            }

            Button currentToggle = (Button)sender;
            string[] upkFiles;
            string sourceDirectory;
            string destinationDirectory;

            bool currentState = currentToggle.Name.Contains("_on") ? true : false;

            if (currentToggle.Name.Contains("animation"))
            {
                Debug.WriteLine("Animation Toggle");
                _isInitialized = false;
                upkFiles = SystemConfig.SYS.CLASS.SelectMany(entries => entries.ANIMATIONS).ToArray();
                foreach (var fuckass in systemToggles)
                    fuckass.animToggle.IsChecked = currentState;
                _isInitialized = true;
            }
            else if (currentToggle.Name.Contains("effect"))
            {
                Debug.WriteLine("effect Toggle");
                _isInitialized = false;
                upkFiles = SystemConfig.SYS.CLASS.SelectMany(entries => entries.EFFECTS).ToArray();
                foreach (var fuckass in systemToggles)
                    fuckass.fxToggle.IsChecked = currentState;
                _isInitialized = true;
            }
            else
            {
                _isInitialized = false;

                //Put all animations and stuff into one big array, I'm sure there is a better way to do this but it's 6am and I can't be bothered to think efficient.
                upkFiles = SystemConfig.SYS.CLASS.SelectMany(entries => entries.ANIMATIONS).ToArray();
                upkFiles = upkFiles.Concat(SystemConfig.SYS.CLASS.SelectMany(entries => entries.EFFECTS)).ToArray();
                //upkFiles =  upkFiles.Concat(SystemConfig.SYS.MAIN_UPKS).ToArray();

                foreach (var fuckass in systemToggles)
                {
                    fuckass.animToggle.IsChecked = currentState;
                    fuckass.fxToggle.IsChecked = currentState;
                }

                otherEffectsToggle.IsChecked = currentState;
                _isInitialized = true;
            }

            _progressControl = new ProgressControl();
            MainWindow.mainWindowFrame.RemoveBackEntry();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;

            ProgressPanel.Children.Add(_progressControl);

            //Turning whatever the fuck it is on
            if (currentState)
            {
                ProgressControl.errorSadPeepo(Visibility.Hidden);
                ProgressControl.updateProgressLabel("Enabling");
                await Task.Delay(150);
            }
            else
            {
                ProgressControl.errorSadPeepo(Visibility.Hidden);
                ProgressControl.updateProgressLabel("Disabling");
                await Task.Delay(150);
            }

            sourceDirectory = removalPath;
            destinationDirectory = removalDirectory;

            if (upkFiles.Count() > 0)
            {
                foreach (string file in upkFiles)
                {
                    try
                    {
                        ProgressControl.updateProgressLabel(String.Format("Checking for {0}", file));
                        await Task.Delay(25);
                        //Move our target file to our new destination
                        if (File.Exists(Path.Combine(destinationDirectory, file)))
                        {
                            if (!File.Exists(Path.Combine(sourceDirectory, file)) && !currentState)
                            {
                                CreateSymbolicLink(Path.Combine(sourceDirectory, file), Path.Combine(destinationDirectory, file), SymbolicLink.File);
                                CreateSymbolicLink(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), Path.Combine(destinationDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))), SymbolicLink.File);
                            }
                            else if(currentState)
                            {
                                if (File.Exists(Path.Combine(sourceDirectory, file)))
                                    File.Delete(Path.Combine(sourceDirectory, file));
                                if (File.Exists(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file)))))
                                    File.Delete(Path.Combine(sourceDirectory, string.Format("{0}.sig", Path.GetFileNameWithoutExtension(file))));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error("Effects::handleToggle\n{0}", ex.ToString());
                        ProgressControl.updateProgressLabel(ex.Message);
                        await Task.Delay(500);
                    }
                    await Task.Delay(50);
                }
            }

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }

        private string[] options = { 
            "PlayerHighEmitter", "PlayerMidEmitter", "PlayerLowEmitter", "PlayerJewelEffect", "PlayerImmuneEffect", "PlayerCharLOD", "PlayerPhysics", "PlayerParticleLight",
            "PcHighEmitter", "PcMidEmitter", "PcLowEmitter", "PcJewelEffect", "PcImmuneEffect", "PcCharLOD", "PcPhysics", "PcParticleLight",
            "NpcHighEmitter", "NpcMidEmitter", "NpcLowEmitter", "NpcJewelEffect", "NpcImmuneEffect", "NpcCharLOD", "NpcPhysics", "NpcParticleLight",
            "BackHighEmitter", "BackMidEmitter", "BackLowEmitter", "BackParticleLight"
        };

        private void ExtendedOptions_Click(object sender, RoutedEventArgs e)
        {
            Active_Profile.SelectedIndex = 0;
            MainGrid.Visibility=Visibility.Collapsed;
            ExtendedOptionsGrid.Visibility=Visibility.Visible;
        }

        private void CloseExtendedOptions(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Visible;
            ExtendedOptionsGrid.Visibility = Visibility.Collapsed;
        }

        int curProfKey;

        private void SaveCurrentProfile(object sender, RoutedEventArgs e)
        {
            var selection = Active_Profile.SelectedIndex;

            foreach (var option in options) {
                var checkbox = (CheckBox)this.FindName(option);
                if (checkbox == null) continue;
                MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/video_options/option[@name='{1}']", selection, option)).Attribute("enable").Value = (bool)checkbox.IsChecked ? "1" : "0";
            }
            // Phantom
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/phantom", selection)).Attribute("enable").Value = (bool)showPhantom.IsChecked ? "1" : "0";

            // Damage Font stuff
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection)).Attribute("scale").Value = signalInfo_Scale.Text;
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection)).Attribute("wordspacing").Value = signalInfo_Spacing.Text;
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection)).Attribute("sgt_hit_enemy").Value = (bool)sgt_hit_enemy.IsChecked ? "0" : "1";
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection)).Attribute("sgt_crithit_enemy").Value = (bool)sgt_crithit_enemy.IsChecked ? "0" : "1";
            MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection)).Attribute("sgt_bighit_enemy").Value = (bool)sgt_bighit_enemy.IsChecked ? "0" : "1";

            // Console Commands
            var dataList = ConsoleCmds.Items.OfType<ConsoleCommands>().ToList();
            var cmds = MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/console_cmds", selection));
            cmds.Descendants().Where(x => true).Remove();
            foreach (var cmd in ConsoleCmds.Items.OfType<ConsoleCommands>().ToList())
                cmds.Add(new XElement("cmd", new XAttribute("run", cmd.command)));

            // Hotkey stuff
            XElement hotkey;
            if (selection == 0)
                hotkey = MainWindow.extended_xml.XPathSelectElement("config/options/option[@name='reloadKey']");
            else
                hotkey = MainWindow.extended_xml.XPathSelectElement(string.Format("config/options/option[@name='profile_{0}']", selection));

            hotkey.Attribute("keyCode").Value = curProfKey.ToString("x");
            hotkey.Attribute("bAlt").Value = (bool)bAlt.IsChecked ? "1" : "0";
            hotkey.Attribute("bShift").Value = (bool)bShift.IsChecked ? "1" : "0";
            hotkey.Attribute("bCtrl").Value = (bool)bCtrl.IsChecked ? "1" : "0";

            MainWindow.extended_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
        }

        private void Active_Profile_SelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = (sender as ComboBox).SelectedIndex;

            // Check that the element is currently present
            if (MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}", selection)) == null)
            {
                var master = MainWindow.extended_xml.XPathSelectElement("config").LastNode;
                master.AddAfterSelf(new XElement(string.Format("profile_{0}", selection), new XElement("console_cmds"), new XElement("video_options"), new XElement("phantom", new XAttribute("enable", "1"))));
                // MainWindow.extended_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
            }

            foreach (var option in options)
            {
                // Individual video options
                var node = MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/video_options/option[@name='{1}']", selection, option));
                if(node == null)
                {
                    var lastNode = MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/video_options", selection));
                    lastNode.Add(new XElement("option", new XAttribute("name", option), new XAttribute("enable", "1")));
                    //MainWindow.extended_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));

                    var checkbox = (CheckBox)this.FindName(option);
                    if (checkbox != null)
                        checkbox.IsChecked = true;
                } else
                {
                    var checkbox = (CheckBox)this.FindName(option);
                    if (checkbox != null)
                        checkbox.IsChecked = node.Attribute("enable").Value == "1" ? true : false;
                }
            }

            // Check if Phantom effects is enabled or disabled
            var phantom = MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/phantom", selection));
            if (phantom == null)
            {
                MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}", selection)).Add(new XElement("phantom", new XAttribute("enable", "1")));
                showPhantom.IsChecked = true;
            }
            else
                showPhantom.IsChecked = phantom.Attribute("enable").Value == "1" ? true : false;

            var signalInfo = MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}/damage_font", selection));
            if (signalInfo == null)
            {
                MainWindow.extended_xml.XPathSelectElement(string.Format("config/profile_{0}", selection)).Add(new XElement("damage_font",
                    new XAttribute("scale", "1.6"),
                    new XAttribute("wordspacing", "0"),
                    new XAttribute("sgt_hit_enemy", "0"),
                    new XAttribute("sgt_crithit_enemy", "0"),
                    new XAttribute("sgt_bighit_enemy", "0")
                    ));
                signalInfo_Scale.Text = "1.6";
                signalInfo_Spacing.Text = "0";
                sgt_bighit_enemy.IsChecked = true;
                sgt_crithit_enemy.IsChecked = true;
                sgt_hit_enemy.IsChecked = true;
            }
            else
            {
                signalInfo_Scale.Text = signalInfo.Attribute("scale").Value;
                signalInfo_Spacing.Text = signalInfo.Attribute("wordspacing").Value;

                if (signalInfo.Attribute("sgt_hit_enemy") == null)
                {
                    signalInfo.Add(new XAttribute("sgt_hit_enemy", "0"), new XAttribute("sgt_crithit_enemy", "0"), new XAttribute("sgt_bighit_enemy", "0"));
                    sgt_bighit_enemy.IsChecked = true;
                    sgt_crithit_enemy.IsChecked = true;
                    sgt_hit_enemy.IsChecked = true;
                } else
                {
                    sgt_bighit_enemy.IsChecked = signalInfo.Attribute("sgt_bighit_enemy").Value == "0" ? true : false;
                    sgt_crithit_enemy.IsChecked = signalInfo.Attribute("sgt_crithit_enemy").Value == "0" ? true : false;
                    sgt_hit_enemy.IsChecked = signalInfo.Attribute("sgt_hit_enemy").Value == "0" ? true : false;
                }
            }

            // Get Console commands
            var commands = new List<ConsoleCommands>();
            foreach (var cmd in MainWindow.extended_xml.XPathSelectElements(string.Format("config/profile_{0}/console_cmds/cmd", selection)))
                commands.Add(new ConsoleCommands { command = cmd.Attribute("run").Value });

            ConsoleCmds.ItemsSource = commands;

            XElement hotkey;
            if (selection == 0)
                hotkey = MainWindow.extended_xml.XPathSelectElement("config/options/option[@name='reloadKey']");
            else
                hotkey = MainWindow.extended_xml.XPathSelectElement(string.Format("config/options/option[@name='profile_{0}']", selection));

            curProfKey = int.Parse(hotkey.Attribute("keyCode").Value, System.Globalization.NumberStyles.HexNumber);
            var keyInfo = KeyInterop.KeyFromVirtualKey(curProfKey);

            keyCode.Text = DisplayKeyName(keyInfo.ToString());
            
            bCtrl.IsChecked = hotkey.Attribute("bCtrl").Value == "1" ? true : false;
            bAlt.IsChecked = hotkey.Attribute("bAlt").Value == "1" ? true : false;
            bShift.IsChecked = hotkey.Attribute("bShift").Value == "1" ? true : false;

            MainWindow.extended_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
        }

        private string DisplayKeyName(string keyName)
        {
            // Check if the string is D[0-9] if so strip out the D
            Regex regx = new Regex("^D([0-9]){0,1}$");
            if (regx.IsMatch(keyName))
                keyName = keyName.Replace("D", "");

            return keyName;
        }

        private void keyCode_KeyDown(object sender, KeyEventArgs e)
        {
            keyCode.Text = DisplayKeyName(e.Key.ToString());
            curProfKey = KeyInterop.VirtualKeyFromKey(e.Key);
            e.Handled = true;
        }

        // I really need to start using viewmodels for this type of stuff but if I start doing that I would end up redoing the entire app to be proper.. Can't be bothered tbh, do it enough on professional stuff.
        private void OpenEffectMover(object sender, RoutedEventArgs e)
        {
            ExtendedOptionsGrid.Visibility = Visibility.Hidden;
            ExtendedOptions_EffectMover.Visibility = Visibility.Visible;

            if (MainWindow.extended_xml.XPathSelectElement("config/effect_list") == null)
            {
                var master = MainWindow.extended_xml.XPathSelectElement("config").LastNode;
                master.AddAfterSelf(new XElement("effect_list"));
            }

            var effects = new List<EffectsList>();
            foreach (var effect in MainWindow.extended_xml.XPathSelectElements("config/effect_list/effect"))
            {
                string alias = effect.Attribute("name").Value;
                if (alias == string.Empty) continue;
                EUI_Slot slot = effect.Attribute("ui-slot") != null ? (EUI_Slot)int.Parse(effect.Attribute("ui-slot").Value) : EUI_Slot.None;
                EUI_Category category = effect.Attribute("ui-category") != null ? (EUI_Category)int.Parse(effect.Attribute("ui-category").Value) : EUI_Category.None;

                effects.Add(new EffectsList { Alias = alias, UI_Slot = slot, UI_Category = category });
            }
            EffectList.ItemsSource = effects;
        }

        private void CloseEffectList(object sender, RoutedEventArgs e)
        {
            ExtendedOptionsGrid.Visibility = Visibility.Visible;
            ExtendedOptions_EffectMover.Visibility = Visibility.Hidden;
        }

        private void SaveEffectList(object sender, RoutedEventArgs e)
        {
            var effectList = EffectList.Items.OfType<EffectsList>().ToList();
            var effects = MainWindow.extended_xml.XPathSelectElement("config/effect_list");
            effects.Descendants().Where(x => true).Remove();
            foreach (var fx in effectList)
            {
                if (fx.Alias == string.Empty) continue;
                effects.Add(new XElement("effect", new XAttribute("name", fx.Alias), new XAttribute("ui-slot", (int)fx.UI_Slot), new XAttribute("ui-category", (int)fx.UI_Category)));
            }

            MainWindow.extended_xml.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
        }

        // Right now this just opens a web page that will redirect to a google spreadsheet with a full list. In the future I might make a proper database or implement it into the client but it is a lot of records so I probably won't ever bother.
        private void OpenEffectRecord(object sender, RoutedEventArgs e) => Process.Start(@"http://multitool.tonic.pw/effect_list.php");
    }
}