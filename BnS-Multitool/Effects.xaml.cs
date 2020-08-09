using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Effects.xaml
    /// </summary>
    public partial class Effects : Page
    {
        public static BackgroundWorker effectManagerRemove = new BackgroundWorker();
        public static BackgroundWorker effectManagerRestore = new BackgroundWorker();
        private ProgressControl _progressControl;
        private static bool _isInitialized = false; //logic for toggles cause there isn't a fucking click event

        public Effects()
        {
            InitializeComponent();

            //Remove worker
            effectManagerRemove.DoWork += new DoWorkEventHandler(DisableEffects);
            effectManagerRemove.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DisableEffectsComplete);

            //Restore Worker, why am I doing two workers? Oh yeah cause i'm lazy.
            effectManagerRestore.DoWork += new DoWorkEventHandler(EnableEffects);
            effectManagerRestore.RunWorkerCompleted += new RunWorkerCompletedEventHandler(EnableEffectsComplete);

            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;
            BNS_UPK_BOX.Text = SystemConfig.SYS.UPK_DIR;

            if (SystemConfig.SYS.ADDITIONAL_EFFECTS == 1)
                ADDITIONAL_EFFECTS_BOX.IsChecked = true;

            foreach(var classData in SystemConfig.SYS.ANIMATION_UPKS)
            {
                if(checkAnimationFiles(classData.CLASS))
                {
                    switch(classData.CLASS)
                    {
                        case "Assassin":
                            sin_toggle.IsChecked = true;
                            break;
                        case "Summoner":
                            sum_toggle.IsChecked = true;
                            break;
                        case "KungFuMaster":
                            kfm_toggle.IsChecked = true;
                            break;
                        case "Gunslinger":
                            gs_toggle.IsChecked = true;
                            break;
                        case "Destroyer":
                            des_toggle.IsChecked = true;
                            break;
                        case "Forcemaster":
                            fm_toggle.IsChecked = true;
                            break;
                        case "Soulfighter":
                            sf_toggle.IsChecked = true;
                            break;
                        case "Archer":
                            arc_toggle.IsChecked = true;
                            break;
                        case "Blademaster":
                            bm_toggle.IsChecked = true;
                            break;
                        case "Bladedancer":
                            bd_toggle.IsChecked = true;
                            break;
                        case "Warlock":
                            wl_toggle.IsChecked = true;
                            break;
                        case "Warden":
                            wd_toggle.IsChecked = true;
                            break;
                    }
                }
            }

            if (SystemConfig.SYS.ANIMATION_UPKS.SelectMany(upk => upk.UPK_FILES).Any(file => File.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\" + file)))
                all_toggle.IsChecked = true;

            _isInitialized = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void EnableEffects(object sender, DoWorkEventArgs e)
        {
            ProgressControl.errorSadPeepo(Visibility.Hidden);
            ProgressControl.updateProgressLabel("Restoring Effects");
            Thread.Sleep(500);

            if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\"))
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("Blade and Soul path is not valid");
                Thread.Sleep(5000);
                return;
            }
            else if (SystemConfig.SYS.UPK_DIR == "")
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("UPK Backup directory is not set!");
                Thread.Sleep(5000);
                return;
            }

            if (!Directory.Exists(SystemConfig.SYS.UPK_DIR))
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("Path for backing up UPK's doesn't exist");
                Thread.Sleep(5000);
                return;
            }

            foreach (string UPK in SystemConfig.SYS.MAIN_UPKS)
            {
                ProgressControl.updateProgressLabel(String.Format("Checking for {0}", UPK));
                string _DEST = SystemConfig.SYS.BNS_DIR + "\\contents\\bns\\CookedPC\\" + UPK;
                string _SOURCE = SystemConfig.SYS.UPK_DIR + "\\" + UPK;
                if (File.Exists(_SOURCE))
                {
                    if (File.Exists(_DEST))
                        File.Delete(_DEST);

                    ProgressControl.updateProgressLabel(String.Format("Restoring {0}", UPK));
                    File.Move(_SOURCE, _DEST);
                }
                Thread.Sleep(50);
            }

            if (SystemConfig.SYS.ADDITIONAL_EFFECTS == 1)
            {
                foreach (string UPK in SystemConfig.SYS.ADDITIONAL_UPKS)
                {
                    ProgressControl.updateProgressLabel(String.Format("Checking for {0}", UPK));
                    string _DEST = SystemConfig.SYS.BNS_DIR + "\\contents\\bns\\CookedPC\\" + UPK;
                    string _SOURCE = SystemConfig.SYS.UPK_DIR + "\\" + UPK;
                    if (File.Exists(_SOURCE))
                    {
                        if (File.Exists(_DEST))
                            File.Delete(_DEST);

                        ProgressControl.updateProgressLabel(String.Format("Removing {0}", UPK));
                        File.Move(_SOURCE, _DEST);
                    }
                }
                Thread.Sleep(50);
            }
        }

        private void EnableEffectsComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }


        private void DisableEffects(object sender, DoWorkEventArgs e)
        {
            ProgressControl.errorSadPeepo(Visibility.Hidden);
            ProgressControl.updateProgressLabel("Removing Effects");
            Thread.Sleep(500);

            if(!Directory.Exists(SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\"))
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("Blade and Soul path is not valid");
                Thread.Sleep(5000);
                return;
            } else if(SystemConfig.SYS.UPK_DIR == "")
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("UPK Backup directory is not set!");
                Thread.Sleep(5000);
                return;
            }

            if(!Directory.Exists(SystemConfig.SYS.UPK_DIR))
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel("Path for backing up UPK's doesn't exist");
                Thread.Sleep(5000);
                return;
            }

            foreach(string UPK in SystemConfig.SYS.MAIN_UPKS)
            {
                string _SOURCE = SystemConfig.SYS.BNS_DIR + "\\contents\\bns\\CookedPC\\" + UPK;
                string _DEST = SystemConfig.SYS.UPK_DIR + "\\" + UPK;

                ProgressControl.updateProgressLabel(String.Format("Checking for {0}", UPK));
                if (File.Exists(_SOURCE))
                {
                    if (File.Exists(_DEST))
                        File.Delete(_DEST);

                    ProgressControl.updateProgressLabel(String.Format("Removing {0}", UPK));
                    File.Move(_SOURCE, _DEST);
                }

                Thread.Sleep(50);
            }

            if (SystemConfig.SYS.ADDITIONAL_EFFECTS == 1)
            {
                foreach (string UPK in SystemConfig.SYS.ADDITIONAL_UPKS)
                {
                    string _SOURCE = SystemConfig.SYS.BNS_DIR + "\\contents\\bns\\CookedPC\\" + UPK;
                    string _DEST = SystemConfig.SYS.UPK_DIR + "\\" + UPK;
                    ProgressControl.updateProgressLabel(String.Format("Checking for {0}", UPK));

                    if (File.Exists(_SOURCE))
                    {
                        if (File.Exists(_DEST))
                            File.Delete(_DEST);

                        ProgressControl.updateProgressLabel(String.Format("Removing {0}", UPK));
                        File.Move(_SOURCE, _DEST);
                    }

                    Thread.Sleep(50);
                }
            }
        }

        private void DisableEffectsClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            MainWindow.mainWindowFrame.RemoveBackEntry();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;

            ProgressPanel.Children.Add(_progressControl);
            effectManagerRemove.RunWorkerAsync();
        }

        private void DisableEffectsComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }

        private void EnableEffectsClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            MainWindow.mainWindowFrame.RemoveBackEntry();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;

            ProgressPanel.Children.Add(_progressControl);
            effectManagerRestore.RunWorkerAsync();
        }

        private void ADDITIONAL_EFFECTS_BOX_Click(object sender, RoutedEventArgs e)
        {
            if((bool)ADDITIONAL_EFFECTS_BOX.IsChecked)
                SystemConfig.SYS.ADDITIONAL_EFFECTS = 1;
            else
                SystemConfig.SYS.ADDITIONAL_EFFECTS = 0;

            SystemConfig.appendChangesToConfig();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var FOLDER = new FolderBrowserDialog())
            {
                DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                {
                    BNS_LOCATION_BOX.Text = FOLDER.SelectedPath;
                    SystemConfig.SYS.BNS_DIR = FOLDER.SelectedPath;
                    SystemConfig.appendChangesToConfig();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (var FOLDER = new FolderBrowserDialog())
            {
                DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                {
                    BNS_UPK_BOX.Text = FOLDER.SelectedPath;
                    SystemConfig.SYS.UPK_DIR = FOLDER.SelectedPath;
                    SystemConfig.appendChangesToConfig();
                }
            }
        }

        private void updateLocationsOnChange(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox currentBox = (System.Windows.Controls.TextBox)sender;

            if (currentBox.Name == "BNS_LOCATION_BOX")
                SystemConfig.SYS.BNS_DIR = currentBox.Text;
            else
                SystemConfig.SYS.UPK_DIR = currentBox.Text;

            SystemConfig.appendChangesToConfig();
        }

        private bool checkAnimationFiles(string className)
        {
            string directory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
            return SystemConfig.SYS.ANIMATION_UPKS.Where(x => x.CLASS == className).SelectMany(upk => upk.UPK_FILES).Any(file => File.Exists(directory + file));
        }

        private void toggleChecked(object sender, RoutedEventArgs e)
        {
            //Page is not fully initialized yet so get us out
            if (!_isInitialized)
                return;
            if (SystemConfig.SYS.UPK_DIR == "") return;
            if (!Directory.Exists(SystemConfig.SYS.UPK_DIR)) return;

            ToggleSwitch.HorizontalToggleSwitch currentBox = (ToggleSwitch.HorizontalToggleSwitch)sender;

            if (!(bool)currentBox.IsChecked)
                return;

            string className = "";

            switch(currentBox.Name)
            {
                case "sin_toggle":
                    className = "Assassin";
                    break;
                case "sum_toggle":
                    className = "Summoner";
                    break;
                case "kfm_toggle":
                    className = "KungFuMaster";
                    break;
                case "fm_toggle":
                    className = "Forcemaster";
                    break;
                case "gs_toggle":
                    className = "Gunslinger";
                    break;
                case "des_toggle":
                    className = "Destroyer";
                    break;
                case "sf_toggle":
                    className = "Soulfighter";
                    break;
                case "arc_toggle":
                    className = "Archer";
                    break;
                case "bm_toggle":
                    className = "Blademaster";
                    break;
                case "bd_toggle":
                    className = "Bladedancer";
                    break;
                case "wl_toggle":
                    className = "Warlock";
                    break;
                case "wd_toggle":
                    className = "Warden";
                    break;
            }

            if (className != "" && !checkAnimationFiles(className))
                handleClassAnimations(false, className);
        }

        private void handleAllClassAnimations(bool state)
        {
            string directory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
            var upks = SystemConfig.SYS.ANIMATION_UPKS.SelectMany(upk => upk.UPK_FILES);

            foreach (var file in upks)
            {
                if (state)
                {
                    if (File.Exists(directory + file))
                        if (!File.Exists(SystemConfig.SYS.UPK_DIR + @"\" + file))
                            File.Move(directory + file, SystemConfig.SYS.UPK_DIR + @"\" + file);
                }
                else
                {
                    if (File.Exists(SystemConfig.SYS.UPK_DIR + @"\" + file))
                        if (!File.Exists(directory + file))
                            File.Move(SystemConfig.SYS.UPK_DIR + @"\" + file, directory + file);
                }
            }

            if (state)
                classLabel.Content = "All removed";
            else
                classLabel.Content = "All restored";

            sin_toggle.IsChecked = !state;
            sum_toggle.IsChecked = !state;
            kfm_toggle.IsChecked = !state;
            gs_toggle.IsChecked = !state;
            des_toggle.IsChecked = !state;
            fm_toggle.IsChecked = !state;
            sf_toggle.IsChecked = !state;
            arc_toggle.IsChecked = !state;
            bm_toggle.IsChecked = !state;
            bd_toggle.IsChecked = !state;
            wl_toggle.IsChecked = !state;
            wd_toggle.IsChecked = !state;

            ((Storyboard)FindResource("animate")).Begin(classLabel);
            ((Storyboard)FindResource("animate")).Begin(successStatePicture);
        }

        private void handleClassAnimations(bool state, string className)
        {
            string directory = SystemConfig.SYS.BNS_DIR + @"\contents\bns\CookedPC\";
            var class_upks = SystemConfig.SYS.ANIMATION_UPKS.Where(x => x.CLASS == className).SelectMany(upk => upk.UPK_FILES);

            foreach (var file in class_upks)
            {
                if (state)
                {
                    Debug.WriteLine("Attempting to remove files for: " + className + "\n");
                    Debug.Write("File: " + file + "\n");
                    if (File.Exists(directory + file))
                    {
                        try
                        {
                            Debug.Write("Removing: " + file + " for: " + className + "\n");
                            File.Move(directory + file, SystemConfig.SYS.UPK_DIR + @"\" + file);
                        } catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                } else
                {
                    Debug.WriteLine("Attempting to restore files for: " + className + "\n");
                    Debug.Write("File: " + file + "\n");
                    if (File.Exists(SystemConfig.SYS.UPK_DIR + @"\" + file))
                    {
                        try
                        {
                            Debug.Write("Restoring: " + file + " for: " + className + "\n");
                            if (!File.Exists(directory + file))
                                File.Move(SystemConfig.SYS.UPK_DIR + @"\" + file, directory + file);
                        } catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }

            if (state)
                classLabel.Content = String.Format("{0} Removed", className);
            else
                classLabel.Content = String.Format("{0} Restored", className);

            ((Storyboard)FindResource("animate")).Begin(classLabel);
            ((Storyboard)FindResource("animate")).Begin(successStatePicture);
        }

        private void toggleUnchecked(object sender, RoutedEventArgs e)
        {
            //Page is not fully initialized yet so get us out
            if (!_isInitialized)
                return;

            if (SystemConfig.SYS.UPK_DIR == "") return;
            if (!Directory.Exists(SystemConfig.SYS.UPK_DIR)) return;

            ToggleSwitch.HorizontalToggleSwitch currentBox = (ToggleSwitch.HorizontalToggleSwitch)sender;

            if ((bool)currentBox.IsChecked)
                return;

            string className = "";

            switch (currentBox.Name)
            {
                case "sin_toggle":
                    className = "Assassin";
                    break;
                case "sum_toggle":
                    className = "Summoner";
                    break;
                case "kfm_toggle":
                    className = "KungFuMaster";
                    break;
                case "fm_toggle":
                    className = "Forcemaster";
                    break;
                case "gs_toggle":
                    className = "Gunslinger";
                    break;
                case "des_toggle":
                    className = "Destroyer";
                    break;
                case "sf_toggle":
                    className = "Soulfighter";
                    break;
                case "arc_toggle":
                    className = "Archer";
                    break;
                case "bm_toggle":
                    className = "Blademaster";
                    break;
                case "bd_toggle":
                    className = "Bladedancer";
                    break;
                case "wl_toggle":
                    className = "Warlock";
                    break;
                case "wd_toggle":
                    className = "Warden";
                    break;
            }

            if (className != "" && checkAnimationFiles(className))
                handleClassAnimations(true, className);
        }

        private void allChecked(object sender, RoutedEventArgs e)
        {
            //Page is not fully initialized yet so get us out
            if (!_isInitialized)
                return;

            if (SystemConfig.SYS.UPK_DIR == "") return;
            if (!Directory.Exists(SystemConfig.SYS.UPK_DIR)) return;

            _isInitialized = false;
            handleAllClassAnimations(false);
            _isInitialized = true;
        }

        private void allUnchecked(object sender, RoutedEventArgs e)
        {
            //Page is not fully initialized yet so get us out
            if (!_isInitialized)
                return;

            if (SystemConfig.SYS.UPK_DIR == "") return;
            if (!Directory.Exists(SystemConfig.SYS.UPK_DIR)) return;

            _isInitialized = false;
            handleAllClassAnimations(true);
            _isInitialized = true;
        }
    }
}