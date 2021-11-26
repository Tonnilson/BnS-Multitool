using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CG.Web.MegaApiClient;
using ToggleSwitch;
using Button = System.Windows.Controls.Button;
using Path = System.IO.Path;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Modpolice.xaml
    /// </summary>
    /// 
    public partial class Modpolice : Page
    {
        private static string bin_path = @"BNSR\Binaries\Win64\";
        private static string plugins_path = Path.Combine(bin_path, "plugins");
        private static string patches_xml = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\patches.xml";
        private ProgressControl _progressControl;
        private static pluginFileInfo lessLoadingScreen = null;
        private static pluginFileInfo simplemodeTraining = null;
        private static pluginFileInfo highpriorityplugin = null;
        private static pluginFileInfo pluginloader = null;
        private static pluginFileInfo bnspatchPlugin = null;
        private static pluginFileInfo bnsnoggPlugin = null;
        private static pluginFileInfo cutscenePlugin = null;
        private static bool toggleControl = false;

        public class pluginFileInfo
        {
            public DateTime creationTime { get; set; }
            public DateTime modificationTime { get; set; }
            public string hash { get; set; }

            public pluginFileInfo(string file)
            {
                creationTime = File.GetCreationTime(file);
                modificationTime = File.GetLastWriteTime(file);
                hash = CalculateMD5(file);
            }
        }

        static string CalculateMD5(string fileName)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public Modpolice()
        {
            InitializeComponent();
        }

        private async Task checkOnlineVersions()
        {
            #region pluginloader
            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, bin_path, "winmm.dll")))
                pluginloader = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, bin_path, "winmm.dll"));
            else
                pluginloader = null;

            Dispatchers.labelContent(pluginloaderLabel, string.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region bnspatch
            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnspatch.dll")))
                bnspatchPlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnspatch.dll"));
            else
                bnspatchPlugin = null;

            Dispatchers.labelContent(bnspatchLabel, string.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region bnsnogg
            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnsnogg.dll")))
                bnsnoggPlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnsnogg.dll"));
            else
                bnsnoggPlugin = null;

            Dispatchers.labelContent(bnsnogglocalLabel, string.Format("Current: {0}", (bnsnoggPlugin != null) ? bnsnoggPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region highpriorityplugin
            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "highpriority.dll")))
            {
                highpriorityplugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "highpriority.dll"));
                Dispatchers.toggleIsChecked(HighpriorityToggle, true);
            }
            else if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "highpriority.dll.off")))
                highpriorityplugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "highpriority.dll.off"));
            else
                highpriorityplugin = null;

            if (highpriorityplugin == null)
                Dispatchers.buttonVisibility(del_highpriority, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_highpriority, Visibility.Visible);

            Dispatchers.labelContent(HighpriorityCurrentLbl, string.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region cutsceneremoval
            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "cutsceneremoval.dll")))
            {
                cutscenePlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "cutsceneremoval.dll"));
                Dispatchers.toggleIsChecked(CutsceneremovalToggle, true);
            }
            else if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "cutsceneremoval.dll.off")))
                cutscenePlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "cutsceneremoval.dll.off"));
            else
                cutscenePlugin = null;

            if (cutscenePlugin == null)
                Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Visible);

            Dispatchers.labelContent(CutsceneremovalCurrentLbl, string.Format("Current: {0}", (cutscenePlugin != null) ? cutscenePlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            try
            {
                MegaApiClient client = new MegaApiClient(new WebClient(Globals.MEGAAPI_TIMEOUT));
                await client.LoginAnonymousAsync();

                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA")); //Modpolice / Pilao Repo
                IEnumerable<INode> nodes2 = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/ypUTSAzK#LgpImLcfY8ZX86NfSaqmqw")); //My repo

                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnspatch_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^loader3_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode cutscene_node = nodes2.Where(x => x.Type == NodeType.File && x.Name.Contains("UE4_cutscene_removal")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode bnsnogg_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnsnogg_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode highpriority_node = nodes2.Where(x => x.Type == NodeType.File && x.Name.Contains("highpriority")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                await client.LogoutAsync();

                //Format date based off file name.
                Regex pattern = new Regex(@"^(?<fileName>[\w\\.]+)_(?<date>[\d\\.|-]{10})(?<ext>[\w\\.]+)");
                DateTime pluginloader_date = DateTime.Parse(pattern.Match(pluginloader_node.Name).Groups["date"].Value);
                DateTime bnspatch_date = DateTime.Parse(pattern.Match(bnspatch_node.Name).Groups["date"].Value);
                DateTime cutscene_date = DateTime.Parse(pattern.Match(cutscene_node.Name).Groups["date"].Value);
                DateTime bnsnogg_date = DateTime.Parse(pattern.Match(bnsnogg_node.Name).Groups["date"].Value);
                DateTime highpriority_date = DateTime.Parse(pattern.Match(highpriority_node.Name).Groups["date"].Value);

                Dispatchers.labelContent(bnspatchOnlineLbl, string.Format("Online: {0}", bnspatch_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(pluginloaderOnlineLbl, string.Format("Online: {0}", pluginloader_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(CutsceneremovalOnlineLbl, string.Format("Online: {0}", cutscene_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(bnsnoggOnlineLabel, string.Format("Online: {0}", bnsnogg_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(HighpriorityOnlineLbl, string.Format("Online: {0}", highpriority_date.ToString("MM-dd-yy")));

            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            toggleControl = true;
        }

        private void togglePlugin(object sender, RoutedEventArgs e)
        {
            if(!toggleControl)
                return;

            string pluginName;
            switch (((HorizontalToggleSwitch)sender).Name)
            {
                case "simplemodeToggle":
                    pluginName = "fpsbooster";
                    return;
                case "lessloadingToggle":
                    pluginName = "lessloadingscreens";
                    break;
                case "CutsceneremovalToggle":
                    pluginName = "cutsceneremoval";
                    break;
                default:
                    pluginName = "highpriority";
                    break;
            }

            bool isChecked = ((HorizontalToggleSwitch)sender).IsChecked;

            if(isChecked)
            {
                if(File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, pluginName + ".dll.off")))
                {
                    FileInfo pluginFile_x86 = new FileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, pluginName + ".dll.off"));
                    pluginFile_x86.Rename(pluginName + ".dll");
                }
            } else
            {
                if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, pluginName + ".dll")))
                {
                    FileInfo pluginFile_x86 = new FileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, pluginName + ".dll"));
                    pluginFile_x86.Rename(pluginName + ".dll.off");
                }
            }
        }

        private async void installAdditional(object sender, RoutedEventArgs e)
        {
            string pluginName;
            switch (((Button)sender).Name)
            {
                case "simplemodeInstall":
                    pluginName = "fpsbooster";
                    return;
                case "lessloadingInstall":
                    pluginName = "lessloadingscreens";
                    return;
                case "CutsceneremovalInstall":
                    pluginName = "UE4_cutscene_removal";
                    break;
                default:
                    pluginName = "highpriority";
                    break;
            }

            if (pluginName == "simplemodetrainingroom")
                return;

            toggleControl = false;
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            try
            {
                if (!Directory.Exists("modpolice"))
                    Directory.CreateDirectory("modpolice");

                ProgressControl.updateProgressLabel("Logging into Mega");

                MegaApiClient client = new MegaApiClient(new WebClient(Globals.MEGAAPI_TIMEOUT));
                await client.LoginAnonymousAsync();

                ProgressControl.updateProgressLabel("Retrieving file list...");

                IEnumerable<INode> nodes;

                if (pluginName == "UE4_cutscene_removal" || pluginName == "highpriority" || pluginName == "fpsbooster")
                    nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/ypUTSAzK#LgpImLcfY8ZX86NfSaqmqw"));
                else
                    nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                INode currentNode = null;
                IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(string.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));
                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode pluginNode = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^" + pluginName + @"_(?<date>[\d\\.|-]{10}).*").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                if (pluginNode == null)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel("Something went wrong getting the node");
                    await Task.Delay(5000);
                    toggleControl = true;
                    return;
                }

                if (File.Exists(@"modpolice\" + pluginNode.Name))
                    File.Delete(@"modpolice\" + pluginNode.Name);

                currentNode = pluginNode;
                await client.DownloadFileAsync(currentNode, @"modpolice\" + pluginNode.Name, progress);

                ProgressControl.updateProgressLabel("Unzipping: " + pluginNode.Name);
                await Task.Delay(750);
                ExtractZipFileToDirectory(@".\modpolice\" + pluginNode.Name, @".\modpolice", true);

                if (pluginName == "UE4_cutscene_removal")
                    pluginName = "cutsceneremoval";

                ProgressControl.updateProgressLabel("Installing " + pluginName);
                await Task.Delay(750);

                if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_path))
                    Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_path);

                //Delete the current plugin dll
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_path + pluginName + ".dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_path + pluginName + ".dll");

                //Make sure there isn't a plugin dll that is in an off state
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_path + pluginName + ".dll.off"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_path + pluginName + ".dll.off");

                Globals.MoveDirectory(@".\modpolice\BNSR", Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR"));

                ProgressControl.updateProgressLabel("All done");
                await client.LogoutAsync();
                await Task.Delay(750);

            } catch (Exception ex)
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel(ex.Message);
                Debug.WriteLine(ex.ToString());
                await Task.Delay(7000);
            }

            try
            {
                ProgressGrid.Visibility = Visibility.Hidden;
                MainGrid.Visibility = Visibility.Visible;
                ProgressPanel.Children.Clear();
                _progressControl = null;
                toggleControl = true;

                if (pluginName == "fpsbooster")
                {
                    simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_path + "fpsbooster.dll");

                    if (simplemodeTraining != null)
                        Dispatchers.buttonVisibility(del_fpsbooster, Visibility.Visible);

                    //Dispatchers.toggleIsChecked(simplemodeToggle, true);
                    Dispatchers.labelContent(SimplemodeCurrentLbl, string.Format("Current: {0}", (simplemodeTraining != null) ? simplemodeTraining.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else if (pluginName == "lessloadingscreens")
                {
                    lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_path + "lessloadingscreens.dll");

                    if (lessLoadingScreen != null)
                        Dispatchers.buttonVisibility(del_lessloadingscreens, Visibility.Visible);

                    //Dispatchers.toggleIsChecked(lessloadingToggle, true);
                    Dispatchers.labelContent(lessloadingCurrentLbl, string.Format("Current: {0}", (lessLoadingScreen != null) ? lessLoadingScreen.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else if (pluginName == "UE4_cutscene_removal")
                {
                    cutscenePlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "cutsceneremoval.dll"));

                    if (cutscenePlugin != null)
                        Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Visible);

                    Dispatchers.toggleIsChecked(CutsceneremovalToggle, true);
                    Dispatchers.labelContent(CutsceneremovalCurrentLbl, string.Format("Current: {0}", (cutscenePlugin != null) ? cutscenePlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else
                {
                    highpriorityplugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "highpriority.dll"));

                    if (highpriorityplugin != null)
                        Dispatchers.buttonVisibility(del_highpriority, Visibility.Visible);

                    Dispatchers.toggleIsChecked(HighpriorityToggle, true);
                    Dispatchers.labelContent(HighpriorityCurrentLbl, string.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }

                Dispatchers.btnIsEnabled((Button)sender, false);
            } catch (Exception)
            {
                //FUCK ASS CUNT
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine(System.Net.ServicePointManager.SecurityProtocol);
            await Task.Run(async () => await checkOnlineVersions());
        }

        private void refreshSomeShit()
        {
            //Get the file info and display version for pluginloader
            pluginloader = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, bin_path, "winmm.dll"));
            Dispatchers.labelContent(pluginloaderLabel, string.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

            bnspatchPlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnspatch.dll"));
            Dispatchers.labelContent(bnspatchLabel, string.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
        }

        private async void installModPolice(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            if (((Button)sender).Name == "installOnline")
            {
                try
                {
                    ProgressControl.updateProgressLabel("Logging into Mega anonymously...");
                    MegaApiClient client = new MegaApiClient(new WebClient(Globals.MEGAAPI_TIMEOUT));
                    await client.LoginAnonymousAsync();

                    if (!Directory.Exists("modpolice"))
                        Directory.CreateDirectory("modpolice");

                    ProgressControl.updateProgressLabel("Retrieving file list...");
                    IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                    INode currentNode = null;
                    IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(string.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));

                    //Find our latest nodes for download
                    INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                    INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnspatch_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                    INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^loader3_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                    if (pluginloader_node == null)
                    {
                        ProgressControl.errorSadPeepo(Visibility.Visible);
                        ProgressControl.updateProgressLabel("Error retrieving pluginloader");
                    }
                    else
                    {
                        currentNode = pluginloader_node;
                        if (File.Exists(@"modpolice\" + pluginloader_node.Name))
                            File.Delete(@"modpolice\" + pluginloader_node.Name);

                        ProgressControl.errorSadPeepo(Visibility.Hidden);
                        await client.DownloadFileAsync(currentNode, @"modpolice\" + pluginloader_node.Name, progress);
                    }

                    if (pluginloader_node == null)
                    {
                        ProgressControl.errorSadPeepo(Visibility.Visible);
                        ProgressControl.updateProgressLabel("Error retrieving pluginloader");
                    }
                    else
                    {
                        currentNode = bnspatch_node;
                        if (File.Exists(@"modpolice\" + bnspatch_node.Name))
                            File.Delete(@"modpolice\" + bnspatch_node.Name);

                        ProgressControl.errorSadPeepo(Visibility.Hidden);
                        await client.DownloadFileAsync(currentNode, @"modpolice\" + bnspatch_node.Name, progress);
                    }

                    ProgressControl.updateProgressLabel("All done, logging out...");
                    await client.LogoutAsync();
                }
                catch (Exception ex)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel(ex.Message);
                    await Task.Delay(3000);
                }
            }

            try
            {
                string _BNSPATCH_VERSION = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\modpolice\").Select(x => Path.GetFileName(x))
                           .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("bnspatch"))
                                .OrderByDescending(d => new FileInfo(d).Name)
                                    .Select(Name => Path.GetFileNameWithoutExtension(Name)).First().ToString();

                string _PLUGINLOADER_VERSION = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\modpolice\").Select(x => Path.GetFileName(x))
                           .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("loader3"))
                               .OrderByDescending(d => new FileInfo(d).Name)
                                    .Select(Name => Path.GetFileNameWithoutExtension(Name)).First().ToString();

                ProgressControl.updateProgressLabel("Unzipping " + _PLUGINLOADER_VERSION);
                ExtractZipFileToDirectory(@".\modpolice\" + _PLUGINLOADER_VERSION + ".zip", @".\modpolice", true);

                await Task.Delay(750);
                ProgressControl.updateProgressLabel("Unzipping " + _BNSPATCH_VERSION);
                ExtractZipFileToDirectory(@".\modpolice\" + _BNSPATCH_VERSION + ".zip", @".\modpolice", true);

                ProgressControl.updateProgressLabel("Installing Modpolice Core");
                await Task.Delay(750);

                Globals.MoveDirectory(@".\modpolice\BNSR", Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR"));

                ProgressControl.updateProgressLabel("Searching for patches.xml");
                await Task.Delay(500);

                if (!File.Exists(patches_xml))
                {
                    ProgressControl.updateProgressLabel("patches.xml not found, installing...");
                    File.WriteAllText(patches_xml, Properties.Resources.patches);
                }

                ProgressControl.updateProgressLabel("pluginloader & bnspatch successfully installed");
                await Task.Delay(2000);
            } catch (Exception ex)
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel(ex.Message);
                await Task.Delay(7000);
            }

            try
            {
                ProgressGrid.Visibility = Visibility.Hidden;
                MainGrid.Visibility = Visibility.Visible;
                ProgressPanel.Children.Clear();
                _progressControl = null;

                //Get the file info and display version for pluginloader
                pluginloader = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, bin_path, "winmm.dll"));
                Dispatchers.labelContent(pluginloaderLabel, string.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

                bnspatchPlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnspatch.dll"));
                Dispatchers.labelContent(bnspatchLabel, string.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            } catch (Exception)
            {
                //Why are we here, is it just to suffer?
            }
            //refreshSomeShit();
        }

        public static void ExtractZipFileToDirectory(string sourceZipFilePath, string destinationDirectoryName, bool overwrite)
        {
            using (var archive = ZipFile.Open(sourceZipFilePath, ZipArchiveMode.Read))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Trying to extract file outside of destination directory.");

                    if (file.Name == "")
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }
                    file.ExtractToFile(completeFileName, true);
                }
            }
        }

        private void openBinLocation(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64"));
        }

        private async void installBNSNOGG(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            try
            {
                ProgressControl.updateProgressLabel("Logging into Mega anonymously...");
                MegaApiClient client = new MegaApiClient(new WebClient(Globals.MEGAAPI_TIMEOUT));
                await client.LoginAnonymousAsync();

                if (!Directory.Exists("modpolice"))
                    Directory.CreateDirectory("modpolice");

                ProgressControl.updateProgressLabel("Retrieving file list...");
                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                INode currentNode = null;
                IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(string.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));

                //Find our latest nodes for download
                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode bnsnogg_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnsnogg_UE4_(?<date>[\d\\.|-]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                if (bnsnogg_node == null)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel("Error retrieving bnsnogg");
                }
                else
                {
                    currentNode = bnsnogg_node;
                    if (File.Exists(@"modpolice\" + bnsnogg_node.Name))
                        File.Delete(@"modpolice\" + bnsnogg_node.Name);

                    ProgressControl.errorSadPeepo(Visibility.Hidden);
                    await client.DownloadFileAsync(currentNode, @"modpolice\" + bnsnogg_node.Name, progress);
                }

                ProgressControl.updateProgressLabel("All done, logging out...");

                string _BNSNOGG_VERSION = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\modpolice\").Select(x => Path.GetFileName(x))
                           .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("bnsnogg"))
                                .OrderByDescending(d => new FileInfo(d).Name)
                                    .Select(Name => Path.GetFileNameWithoutExtension(Name)).First().ToString();

                ProgressControl.updateProgressLabel("Unzipping " + _BNSNOGG_VERSION);
                ExtractZipFileToDirectory(@".\modpolice\" + _BNSNOGG_VERSION + ".zip", @".\modpolice", true);

                if (!Directory.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path)))
                    Directory.CreateDirectory(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path));

                ProgressControl.updateProgressLabel("Installing bnsnogg");
                await Task.Delay(750);

                Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "modpolice", plugins_path)).ToList().ForEach(f => File.Move(f, Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, Path.GetFileName(f))));

                ProgressControl.updateProgressLabel("bnsnogg successfully installed");
                await client.LogoutAsync();
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel(ex.Message);
                await Task.Delay(7000);
            }

            try
            {
                ProgressGrid.Visibility = Visibility.Hidden;
                MainGrid.Visibility = Visibility.Visible;
                ProgressPanel.Children.Clear();
                _progressControl = null;

                bnsnoggPlugin = new pluginFileInfo(Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, "bnsnogg.dll"));
                Dispatchers.labelContent(bnsnogglocalLabel, string.Format("Current: {0}", (bnsnoggPlugin != null) ? bnsnoggPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            }
            catch (Exception)
            {
                //Why are we here, is it just to suffer?
            }
        }

        private async void deletePlugin(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            string senderName = ((Button)sender).Name.Split('_')[1];
            ProgressControl.updateProgressLabel(string.Format("Removing plugin: {0}", senderName));
            await Task.Delay(500);

            string x86_path = Path.Combine(SystemConfig.SYS.BNS_DIR, plugins_path, senderName + ".dll");

            try
            {
                if (File.Exists(x86_path))
                    File.Delete(x86_path);
                else
                    if (File.Exists(x86_path + ".off"))
                    File.Delete(x86_path + ".off");
            }
            catch (Exception)
            { }

            ProgressControl.updateProgressLabel(string.Format("{0} removed", senderName));

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
            await checkOnlineVersions();
        }
    }
}