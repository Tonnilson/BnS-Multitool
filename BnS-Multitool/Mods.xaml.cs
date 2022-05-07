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
            string region = regionFromSelection();

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
            ACCOUNT_CONFIG.appendChangesToConfig();

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

        private void openModFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(modPath);
        }

        private string languageFromSelection()
        {
            string lang;
            switch (LANGUAGE_BOX.SelectedIndex)
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
                case 5:
                    lang = "korean";
                    break;
                default:
                    lang = "English";
                    break;
            }
            return lang;
        }

        private string regionFromSelection()
        {
            string region;
            switch ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case Globals.BnS_Region.TW:
                    region = "NCTAIWAN";
                    break;
                case Globals.BnS_Region.KR:
                    region = "NCSoft";
                    break;
                default:
                    region = "NCWEST";
                    break;
            }
            return region;
        }

        private void handleToggle(object sender, RoutedEventArgs e)
        {
            bool state = ((Button)sender).Name.Contains("_on_");
            foreach (var b in userMods)
                b.isChecked = state;

            CollectionViewSource.GetDefaultView(ModsListBox.DataContext).Refresh();
            applyMods(sender, e);
        }

        List<string> mt_filters = new List<string>(Directory.GetFiles(@".\", "*.mt_filter"));
        private string originalLocalPath;
        private string original_local64;
        private string original_local;

        private void OpenLocalDatEditor(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeOut")).Begin(MainGrid);
            ((Storyboard)FindResource("FadeIn")).Begin(LocalDatGrid);

            customFilters.Items.Clear();
            foreach (string file in mt_filters)
                customFilters.Items.Add(Path.GetFileNameWithoutExtension(file));

            originalLocalPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "contents", "Local", regionFromSelection(), languageFromSelection(), "data");

            try
            {
                if (infoDat.Count == 0)
                {
                    var FileInfoMapName = Path.GetFileName(Directory.EnumerateFiles(SystemConfig.SYS.BNS_DIR).Where(x => x.Contains("FileInfoMap_")).FirstOrDefault());
                    if (String.IsNullOrEmpty(FileInfoMapName))
                        FileInfoMapName = "FileInfoMap_BnS.dat";

                    List<string> FileInfoMap = File.ReadLines(Path.Combine(SystemConfig.SYS.BNS_DIR, FileInfoMapName)).ToList<string>();
                    Parallel.ForEach<string>(FileInfoMap, new ParallelOptions { MaxDegreeOfParallelism = 2 }, delegate (string line)
                    {
                        string[] lineData = line.Split(new char[] { ':' });
                        string FilePath = lineData[0];
                        string FileSize = lineData[1];
                        string FileHash = lineData[2];
                        string FileFlag = lineData[3];

                        if (FilePath.Contains("local.dat") || FilePath.Contains("local64.dat"))
                            infoDat.Add(new InfoDat() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                    });
                }

                var local_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local.dat", regionFromSelection(), languageFromSelection()), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                var local64_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local64.dat", regionFromSelection(), languageFromSelection()), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                if (File.Exists(Path.Combine(originalLocalPath, "local.dat")) || File.Exists(Path.Combine(originalLocalPath, "local64.dat")))
                {
                    FileInfo local_dati = new FileInfo(Path.Combine(originalLocalPath, File.Exists(Path.Combine(originalLocalPath, "local.dat")) ? "local.dat" : "local.dat.bk"));
                    FileInfo local64_dati = new FileInfo(Path.Combine(originalLocalPath, File.Exists(Path.Combine(originalLocalPath, "local.dat")) ? "local64.dat" : "local64.dat.bk"));

                    if (local_dati.Length != long.Parse(local_dat.size))
                    {
                        if (File.Exists(Path.Combine(originalLocalPath, "local.dat.bk")))
                            original_local = Path.Combine(originalLocalPath, "local.dat.bk");
                        else
                            original_local = Path.Combine(originalLocalPath, local_dati.Name);
                    }
                    else
                        original_local = Path.Combine(originalLocalPath, local_dati.Name);

                    if (local64_dati.Length != long.Parse(local64_dat.size))
                    {
                        if (File.Exists(Path.Combine(originalLocalPath, "local64.dat.bk")))
                            original_local64 = Path.Combine(originalLocalPath, "local64.dat.bk");
                        else
                            original_local64 = Path.Combine(originalLocalPath, local64_dati.Name);
                    }
                    else
                        original_local64 = Path.Combine(originalLocalPath, local64_dati.Name);
                } else
                {
                    if(File.Exists(Path.Combine(originalLocalPath, "local.dat.bk")))
                        original_local = Path.Combine(originalLocalPath, "local.dat.bk");

                    if (File.Exists(Path.Combine(originalLocalPath, "local64.dat.bk")))
                        original_local64 = Path.Combine(originalLocalPath, "local64.dat.bk");
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void CloseLocalDat(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeOut")).Begin(LocalDatGrid);
            ((Storyboard)FindResource("FadeIn")).Begin(MainGrid);
            infoDat.Clear();
        }

        private void PresetSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterList.Document.Blocks.Clear();
            switch(((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    FilterList.AppendText(Properties.Resources.playable_ncwest);
                    break;
                case 1:
                    FilterList.AppendText(Properties.Resources.playable_kr);
                    break;
                case 2:
                    FilterList.AppendText(Properties.Resources.minimal);
                    break;
            }
        }

        private void customSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterList.Document.Blocks.Clear();
            try
            {
                string fileContent = File.ReadAllText(mt_filters[((ComboBox)sender).SelectedIndex]);
                FilterList.AppendText(fileContent);
            } catch (Exception)
            {

            }
        }

        private void SaveCustomPreset(object sender, RoutedEventArgs e)
        {
            if(FilterName.Text == "")
            {
                var dialog = new ErrorPrompt("The name of the fitler cannot be empty",false,true);
                dialog.ShowDialog();
                return;
            }

            try
            {
                string filterName = FilterName.Text;
                string filterContents = new System.Windows.Documents.TextRange(FilterList.Document.ContentStart, FilterList.Document.ContentEnd).Text;

                if (File.Exists(filterName + ".mt_filter"))
                    File.Delete(filterName + ".mt_filter");

                File.WriteAllText(filterName + ".mt_filter", filterContents);

                if (mt_filters.IndexOf(String.Format(@".\{0}.mt_filter", filterName)) == -1)
                {
                    mt_filters.Add(String.Format(@".\{0}.mt_filter", filterName));
                    customFilters.Items.Add(filterName);
                }
            } catch (Exception)
            {

            }
        }

        class InfoDat
        {
            public string path { get; set; }
            public string size { get; set; }
            public string hash { get; set; }
            public string flag { get; set; }
        }

        List<InfoDat> infoDat = new List<InfoDat>();

        private async void PatchPreset_Click(object sender, RoutedEventArgs e)
        {
            /*
            if(presetFilters.SelectedIndex == -1)
            {
                var dialog = new ErrorPrompt("You need to select a filter before attempting to patch!", false, true);
                dialog.ShowDialog();
                return;
            }

            if(!File.Exists(original_local) || !File.Exists(original_local64))
            {
                var dialog = new ErrorPrompt(String.Format("The Following dats are missing with no backup source:\n\n{0}{1}", !File.Exists(original_local) ? "local.dat\n" : "", !File.Exists(original_local64) ? "local64.dat" : ""), false, true);
                dialog.ShowDialog();
                return;
            }

            ((Storyboard)FindResource("FadeIn")).Begin(PatchProgress);

            string filterName;
            switch (presetFilters.SelectedIndex)
            {
                case 1:
                    filterName = Properties.Resources.playable_kr;
                    break;
                case 2:
                    filterName = Properties.Resources.minimal;
                    break;
                default:
                    filterName = Properties.Resources.playable_ncwest;
                    break;
            }

            string curRegion = regionFromSelection();
            string curLang = languageFromSelection();
            bool taskIsDone = false;
            await Task.Run(new Action(async () =>
            {
                try
                {
                    var local_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local.dat", curRegion, curLang), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    var local64_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local64.dat", curRegion, curLang), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                    long localSize = 0L;
                    long local64Size = 0L;
                    if(File.Exists(original_local))
                        localSize = new FileInfo(original_local).Length;
                    
                    if(File.Exists(original_local64))
                        local64Size = new FileInfo(original_local64).Length;

                    if (!original_local.Contains(".bk"))
                    {
                        if (localSize != long.Parse(local_dat.size) || !(local_dat.hash.Equals(GameUpdater.SHA1HASH(original_local), StringComparison.CurrentCultureIgnoreCase)))
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                var dialog = new ErrorPrompt("local.dat does not match the current version of the game and there is no backup source, run a file check before trying again.", false, true);
                                dialog.ShowDialog();
                                throw new Exception("Wrong version");
                            });
                        }
                    }

                    if (!original_local64.Contains(".bk"))
                    {
                        if (local64Size != long.Parse(local64_dat.size) || !(local64_dat.hash.Equals(GameUpdater.SHA1HASH(original_local64), StringComparison.CurrentCultureIgnoreCase)))
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                var dialog = new ErrorPrompt("local64.dat does not match the current version of the game and there is no backup source, run a file check before trying again.", false, true);
                                dialog.ShowDialog();
                                throw new Exception("Wrong version");
                            });
                        }
                    }

                    var filter = default(BnsPerformanceFix.Filter);
                    using (var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(filterName)))
                        filter = BnsPerformanceFix.Filter.Load(stream);

                    string temp = "";
                    string temp2  = "";
                    string status;
                    bool patchingBin = false;
                    if (SystemConfig.SYS.patch32 == 1)
                    {
                        Dispatchers.labelContent(PatchingLabel, "Patching local.dat");
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);

                        temp = Path.Combine(tempDir, Path.GetFileName(original_local));
                        File.Copy(original_local, temp);

                        if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "binloader.dll")))
                        {
                            var extracted = BnsPerformanceFix.BnsPerformanceFix.ExtractDat(temp, false);
                            var binfile = Path.Combine(extracted, "localfile.bin");
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalBinInPlace(binfile, filter);

                            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin")))
                                File.Delete(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin"));

                            File.Move(binfile, Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin"));
                            patchingBin = true;
                        }
                        else
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalDatInPlace(temp, filter);

                        Dispatchers.labelContent(PatchingLabel, status);
                        await Task.Delay(2000);
                    }

                    if (SystemConfig.SYS.patch64 == 1)
                    {
                        Dispatchers.labelContent(PatchingLabel, "Patching local64.dat");

                        //local64.dat part
                        var tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir2);

                        temp2 = Path.Combine(tempDir2, Path.GetFileName(original_local64));
                        File.Copy(original_local64, temp2);

                        if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "binloader.dll")))
                        {
                            patchingBin = true;
                            var extracted = BnsPerformanceFix.BnsPerformanceFix.ExtractDat(temp2, true);
                            var binfile = Path.Combine(extracted, "localfile64.bin");
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalBinInPlace(binfile, filter);

                            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin")))
                                File.Delete(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin"));

                            File.Move(binfile, Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin"));
                        }
                        else
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalDatInPlace(temp2, filter);

                        Dispatchers.labelContent(PatchingLabel, status);
                    }


                    await Task.Delay(500);
                    Dispatchers.labelContent(PatchingLabel, "Cleaning up");

                    if (SystemConfig.SYS.patch32 == 1 && !patchingBin)
                    {
                        if (original_local.Contains(".bk"))
                        {
                            //Check the file size to make sure the backup version of local.dat matches what it should be, if not delete and make new backup.
                            if (localSize != long.Parse(local_dat.size))
                            {
                                File.Delete(original_local);
                                File.Move(original_local.Substring(0, original_local.Length - 3), original_local);
                            }

                            File.Delete(original_local.Substring(0, original_local.Length - 3)); //Delete our original
                        }
                        else
                        {
                            if (File.Exists(original_local + ".bk"))
                                File.Delete(original_local + ".bk");

                            File.Move(original_local, original_local + ".bk");
                        }
                        if (!original_local.Contains(".bk"))
                            original_local += ".bk";

                        File.Move(temp, (original_local.Contains(".bk") ? original_local.Substring(0, original_local.Length - 3) : original_local));

                        //Cleanup
                        if (Directory.Exists(Path.GetDirectoryName(temp)))
                            Directory.Delete(Path.GetDirectoryName(temp), true);
                    }


                    if (SystemConfig.SYS.patch64 == 1 && !patchingBin)
                    {
                        if (original_local64.Contains(".bk"))
                        {
                            //Check the file size to make sure the backup version of local.dat matches what it should be, if not delete and make new backup.
                            if (local64Size != long.Parse(local64_dat.size))
                            {
                                File.Delete(original_local64);
                                File.Move(original_local64.Substring(0, original_local64.Length - 3), original_local64);
                            }

                            File.Delete(original_local64.Substring(0, original_local64.Length - 3)); //Delete our original 64
                        }
                        else
                        {
                            if (File.Exists(original_local64 + ".bk"))
                                File.Delete(original_local64 + ".bk");

                            File.Move(original_local64, original_local64 + ".bk");
                        }

                        if (!original_local64.Contains(".bk"))
                            original_local64 += ".bk";

                        File.Move(temp2, (original_local64.Contains(".bk") ? original_local64.Substring(0, original_local64.Length - 3) : original_local64));
                        if (Directory.Exists(Path.GetDirectoryName(temp2)))
                            Directory.Delete(Path.GetDirectoryName(temp2), true);
                    }

                    await Task.Delay(1000);
                    Dispatchers.labelContent(PatchingLabel, "Sedro giveith fps");
                    await Task.Delay(3000);
                    GC.Collect();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                taskIsDone = true;
            }));

            while (!taskIsDone)
               await Task.Delay(50);

            ((Storyboard)FindResource("FadeOut")).Begin(PatchProgress);
            */
        }

        private async void PatchCustom_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (customFilters.SelectedIndex == -1)
            {
                var dialog = new ErrorPrompt("You need to select a filter before attempting to patch!", false, true);
                dialog.ShowDialog();
                return;
            }

            if ((!File.Exists(original_local) || !File.Exists(original_local64)))
            {
                var dialog = new ErrorPrompt(String.Format("The Following dats are missing with no backup source:\n\n{0}{1}", !File.Exists(original_local) ? "local.dat\n" : "", !File.Exists(original_local64) ? "local64.dat" : ""), false, true);
                dialog.ShowDialog();
                return;
            }

            ((Storyboard)FindResource("FadeIn")).Begin(PatchProgress);

            string filterName = mt_filters[customFilters.SelectedIndex];

            string curRegion = regionFromSelection();
            string curLang = languageFromSelection();
            bool taskIsDone = false;
            await Task.Run(new Action(async () =>
            {
                try
                {
                    var local_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local.dat", curRegion, curLang), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    var local64_dat = infoDat.Where(t => t.path.Equals(String.Format(@"contents\local\{0}\{1}\data\local64.dat", curRegion, curLang), StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                    long localSize = 0L;
                    long local64Size = 0L;
                    if (File.Exists(original_local))
                        localSize = new FileInfo(original_local).Length;

                    if (File.Exists(original_local64))
                        local64Size = new FileInfo(original_local64).Length;

                    if (!original_local.Contains(".bk"))
                    {
                        if (localSize != long.Parse(local_dat.size) || !(local_dat.hash.Equals(GameUpdater.SHA1HASH(original_local), StringComparison.CurrentCultureIgnoreCase)))
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                var dialog = new ErrorPrompt("local.dat does not match the current version of the game and there is no backup source, run a file check before trying again.", false, true);
                                dialog.ShowDialog();
                                throw new Exception("Wrong version");
                            });
                        }
                    }

                    if (!original_local64.Contains(".bk"))
                    {
                        if (local64Size != long.Parse(local64_dat.size) || !(local64_dat.hash.Equals(GameUpdater.SHA1HASH(original_local64), StringComparison.CurrentCultureIgnoreCase)))
                        {
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                var dialog = new ErrorPrompt("local64.dat does not match the current version of the game and there is no backup source, run a file check before trying again.", false, true);
                                dialog.ShowDialog();
                                throw new Exception("Wrong version");
                            });
                        }
                    }

                    var filter = default(BnsPerformanceFix.Filter);
                    using (var stream = new FileStream(filterName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        filter = BnsPerformanceFix.Filter.Load(stream);

                    string temp = "";
                    string temp2  = "";
                    string status;
                    bool patchingBin = false;
                    if (SystemConfig.SYS.patch32 == 1)
                    {
                        Dispatchers.labelContent(PatchingLabel, "Patching local.dat");
                        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir);

                        temp = Path.Combine(tempDir, Path.GetFileName(original_local));
                        File.Copy(original_local, temp);

                        if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "binloader.dll")))
                        {
                            var extracted = BnsPerformanceFix.BnsPerformanceFix.ExtractDat(temp, false);
                            var binfile = Path.Combine(extracted, "localfile.bin");
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalBinInPlace(binfile, filter);

                            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin")))
                                File.Delete(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin"));

                            File.Move(binfile, Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", "localfile.bin"));
                            patchingBin = true;
                        }
                        else
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalDatInPlace(temp, filter);

                        Dispatchers.labelContent(PatchingLabel, status);
                        await Task.Delay(2000);
                    }

                    if (SystemConfig.SYS.patch64 == 1)
                    {
                        Dispatchers.labelContent(PatchingLabel, "Patching local64.dat");

                        //local64.dat part
                        var tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempDir2);

                        temp2 = Path.Combine(tempDir2, Path.GetFileName(original_local64));
                        File.Copy(original_local64, temp2);

                        if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "binloader.dll")))
                        {
                            patchingBin = true;
                            var extracted = BnsPerformanceFix.BnsPerformanceFix.ExtractDat(temp2, true);
                            var binfile = Path.Combine(extracted, "localfile64.bin");
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalBinInPlace(binfile, filter);

                            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin")))
                                File.Delete(Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin"));

                            File.Move(binfile, Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", "localfile64.bin"));
                        }
                        else
                            status = BnsPerformanceFix.BnsPerformanceFix.FilterLocalDatInPlace(temp2, filter);

                        Dispatchers.labelContent(PatchingLabel, status);
                    }

                    await Task.Delay(500);
                    Dispatchers.labelContent(PatchingLabel, "Cleaning up");

                    if (SystemConfig.SYS.patch32 == 1 && !patchingBin)
                    {
                        if (original_local.Contains(".bk"))
                        {
                            //Check the file size to make sure the backup version of local.dat matches what it should be, if not delete and make new backup.
                            if (localSize != long.Parse(local_dat.size))
                            {
                                File.Delete(original_local);
                                File.Move(original_local.Substring(0, original_local.Length - 3), original_local);
                            }

                            File.Delete(original_local.Substring(0, original_local.Length - 3)); //Delete our original
                        }
                        else
                        {
                            if (File.Exists(original_local + ".bk"))
                                File.Delete(original_local + ".bk");

                            File.Move(original_local, original_local + ".bk");
                        }

                        if (!original_local.Contains(".bk"))
                            original_local += ".bk";

                        File.Move(temp, (original_local.Contains(".bk") ? original_local.Substring(0, original_local.Length - 3) : original_local));
                        if (Directory.Exists(Path.GetDirectoryName(temp)))
                            Directory.Delete(Path.GetDirectoryName(temp), true);
                    }

                    if (SystemConfig.SYS.patch64 == 1 && !patchingBin)
                    {
                        if (original_local64.Contains(".bk"))
                        {
                            //Check the file size to make sure the backup version of local.dat matches what it should be, if not delete and make new backup.
                            if (local64Size != long.Parse(local64_dat.size))
                            {
                                File.Delete(original_local64);
                                File.Move(original_local64.Substring(0, original_local64.Length - 3), original_local64);
                            }

                            File.Delete(original_local64.Substring(0, original_local64.Length - 3)); //Delete our original 64
                        }
                        else
                        {
                            if (File.Exists(original_local64 + ".bk"))
                                File.Delete(original_local64 + ".bk");

                            File.Move(original_local64, original_local64 + ".bk");
                        }

                        if (!original_local64.Contains(".bk"))
                            original_local64 += ".bk";

                        File.Move(temp2, (original_local64.Contains(".bk") ? original_local64.Substring(0, original_local64.Length - 3) : original_local64));
                        if (Directory.Exists(Path.GetDirectoryName(temp2)))
                            Directory.Delete(Path.GetDirectoryName(temp2), true);
                    }

                    await Task.Delay(1000);
                    Dispatchers.labelContent(PatchingLabel, "Sedro giveith fps");
                    await Task.Delay(3000);
                    GC.Collect();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                taskIsDone = true;
            }));

            while (!taskIsDone)
                await Task.Delay(50);

            ((Storyboard)FindResource("FadeOut")).Begin(PatchProgress);
            */
        }

        private async void LoadDefaultDat(object sender, RoutedEventArgs e)
        {
            /*
            ((Storyboard)FindResource("FadeIn")).Begin(PatchProgress);
            Dispatchers.labelContent(PatchingLabel, "Finding *.dat.bk");
            await Task.Delay(500);

            //contents\local\{0}\{1}\data\local.dat
            var local_dat = Path.Combine(SystemConfig.SYS.BNS_DIR, "contents", "local", regionFromSelection(), languageFromSelection(), "data", "local.dat.bk");
            var local64_dat = Path.Combine(SystemConfig.SYS.BNS_DIR, "contents", "local", regionFromSelection(), languageFromSelection(), "data", "local64.dat.bk");

            Dispatchers.labelContent(PatchingLabel, "Restoring local dats");

            if (SystemConfig.SYS.patch32 == 1)
            {
                if(!File.Exists(local_dat))
                {
                    Dispatchers.labelContent(PatchingLabel, "Backup version of local.dat is missing");
                    await Task.Delay(2000);
                    ((Storyboard)FindResource("FadeOut")).Begin(PatchProgress);
                    return;
                }
                try
                {
                    if (File.Exists(local_dat.Substring(0, local_dat.Length - 3)))
                        File.Delete(local_dat.Substring(0, local_dat.Length - 3));

                    File.Move(local_dat, local_dat.Substring(0, local_dat.Length - 3));
                    original_local = original_local.Substring(0, original_local.Length - 3);
                } catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        var dialog = new ErrorPrompt(String.Format("Error restoring backup\r\r{0}", ex.Message));
                        dialog.Owner = MainWindow.mainWindow;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        dialog.ShowDialog();
                    });
                }
            }

            if (SystemConfig.SYS.patch64 == 1)
            {
                if(!File.Exists(local64_dat))
                {
                    Dispatchers.labelContent(PatchingLabel, "Backup version of local64.dat is missing");
                    await Task.Delay(2000);
                    ((Storyboard)FindResource("FadeOut")).Begin(PatchProgress);
                    return;
                }

                try
                {
                    if (File.Exists(local64_dat.Substring(0, local64_dat.Length - 3)))
                        File.Delete(local64_dat.Substring(0, local64_dat.Length - 3));

                    File.Move(local64_dat, local64_dat.Substring(0, local64_dat.Length - 3));
                    original_local64 = original_local64.Substring(0, original_local64.Length - 3);
                } catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        var dialog = new ErrorPrompt(String.Format("Error restoring backup\r\r{0}", ex.Message));
                        dialog.Owner = MainWindow.mainWindow;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        dialog.ShowDialog();
                    });
                }
            }

            await Task.Delay(1000);

            ((Storyboard)FindResource("FadeOut")).Begin(PatchProgress);
            */
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeOut")).Begin(LocalDatGrid);
            ((Storyboard)FindResource("FadeIn")).Begin(MainGrid);
            infoDat.Clear();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;

            string region = regionFromSelection();
            modPath = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Mods");
            modDestination = Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Content", "Paks", "Mods");

            //These are custom so we need to check if they exist, if not create them for the language.
            if (!Directory.Exists(modPath))
                Directory.CreateDirectory(modPath);

            if (!Directory.Exists(modDestination))
                Directory.CreateDirectory(modDestination);

            await ShowModList();
            LANGUAGE_BOX.SelectedIndex = ACCOUNT_CONFIG.ACCOUNTS.LANGUAGE;
        }

        private void PatchBit(object sender, RoutedEventArgs e)
        {
            CheckBox currentCheckBox = (CheckBox)sender;
            int currentState = ((bool)currentCheckBox.IsChecked) ? 1 : 0;

            SystemConfig.Save();
        }
    }
}
