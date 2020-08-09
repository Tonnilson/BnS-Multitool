using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Mods.xaml
    /// </summary>
    public partial class Mods : Page
    {
        private static string modPath;
        private static string modDestination;
        private static int lastSelectedMod = -1;
        private static BackgroundWorker worker;
        public List<MODS_CLASS> userMods = new List<MODS_CLASS>();

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public class MODS_CLASS
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
        }

        public Mods()
        {
            InitializeComponent();
            LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;

            modPath = SystemConfig.SYS.BNS_DIR + @"\contents\Local\NCWEST\" + languageFromSelection() + @"\CookedPC_Mod";
            modDestination = SystemConfig.SYS.BNS_DIR + @"\contents\Local\NCWEST\" + languageFromSelection() + @"\CookedPC\mod";

            //These are custom so we need to check if they exist, if not create them for the language.
            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(refreshModList);
            worker.RunWorkerAsync();
        }

        private void refreshModList(object sender, DoWorkEventArgs e)
        {
            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            string[] MOD_DIRECTORIES = Directory.GetDirectories(modPath);
            string[] MODS_INSTALLED = Directory.GetDirectories(modDestination);

            userMods = new List<MODS_CLASS>();

            foreach (string DIRECTORY in MOD_DIRECTORIES)
            {
                bool isEnabled = false;
                FileInfo FILE = new FileInfo(DIRECTORY);

                foreach (var mod in MODS_INSTALLED)
                {
                    FileInfo mod_folder = new FileInfo(mod);
                    if (mod_folder.Name == FILE.Name)
                    {
                        isEnabled = true;
                        break;
                    }
                }
                userMods.Add(new MODS_CLASS() { Name = FILE.Name, isChecked = isEnabled });
            }

            //We need to invoke on the UI thread to access the control, we're targeting the control specifically to leave other controls alone.
            this.ModsListBox.Dispatcher.Invoke(new Action(() =>
            {
                ModsListBox.DataContext = userMods;
            }));
        }

        private void launchInfoSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            modPath = SystemConfig.SYS.BNS_DIR + @"\contents\Local\NCWEST\" + languageFromSelection() + @"\CookedPC_Mod";
            modDestination = SystemConfig.SYS.BNS_DIR + @"\contents\Local\NCWEST\" + languageFromSelection() + @"\CookedPC\mod";

            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            ComboBox currentComboBox = (ComboBox)sender;
            int currentIndex = currentComboBox.SelectedIndex;
            ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE = currentIndex;
            ACCOUNT_CONFIG.appendChangesToConfig();
        }

        private void AddonsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lastSelectedMod = ModsListBox.SelectedIndex;
            try
            {
                MODS_CLASS AddonEntry = ModsListBox.SelectedItem as MODS_CLASS;
                if (AddonEntry == null) return;

                if (AddonEntry == null) return;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                ModsListBox.SelectedItem = AddonEntry;
                CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
                ModsListBox.SelectedIndex = lastSelectedMod;
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void AddonsListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                lastSelectedMod = ModsListBox.SelectedIndex;
                MODS_CLASS AddonEntry = ModsListBox.SelectedItem as MODS_CLASS;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                ModsListBox.SelectedItem = AddonEntry;
                CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
                ModsListBox.SelectedIndex = lastSelectedMod;
                e.Handled = true;
            }
        }

        private void applyMods(object sender, RoutedEventArgs e)
        {
            foreach (var mod in userMods)
            {
                if(mod.isChecked)
                {
                    if (Directory.Exists(modPath + @"\" + mod.Name))
                        if (!Directory.Exists(modDestination + @"\" + mod.Name))
                            CreateSymbolicLink(modDestination + @"\" + mod.Name, modPath + @"\" + mod.Name, SymbolicLink.Directory);
                } else
                {
                    if (Directory.Exists(modDestination + @"\" + mod.Name))
                        Directory.Delete(modDestination + @"\" + mod.Name, true);
                }
            }

            ((Storyboard)FindResource("animate")).Begin(successStatePicture);
        }

        private void refreshMods(object sender, RoutedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void openModFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(modPath);
        }

        private string languageFromSelection()
        {
            string lang = "English";
            switch (LANGUAGE_BOX.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    lang = "BPORTUGUESE";
                    break;
                case 2:
                    lang = "GERMAN";
                    break;
                case 3:
                    lang = "FRENCH";
                    break;
            }
            return lang;
        }
    }
}
