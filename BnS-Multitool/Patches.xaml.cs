using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Patches.xaml
    /// </summary>
    public partial class Patches : Page
    {
        public static BackgroundWorker bgWorker = new BackgroundWorker();
        public static string BNS_DIR = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\";
        public static string MANAGER_DIR = BNS_DIR + @"manager\";
        public static string ADDON_DIR = BNS_DIR + @"addons\";
        public static string PATCHES_DIR = BNS_DIR + @"patches\";
        public List<AddonsAndPatches> Addons = new List<AddonsAndPatches>();
        public List<AddonsAndPatches> PatchesXML = new List<AddonsAndPatches>();
        public static int lastSelectedAddon = -1;
        public static int lastSelectedPatch = -1;

        public class AddonsAndPatches
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
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

            bgWorker.RunWorkerAsync();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void updateAddonsAndPatches(object sender, DoWorkEventArgs e)
        {
            //Retrieve addon file names & strip extension (addons directory)
            var ADDON_PATCH_FILES = Directory.EnumerateFiles(ADDON_DIR).Select(x => Path.GetFileName(x))
                .Where(Name => Path.GetExtension(Name) == ".patch")
                    .Select(Name => Path.GetFileName(Name));
            var ADDON_PATCH_NAMES = ADDON_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));

            //Retrieve addon file names & strip extension (manager directory)
            var MANAGER_PATCH_FILES = Directory.EnumerateFiles(MANAGER_DIR).Select(x => Path.GetFileName(x))
                .Where(Name => Path.GetExtension(Name) == ".patch")
                    .Select(Name => Path.GetFileName(Name));
            var MANAGER_PATCH_NAMES = MANAGER_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));

            Addons = new List<AddonsAndPatches>();
            foreach (var patch in MANAGER_PATCH_NAMES)
            {
                bool isEnabled = false;
                if (ADDON_PATCH_NAMES.Contains(patch))
                    isEnabled = true;

                Addons.Add(new AddonsAndPatches() { Name = patch, isChecked = isEnabled });
            }

            //Repurposing this for XML (patches) directory
            ADDON_PATCH_FILES = Directory.EnumerateFiles(PATCHES_DIR).Select(x => Path.GetFileName(x))
           .Where(Name => Path.GetExtension(Name) == ".xml")
               .Select(Name => Path.GetFileName(Name));
            ADDON_PATCH_NAMES = ADDON_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));
            MANAGER_PATCH_FILES = Directory.EnumerateFiles(MANAGER_DIR).Select(x => Path.GetFileName(x))
                .Where(Name => Path.GetExtension(Name) == ".xml")
                .Select(Name => Path.GetFileName(Name));
            MANAGER_PATCH_NAMES = MANAGER_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));

            PatchesXML = new List<AddonsAndPatches>();
            foreach (var patch in MANAGER_PATCH_NAMES)
            {
                bool isEnabled = false;
                if (ADDON_PATCH_NAMES.Contains(patch))
                    isEnabled = true;

                PatchesXML.Add(new AddonsAndPatches() { Name = patch, isChecked = isEnabled });
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
            var ADDON_PATCH_FILES = Directory.EnumerateFiles(ADDON_DIR).Select(x => Path.GetFileName(x))
            .Where(Name => Path.GetExtension(Name) == ".patch")
                .Select(Name => Path.GetFileName(Name));
            var ADDON_PATCH_NAMES = ADDON_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));
            var MANAGER_PATCH_FILES = Directory.EnumerateFiles(MANAGER_DIR).Select(x => Path.GetFileName(x))
                .Where(Name => Path.GetExtension(Name) == ".patch")
                .Select(Name => Path.GetFileName(Name));
            var MANAGER_PATCH_NAMES = MANAGER_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));

            foreach (var addon in Addons)
            {
                if(addon.isChecked)
                {
                    if (!ADDON_PATCH_NAMES.Contains(addon.Name))
                        if (!File.Exists(ADDON_DIR + addon.Name + ".patch"))
                            File.Copy(MANAGER_DIR + addon.Name + ".patch", ADDON_DIR + addon.Name + ".patch");
                }
                else
                {
                    if (File.Exists(ADDON_DIR + addon.Name + ".patch"))
                        File.Delete(ADDON_DIR + addon.Name + ".patch");
                }
            }

            //Patches part
            ADDON_PATCH_FILES = Directory.EnumerateFiles(PATCHES_DIR).Select(x => Path.GetFileName(x))
            .Where(Name => Path.GetExtension(Name) == ".xml")
                .Select(Name => Path.GetFileName(Name));
            ADDON_PATCH_NAMES = ADDON_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));
            MANAGER_PATCH_FILES = Directory.EnumerateFiles(MANAGER_DIR).Select(x => Path.GetFileName(x))
                .Where(Name => Path.GetExtension(Name) == ".xml")
                .Select(Name => Path.GetFileName(Name));
            MANAGER_PATCH_NAMES = MANAGER_PATCH_FILES.Where(x => Path.GetFileName(x) == x).Select(Name => Path.GetFileNameWithoutExtension(Name));

            foreach (var addon in PatchesXML)
            {
                if (addon.isChecked)
                {
                    if (!ADDON_PATCH_NAMES.Contains(addon.Name))
                        if (!File.Exists(PATCHES_DIR + addon.Name + ".xml"))
                            File.Copy(MANAGER_DIR + addon.Name + ".xml", PATCHES_DIR + addon.Name + ".xml");
                }
                else
                {
                    if (File.Exists(PATCHES_DIR + addon.Name + ".xml"))
                        File.Delete(PATCHES_DIR + addon.Name + ".xml");
                }
            }

            ((Storyboard)FindResource("animate")).Begin(successStatePicture);
            ((Storyboard)FindResource("animate")).Begin(PoggiesLabel);
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start(MANAGER_DIR);
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
