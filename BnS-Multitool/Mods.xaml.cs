using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static BnS_Multitool.Functions.FileExtraction;

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

            //FilterList.AppendText(Properties.Resources.playable_ncwest);
        }

        private async Task ShowModList()
        {
            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            string[] MOD_DIRECTORIES = Directory.GetDirectories(modPath);
            string[] MODS_INSTALLED = Directory.GetDirectories(modDestination);

            try
            {
                //Cleanup of bad links
                var mod_directories = Directory.GetDirectories(modDestination);
                foreach (var mod in mod_directories)
                {
                    DirectoryInfo di = new DirectoryInfo(mod);
                    if (di.IsSymbolicLink() && !Directory.Exists(di.GetSymbolicLinkTarget()))
                        di.Delete(true);
                }
            } catch (IOException)
            {

            }
            
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
            await this.ModsListBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                ModsListBox.DataContext = userMods;
            }));
        }

        private async void launchInfoSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //KR is slightly different so we need to adjust for that.
            modPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Mods");
            modDestination = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Mods");

            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            ComboBox currentComboBox = (ComboBox)sender;
            int currentIndex = currentComboBox.SelectedIndex;
            ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE = currentIndex;
            ACCOUNT_CONFIG.Save();

            await ShowModList();
        }

        private void AddonsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lastSelectedMod = ModsListBox.SelectedIndex;
            try
            {
                MODS_CLASS AddonEntry = ModsListBox.SelectedItem as MODS_CLASS;
                if (AddonEntry != null)
                {
                    if (Directory.GetFiles(Path.Combine(modPath, AddonEntry.Name), "*.upk").Length != 0)
                    {
                        new ErrorPrompt(string.Format("{0} contains files meant for UE3 and not UE4", AddonEntry.Name)).ShowDialog();
                        return;
                    }

                    if (AddonEntry == null) return;

                    if (AddonEntry == null) return;
                    if (AddonEntry.isChecked)
                        AddonEntry.isChecked = false;
                    else
                        AddonEntry.isChecked = true;

                    ModsListBox.SelectedItem = AddonEntry;
                    CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
                    ModsListBox.SelectedIndex = lastSelectedMod;
                }
            } catch (Exception ex)
            {
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }
        }

        private void AddonsListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                lastSelectedMod = ModsListBox.SelectedIndex;
                MODS_CLASS AddonEntry = ModsListBox.SelectedItem as MODS_CLASS;
                if (AddonEntry != null)
                {
                    if (Directory.GetFiles(Path.Combine(modPath, AddonEntry.Name), "*.upk").Length != 0)
                    {
                        new ErrorPrompt(string.Format("{0} contains files meant for UE3 and not UE4", AddonEntry.Name)).ShowDialog();
                        return;
                    }
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
        }

        private void OnModChecked(object sender, RoutedEventArgs e)
        {
            // Janky way to interact with the way I do data binding, maybe one day I will switch to a view model and handle it the proper way.
            foreach(var mod in userMods)
            {
               if(Directory.GetFiles(Path.Combine(modPath, mod.Name), "*.upk").Length != 0 && mod.isChecked)
                {
                    new ErrorPrompt(string.Format("{0} contains files meant for UE3 and not UE4", mod.Name)).ShowDialog();
                    mod.isChecked = false;
                    CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
                }
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

        private async void refreshMods(object sender, RoutedEventArgs e) => await ShowModList();
        private void openModFolder(object sender, RoutedEventArgs e) => Process.Start(modPath);

        private void handleToggle(object sender, RoutedEventArgs e)
        {
            bool state = ((Button)sender).Name.Contains("_on_");
            foreach (var b in userMods)
                b.isChecked = state;

            CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
            applyMods(sender, e);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;
                modPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Mods");
                modDestination = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Mods");

                //These are custom so we need to check if they exist, if not create them for the language.
                if (!Directory.Exists(modPath))
                    Directory.CreateDirectory(modPath);

                if (!Directory.Exists(modDestination))
                    Directory.CreateDirectory(modDestination);

                await ShowModList();
                LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;
            } catch (Exception ex)
            {
                Logger.log.Error("{0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void PatchBit(object sender, RoutedEventArgs e)
        {
            CheckBox currentCheckBox = (CheckBox)sender;
            int currentState = ((bool)currentCheckBox.IsChecked) ? 1 : 0;

            SystemConfig.Save();
        }
    }
}
