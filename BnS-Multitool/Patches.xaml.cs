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
        public List<AddonsAndPatches> Addons = new List<AddonsAndPatches>();
        public List<AddonsAndPatches> PatchesXML = new List<AddonsAndPatches>();
        public static int lastSelectedAddon = -1;
        public static int lastSelectedPatch = -1;

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public class AddonsAndPatches
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
            public string File { get; set; }
        }

        public Patches()
        {
            InitializeComponent();

            bgWorker.DoWork += new DoWorkEventHandler(updateAddonsAndPatches);

            //Check if any of our directories are missing
            if (!Directory.Exists(MANAGER_DIR))
                Directory.CreateDirectory(MANAGER_DIR);

            if (!Directory.Exists(ADDON_DIR))
                Directory.CreateDirectory(ADDON_DIR);

            if (!Directory.Exists(PATCHES_DIR))
                Directory.CreateDirectory(PATCHES_DIR);

            //bgWorker.RunWorkerAsync();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bgWorker.RunWorkerAsync();
        }

        private void updateAddonsAndPatches(object sender, DoWorkEventArgs e)
        {
            Addons = new List<AddonsAndPatches>();
            PatchesXML = new List<AddonsAndPatches>();

            foreach(var file in Directory.EnumerateFiles(MANAGER_DIR).Where(x => x.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith("patch", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (Path.GetExtension(file) == ".patch")
                    Addons.Add(new AddonsAndPatches() { Name = Path.GetFileNameWithoutExtension(file), isChecked = (File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(file)))), File = file });
                else if(Path.GetExtension(file) == ".xml")
                {
                    if (isBNSPatchXML(file))
                        PatchesXML.Add(new AddonsAndPatches() { Name = Path.GetFileNameWithoutExtension(file), isChecked = (File.Exists(Path.Combine(PATCHES_DIR, Path.GetFileName(file)))), File = file });
                    else
                        Addons.Add(new AddonsAndPatches() { Name = Path.GetFileNameWithoutExtension(file), isChecked = (File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(file)))), File = file });
                }
            }

            this.AddonsListBox.Dispatcher.Invoke(new Action(() =>
            {
                AddonsListBox.DataContext = Addons;
            }));

            this.PatchesListBox.Dispatcher.Invoke(new Action(() =>
            {
                PatchesListBox.DataContext = PatchesXML;
            }));
        }

        private void applyPatchesAndAddons(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var addon in Addons)
                {
                    if (addon.isChecked)
                    {
                        if (!File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File))))
                            if (File.Exists(addon.File))
                                CreateSymbolicLink(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File)), addon.File, SymbolicLink.File);
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File))))
                            File.Delete(Path.Combine(ADDON_DIR, Path.GetFileName(addon.File)));
                    }
                }

                foreach (var addon in PatchesXML)
                {
                    if (addon.isChecked)
                    {
                        if (!File.Exists(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File))))
                            if (File.Exists(addon.File))
                                CreateSymbolicLink(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File)), addon.File, SymbolicLink.File);
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File))))
                            File.Delete(Path.Combine(PATCHES_DIR, Path.GetFileName(addon.File)));
                    }
                }

                ((Storyboard)FindResource("animate")).Begin(successStatePicture);
                ((Storyboard)FindResource("animate")).Begin(PoggiesLabel);
            } catch (Exception ex)
            {
                new ErrorPrompt(String.Format("Something went wrong, could be an issue with Windows Defender. Check to see if Controlled Folder Access is turned on.\r\rAdditional Information: \r{0}", ex.Message)).ShowDialog();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bgWorker.RunWorkerAsync();
        }

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
                CollectionViewSource.GetDefaultView(AddonsListBox.DataContext).Refresh();
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
                CollectionViewSource.GetDefaultView(AddonsListBox.DataContext).Refresh();
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
                CollectionViewSource.GetDefaultView(PatchesListBox.DataContext).Refresh();
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
                CollectionViewSource.GetDefaultView(PatchesListBox.DataContext).Refresh();
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
                XDocument xmlFile = XDocument.Load(file, LoadOptions.None);
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

        private void handleToggle(object sender, RoutedEventArgs e)
        {
            bool state = ((Button)sender).Name.Contains("_on_");
            if(((Button)sender).Name.Contains("addons_"))
            {
                foreach (var b in Addons)
                    b.isChecked = state;

                CollectionViewSource.GetDefaultView(AddonsListBox.DataContext).Refresh();
            } else
            {
                foreach (var b in PatchesXML)
                    b.isChecked = state;

                CollectionViewSource.GetDefaultView(PatchesListBox.DataContext).Refresh();
            }

            applyPatchesAndAddons(sender, e);
        }
    }
}
