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
using ToggleSwitch;
using static BnS_Multitool.Functions.FileExtraction;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Effects.xaml
    /// </summary>
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

            GC.Collect();
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

        private void ExtendedOptions_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility=Visibility.Collapsed;
            ExtendedOptionsGrid.Visibility=Visibility.Visible;
        }
    }
}