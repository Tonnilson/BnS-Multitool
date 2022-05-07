using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Patches.xaml
    /// </summary>
    public partial class Patches : Page
    {
        public static BackgroundWorker bgWorker = new BackgroundWorker();
        public static string BNS_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"BnS");
        public static string MANAGER_DIR = Path.Combine(BNS_DIR, "manager");
        public static string ADDON_DIR = Path.Combine(BNS_DIR, "addons");
        public static string PATCHES_DIR = Path.Combine(BNS_DIR, "patches");
        public static int lastSelectedAddon = -1;
        public static int lastSelectedPatch = -1;

        private ObservableCollection<AddonsAndPatches> _XmlViewCollection = new ObservableCollection<AddonsAndPatches>();
        public ObservableCollection<AddonsAndPatches> XML_View_Collection { get { return _XmlViewCollection; } }

        private ObservableCollection<AddonsAndPatches> _AddonsViewCollection = new ObservableCollection<AddonsAndPatches>();
        public ObservableCollection<AddonsAndPatches> ADDON_View_Collection { get { return _AddonsViewCollection; } }

        public class AddonsAndPatches
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
            public string File { get; set; }
            public string directory { get; set; }
            public string DisplayName { get; set; }
        }

        public Patches()
        {
            InitializeComponent();

            //Check if any of our directories are missing
            if (!Directory.Exists(MANAGER_DIR))
                Directory.CreateDirectory(MANAGER_DIR);

            if (!Directory.Exists(ADDON_DIR))
                Directory.CreateDirectory(ADDON_DIR);

            if (!Directory.Exists(PATCHES_DIR))
                Directory.CreateDirectory(PATCHES_DIR);

            if (!Directory.Exists(Path.Combine(MANAGER_DIR, "sync")))
                Directory.CreateDirectory(Path.Combine(MANAGER_DIR, "sync"));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await UpdateListing();

        /// <summary>
        /// This could be done a bit better but i'm done trying to work with old code
        /// and don't feel like redoing it entirely right now.
        /// </summary>
        private async Task UpdateListing()
        {
            try
            {
                _AddonsViewCollection.Clear();
                _XmlViewCollection.Clear();
                var _sync = new SyncClient("");

                foreach (var file in Directory.EnumerateFiles(MANAGER_DIR, "*.*", SearchOption.AllDirectories).
                    Where(x => x.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith("patch", StringComparison.InvariantCultureIgnoreCase)).
                    OrderBy(x => Path.GetFileName(x)))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string fileExt = Path.GetExtension(file);
                    string directory = Path.GetDirectoryName(file);
                    string DisplayName = fileName;

                    if(directory.Contains("\\sync\\"))
                    {
                        //We need to make sure the user synced with this file
                        if (SyncConfig.Synced == null || !SyncConfig.Synced.Any(x => x.Name == fileName && x.Type == fileExt.Replace(".",""))) continue;
                        DisplayName = await _sync.Base64UrlDecodeAsync(SyncConfig.Synced.First(x => x.Name == fileName).Title); //If everything is fine set the Display Name
                    }

                    if (fileExt == ".patch")
                    {
                        if (_AddonsViewCollection.Any(x => Path.GetFileName(x.File) == Path.GetFileName(file))) continue;
                        _AddonsViewCollection.Add(new AddonsAndPatches() { directory = directory, DisplayName = DisplayName, File = file, isChecked = File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(file))) });
                    }
                    else
                    {
                        if (isBNSPatchXML(file))
                        {
                            if (_XmlViewCollection.Any(x => Path.GetFileName(x.File) == Path.GetFileName(file))) continue;
                            _XmlViewCollection.Add(new AddonsAndPatches() { directory = directory, DisplayName = DisplayName, File = file, isChecked = File.Exists(Path.Combine(PATCHES_DIR, Path.GetFileName(file))) });
                        }
                        else
                        {
                            if (_AddonsViewCollection.Any(x => Path.GetFileName(x.File) == Path.GetFileName(file))) continue;
                            _AddonsViewCollection.Add(new AddonsAndPatches() { directory = directory, DisplayName = DisplayName, File = file, isChecked = File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(file))) });
                        }
                    }
                }
            } catch (Exception ex)
            {
                Logger.log.Error("Patches::UpdateListing\nType: {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                new ErrorPrompt("An error occured, check your anti-virus settings for Controlled Folder Access and either turn it off or whitelist this app to have folder access.").ShowDialog();
            }
        }

        private void applyPatchesAndAddons(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var addon in _AddonsViewCollection)
                {
                    if (addon.isChecked)
                    {
                        if (!File.Exists(Path.GetFullPath(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File)))))
                            if (File.Exists(Path.GetFullPath(addon.File)))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.GetFullPath(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File))), Path.GetFullPath(addon.File));
                    }
                    else
                    {
                        if (File.Exists(Path.GetFullPath(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File)))))
                            File.Delete(Path.GetFullPath(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File))));
                    }
                }

                foreach (var addon in _XmlViewCollection)
                {
                    if (addon.isChecked)
                    {
                        if (!File.Exists(Path.GetFullPath(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File)))))
                            if (File.Exists(Path.GetFullPath(addon.File)))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.GetFullPath(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File))), Path.GetFullPath(addon.File));
                    }
                    else
                    {
                        if (File.Exists(Path.GetFullPath(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File)))))
                        {
                            Debug.WriteLine(Path.GetFullPath(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File))));
                            File.Delete(Path.GetFullPath(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File))));
                        }
                    }
                }

                ((Storyboard)FindResource("animate")).Begin(successStatePicture);
                ((Storyboard)FindResource("animate")).Begin(PoggiesLabel);
            } catch (Exception ex)
            {
                Logger.log.Error("Patches::ApplyPatches\nType: {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                new ErrorPrompt(String.Format("Something went wrong, could be an issue with Windows Defender. Check to see if Controlled Folder Access is turned on.\r\rAdditional Information: \r{0}", ex.Message)).ShowDialog();
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e) => await UpdateListing();

        //This shit is jank AF
        private void AddonsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lastSelectedAddon = AddonsListBox.SelectedIndex;
            try
            {
                AddonsAndPatches AddonEntry = AddonsListBox.SelectedItem as AddonsAndPatches;
                if (AddonEntry == null) return;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                AddonsListBox.SelectedItem = AddonEntry;
                AddonsListBox.Items.Refresh();
                AddonsListBox.SelectedIndex = lastSelectedAddon;
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
                lastSelectedAddon = AddonsListBox.SelectedIndex;
                AddonsAndPatches AddonEntry = AddonsListBox.SelectedItem as AddonsAndPatches;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                AddonsListBox.SelectedItem = AddonEntry;
                AddonsListBox.Items.Refresh();
                AddonsListBox.SelectedIndex = lastSelectedAddon;
                e.Handled = true;
            }
        }

        private void PatchesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lastSelectedPatch = PatchesListBox.SelectedIndex;
            try
            {
                AddonsAndPatches AddonEntry = PatchesListBox.SelectedItem as AddonsAndPatches;
                if (AddonEntry == null) return;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                PatchesListBox.SelectedItem = AddonEntry;
                PatchesListBox.Items.Refresh();
                PatchesListBox.SelectedIndex = lastSelectedPatch;
            } catch (Exception ex) {
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }
        }

        private void PatchesListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                lastSelectedPatch = PatchesListBox.SelectedIndex;
                AddonsAndPatches AddonEntry = PatchesListBox.SelectedItem as AddonsAndPatches;
                if (AddonEntry.isChecked)
                    AddonEntry.isChecked = false;
                else
                    AddonEntry.isChecked = true;

                PatchesListBox.SelectedItem = AddonEntry;
                PatchesListBox.Items.Refresh();
                PatchesListBox.SelectedIndex = lastSelectedPatch;
                e.Handled = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) =>
            Process.Start(MANAGER_DIR);

        private bool isBNSPatchXML(string file)
        {
            try
            {
                XDocument xmlFile = XDocument.Parse(File.ReadAllText(file)); //Allows us to load all text files regardless of encoding
                //XDocument xmlFile = XDocument.Load(file, LoadOptions.None);
                return xmlFile.Root.Name == "patches";
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    new ErrorPrompt(String.Format("XML files should start with <?xml and not whitespaces or comments, fix the file format or delete it.\r\r {0}", file)).ShowDialog();
                });
                return false;
            }
        }

        private void HandleToggle(object sender, RoutedEventArgs e)
        {
            bool state = ((Button)sender).Name.Contains("_on_");
            if(((Button)sender).Name.Contains("addons_"))
            {
                foreach (var b in _AddonsViewCollection)
                    b.isChecked = state;

                AddonsListBox.Items.Refresh();
            } else
            {
                foreach (var b in _XmlViewCollection)
                    b.isChecked = state;

                PatchesListBox.Items.Refresh();
            }
        }

        private void OpenSyncPage(object sender, RoutedEventArgs e) =>
            MainWindow.mainWindow.SetCurrentPage("Sync");
    }
}
