using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ToggleSwitch;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Effects.xaml
    /// </summary>
    public partial class Effects : Page
    {
        private ProgressControl _progressControl;
        private static bool _isInitialized = false; //logic for toggles cause there isn't a fucking click event
        private static string backupLocation = SystemConfig.SYS.BNS_DIR + @"\contents\bns\backup\";

        public class toggleStruct
        {
            public string className { get; set; }
            public HorizontalToggleSwitch animToggle { get; set; }
            public HorizontalToggleSwitch fxToggle { get; set; }
        }

        private static List<toggleStruct> systemToggles;

        private bool DoesFilesExist(string className, bool section)
        {
            //true for effects, false for animations
            if (section)
                return SystemConfig.SYS.CLASSES.Where(x => x.CLASS == className).SelectMany(upk => upk.EFFECTS).Any(file => File.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\" + file));
            else
                return SystemConfig.SYS.CLASSES.Where(x => x.CLASS == className).SelectMany(upk => upk.ANIMATIONS).Any(file => File.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\" + file));
        }

        public Effects()
        {
            InitializeComponent();

            //Make sure the BnS directory is valid and create the backup directory if it does not exist
            if (Directory.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\"))
                if (!Directory.Exists(backupLocation))
                    Directory.CreateDirectory(backupLocation);

            //Backwards compatability with older versions, need to move all the files back to the bns directory, remove in 3 version iterations
            if(SystemConfig.SYS.UPK_DIR != "" && Directory.Exists(SystemConfig.SYS.UPK_DIR))
            {
                foreach(string file in Directory.GetFiles(SystemConfig.SYS.UPK_DIR))
                {
                    try
                    {
                        FileInfo upkFile = new FileInfo(file);
                        upkFile.MoveTo(backupLocation + upkFile.Name);
                    } catch (Exception)
                    {

                    }
                }
            }

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
                new toggleStruct() {className = "Archer", animToggle = arc_anim_toggle, fxToggle = arc_fx_toggle}
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            refreshToggles();

            GC.Collect();
        }

        private void refreshToggles()
        {
            _isInitialized = false;
            //Loop through individual classes and check if their animation upk's and effect upk's exist, if so flip their toggles to on.
            foreach (var classData in SystemConfig.SYS.CLASSES)
            {
                var curClass = systemToggles.Where(x => x.className == classData.CLASS).FirstOrDefault();
                if (DoesFilesExist(classData.CLASS, true))
                    curClass.fxToggle.IsChecked = true;

                if (DoesFilesExist(classData.CLASS, false))
                    curClass.animToggle.IsChecked = true;
            }

            //Check if any of the other effects are present, if so toggle on.
            if (SystemConfig.SYS.MAIN_UPKS.Any(file => File.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\" + file)))
                otherEffectsToggle.IsChecked = true;

            _isInitialized = true;
        }

        private async void handleToggleChange(object sender, RoutedEventArgs e)
        {
            HorizontalToggleSwitch currentToggle = (HorizontalToggleSwitch)sender;

            if (!_isInitialized)
                return;

            if (Process.GetProcessesByName("Client").Length >= 1)
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

                if(currentToggle.Name.Contains("_fx_"))
                {
                    sysToggle = systemToggles.Where(x => x.fxToggle.Name == currentToggle.Name).FirstOrDefault();
                    upkfiles = SystemConfig.SYS.CLASSES.Where(x => x.CLASS == sysToggle.className).Select(upk => upk.EFFECTS).FirstOrDefault();
                } else
                {
                    sysToggle = systemToggles.Where(x => x.animToggle.Name == currentToggle.Name).FirstOrDefault();
                    upkfiles = SystemConfig.SYS.CLASSES.Where(x => x.CLASS == sysToggle.className).Select(upk => upk.ANIMATIONS).FirstOrDefault();
                }

                //We're restoring
                if(currentToggle.IsChecked)
                {
                    sourceDirectory = backupLocation;
                    destinationDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
                } else
                {
                    sourceDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
                    destinationDirectory = backupLocation;
                }

               // _isInitialized = false;
                foreach(string file in upkfiles)
                {
                    try
                    {
                        //Move our target file to our new destination
                        if (File.Exists(sourceDirectory + file))
                        {
                            if (!File.Exists(destinationDirectory + file))
                                File.Move(sourceDirectory + file, destinationDirectory + file);
                            else
                                File.Delete(sourceDirectory + file);
                        }
                    }
                    catch (Exception)
                    {
                        //ProgressControl.updateProgressLabel(ex.Message);
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
                upkFiles = SystemConfig.SYS.MAIN_UPKS;

                _progressControl = new ProgressControl();
                MainWindow.mainWindowFrame.RemoveBackEntry();
                ProgressGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Collapsed;

                ProgressPanel.Children.Add(_progressControl);

                //Turning whatever the fuck it is on
                if (currentToggle.IsChecked)
                {
                    ProgressControl.errorSadPeepo(Visibility.Hidden);
                    ProgressControl.updateProgressLabel("Restoring files");
                    await Task.Delay(150);
                    sourceDirectory = backupLocation;
                    destinationDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
                } else
                {
                    ProgressControl.errorSadPeepo(Visibility.Hidden);
                    ProgressControl.updateProgressLabel("Removing files");
                    await Task.Delay(150);
                    sourceDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
                    destinationDirectory = backupLocation;
                }

                foreach (string file in upkFiles)
                {
                    try
                    {
                        ProgressControl.updateProgressLabel(String.Format("Checking for {0}", file));
                        await Task.Delay(25);
                        //Move our target file to our new destination
                        if (File.Exists(sourceDirectory + file))
                        {
                            ProgressControl.updateProgressLabel(String.Format("{0} {1}", (currentToggle.IsChecked) ? "Restoring" : "Removing", file));
                            if (!File.Exists(destinationDirectory + file))
                                File.Move(sourceDirectory + file, destinationDirectory + file);
                            else
                                File.Delete(sourceDirectory + file);
                        }

                    } catch (Exception ex)
                    {
                        ProgressControl.updateProgressLabel(ex.Message);
                        await Task.Delay(500);
                    }
                    await Task.Delay(50);
                }

                ProgressGrid.Visibility = Visibility.Hidden;
                MainGrid.Visibility = Visibility.Visible;
                ProgressPanel.Children.Clear();
                _progressControl = null;
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
                upkFiles = SystemConfig.SYS.CLASSES.SelectMany(entries => entries.ANIMATIONS).ToArray();
                foreach (var fuckass in systemToggles)
                    fuckass.animToggle.IsChecked = currentState;
                _isInitialized = true;
            }
            else if (currentToggle.Name.Contains("effect"))
            {
                Debug.WriteLine("effect Toggle");
                _isInitialized = false;
                upkFiles = SystemConfig.SYS.CLASSES.SelectMany(entries => entries.EFFECTS).ToArray();
                foreach (var fuckass in systemToggles)
                    fuckass.fxToggle.IsChecked = currentState;
                _isInitialized = true;
            }
            else
            {
                _isInitialized = false;

                //Put all animations and stuff into one big array, I'm sure there is a better way to do this but it's 6am and I can't be bothered to think efficient.
                upkFiles = SystemConfig.SYS.CLASSES.SelectMany(entries => entries.ANIMATIONS).ToArray();
                upkFiles = upkFiles.Concat(SystemConfig.SYS.CLASSES.SelectMany(entries => entries.EFFECTS)).ToArray();
                upkFiles =  upkFiles.Concat(SystemConfig.SYS.MAIN_UPKS).ToArray();

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
                ProgressControl.updateProgressLabel("Restoring files");
                await Task.Delay(150);
                sourceDirectory = backupLocation;
                destinationDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
            }
            else
            {
                ProgressControl.errorSadPeepo(Visibility.Hidden);
                ProgressControl.updateProgressLabel("Removing files");
                await Task.Delay(150);
                sourceDirectory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
                destinationDirectory = backupLocation;
            }

            foreach (string file in upkFiles)
            {
                try
                {
                    ProgressControl.updateProgressLabel(String.Format("Checking for {0}", file));
                    await Task.Delay(25);
                    //Move our target file to our new destination
                    if (File.Exists(sourceDirectory + file))
                    {
                        ProgressControl.updateProgressLabel(String.Format("{0} {1}", currentState ? "Restoring" : "Removing", file));
                        if (!File.Exists(destinationDirectory + file))
                            File.Move(sourceDirectory + file, destinationDirectory + file);
                        else
                            File.Delete(sourceDirectory + file);
                    }

                }
                catch (Exception ex)
                {
                    ProgressControl.updateProgressLabel(ex.Message);
                    await Task.Delay(500);
                }
                await Task.Delay(50);
            }

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }
    }
}