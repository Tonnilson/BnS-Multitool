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

namespace System.IO
{
    public static class FileInfoExtensions
    {
        public static void Rename(this FileInfo fileInfo, string newName)
        {
            if (!fileInfo.Exists)
                return;

            if (File.Exists(fileInfo.Directory + @"\" + newName))
                File.Delete(fileInfo.Directory + @"\" + newName);

            fileInfo.MoveTo(Path.Combine(fileInfo.Directory.FullName, newName));
        }
    }
}

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Modpolice.xaml
    /// </summary>
    /// 
    public partial class Modpolice : Page
    {
        private static string bin_x86 = @"\bin\";
        private static string bin_x64 = @"\bin64\";
        private static string plugins_x86 = bin_x86 + @"plugins\";
        private static string plugins_x64 = bin_x64 + @"plugins\";
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

            //BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;
        }

        private async Task checkOnlineVersions()
        {
            #region pluginloader
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + @"\bin\winmm.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + @"\bin64\winmm.dll")))
                pluginloader = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + @"\bin\winmm.dll");
            else
                pluginloader = null;

            Dispatchers.labelContent(pluginloaderLabel, String.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region bnspatch
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll")))
                bnspatchPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");
            else
                bnspatchPlugin = null;

            Dispatchers.labelContent(bnspatchLabel, String.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region bnsnogg
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnsnogg.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll")))
                bnsnoggPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll");
            else
                bnsnoggPlugin = null;

            Dispatchers.labelContent(bnsnogglocalLabel, String.Format("Current: {0}", (bnsnoggPlugin != null) ? bnsnoggPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region lessloadingscreen
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "lessloadingscreens.dll")))
            {
                lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll");
                Dispatchers.toggleIsChecked(lessloadingToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "lessloadingscreens.dll.off")))
                lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll.off");
            else
                lessLoadingScreen = null;

            if (lessLoadingScreen == null)
                Dispatchers.buttonVisibility(del_lessloadingscreens, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_lessloadingscreens, Visibility.Visible);

            Dispatchers.labelContent(lessloadingCurrentLbl, String.Format("Current: {0}", (lessLoadingScreen != null) ? lessLoadingScreen.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region fpsbooster
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "fpsbooster.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "fpsbooster.dll")))
            {
                simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "fpsbooster.dll");
                Dispatchers.toggleIsChecked(simplemodeToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "fpsbooster.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "fpsbooster.dll.off")))
                simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "fpsbooster.dll.off");
            else
                simplemodeTraining = null;

            Dispatchers.labelContent(SimplemodeCurrentLbl, String.Format("Current: {0}", (simplemodeTraining != null) ? simplemodeTraining.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            if (simplemodeTraining == null)
                Dispatchers.buttonVisibility(del_fpsbooster, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_fpsbooster, Visibility.Visible);

            #endregion

            #region highpriorityplugin
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "highpriority.dll")))
            {
                highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll");
                Dispatchers.toggleIsChecked(HighpriorityToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "highpriority.dll.off")))
                highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll.off");
            else
                highpriorityplugin = null;

            if (highpriorityplugin == null)
                Dispatchers.buttonVisibility(del_highpriority, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_highpriority, Visibility.Visible);

            Dispatchers.labelContent(HighpriorityCurrentLbl, String.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region cutsceneremoval
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "cutsceneremoval.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "cutsceneremoval.dll")))
            {
                cutscenePlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "cutsceneremoval.dll");
                Dispatchers.toggleIsChecked(CutsceneremovalToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "cutsceneremoval.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "cutsceneremoval.dll.off")))
                cutscenePlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "cutsceneremoval.dll.off");
            else
                cutscenePlugin = null;

            if (cutscenePlugin == null)
                Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Hidden);
            else
                Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Visible);

            Dispatchers.labelContent(CutsceneremovalCurrentLbl, String.Format("Current: {0}", (cutscenePlugin != null) ? cutscenePlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            try
            {
                var client = new MegaApiClient();
                await client.LoginAnonymousAsync();

                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA")); //Modpolice / Pilao Repo
                IEnumerable<INode> nodes2 = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/ypUTSAzK#LgpImLcfY8ZX86NfSaqmqw")); //My repo

                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode lessloading_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^lessloadingscreens_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode fpsbooster_node = nodes2.Where(x => x.Type == NodeType.File && x.Name.Contains("fpsbooster")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode highpriority_node = nodes2.Where(x => x.Type == NodeType.File && x.Name.Contains("highpriority")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnspatch_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^loader3_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode bnsnogg_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnsnogg_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode cutscene_node = nodes2.Where(x => x.Type == NodeType.File && x.Name.Contains("cutsceneremoval")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                //Format date based off file name.
                Regex pattern = new Regex(@"^(?<fileName>[\w\\.]+)_(?<date>[\d\\.]{10})(?<ext>[\w\\.]+)");
                DateTime highpriority_date = DateTime.Parse(pattern.Match(highpriority_node.Name).Groups["date"].Value);
                DateTime lessloading_date = DateTime.Parse(pattern.Match(lessloading_node.Name).Groups["date"].Value);
                DateTime fpsbooster_date = DateTime.Parse(pattern.Match(fpsbooster_node.Name).Groups["date"].Value);
                DateTime pluginloader_date = DateTime.Parse(pattern.Match(pluginloader_node.Name).Groups["date"].Value);
                DateTime bnspatch_date = DateTime.Parse(pattern.Match(bnspatch_node.Name).Groups["date"].Value);
                DateTime bnsnogg_date = DateTime.Parse(pattern.Match(bnsnogg_node.Name).Groups["date"].Value);
                DateTime cutscene_date = DateTime.Parse(pattern.Match(cutscene_node.Name).Groups["date"].Value);

                Dispatchers.labelContent(lessloadingOnlineLbl, String.Format("Online: {0}", lessloading_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(SimplemodeOnlineLbl, String.Format("Online: {0}", fpsbooster_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(HighpriorityOnlineLbl, String.Format("Online: {0}", highpriority_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(bnspatchOnlineLbl, String.Format("Online: {0}", bnspatch_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(pluginloaderOnlineLbl, String.Format("Online: {0}", pluginloader_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(bnsnoggOnlineLabel, String.Format("Online: {0}", bnsnogg_date.ToString("MM-dd-yy")));
                Dispatchers.labelContent(CutsceneremovalOnlineLbl, String.Format("Online: {0}", cutscene_date.ToString("MM-dd-yy")));

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
                    break;
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

            bool isChecked = (bool)((HorizontalToggleSwitch)sender).IsChecked;

            if(isChecked)
            {
                if(File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll.off") || File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll.off"))
                {
                    FileInfo pluginFile_x86 = new FileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll.off");
                    FileInfo pluginFile_x64 = new FileInfo(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll.off");
                    pluginFile_x86.Rename(pluginName + ".dll");
                    pluginFile_x64.Rename(pluginName + ".dll");
                }
            } else
            {
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll") || File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll"))
                {
                    FileInfo pluginFile_x86 = new FileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll");
                    FileInfo pluginFile_x64 = new FileInfo(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll");
                    pluginFile_x86.Rename(pluginName + ".dll.off");
                    pluginFile_x64.Rename(pluginName + ".dll.off");
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
                    break;
                case "lessloadingInstall":
                    pluginName = "lessloadingscreens";
                    break;
                case "CutsceneremovalInstall":
                    pluginName = "cutsceneremoval";
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
                var client = new MegaApiClient();
                await client.LoginAnonymousAsync();

                ProgressControl.updateProgressLabel("Retrieving file list...");

                IEnumerable<INode> nodes;

                if (pluginName == "cutsceneremoval" || pluginName == "highpriority" || pluginName == "fpsbooster")
                    nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/ypUTSAzK#LgpImLcfY8ZX86NfSaqmqw"));
                else
                    nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                INode currentNode = null;
                IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(String.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));
                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode pluginNode = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^" + pluginName + @"_(?<date>[\d\\.]{10}).*").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

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
                Modpolice.ExtractZipFileToDirectory(@".\modpolice\" + pluginNode.Name, @".\modpolice", true);

                ProgressControl.updateProgressLabel("Installing " + pluginName + " x86");
                await Task.Delay(750);

                if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86))
                    Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x86);

                //Delete the current plugin dll
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll");

                //Make sure there isn't a plugin dll that is in an off state
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll.off"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll.off");

                File.Move(@".\modpolice\bin\plugins\" + pluginName + ".dll", SystemConfig.SYS.BNS_DIR + plugins_x86 + pluginName + ".dll");

                ProgressControl.updateProgressLabel("Installing " + pluginName + " x64");
                await Task.Delay(750);

                if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64))
                    Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x64);

                //Delete the current plugin dll
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll");

                //Make sure there isn't a plugin dll that is in an off state
                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll.off"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll.off");

                File.Move(@".\modpolice\bin64\plugins\" + pluginName + ".dll", SystemConfig.SYS.BNS_DIR + plugins_x64 + pluginName + ".dll");

                ProgressControl.updateProgressLabel("All done");
                await Task.Delay(750);

                await client.LogoutAsync();

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
                toggleControl = true;

                if (pluginName == "fpsbooster")
                {
                    simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "fpsbooster.dll");

                    if (simplemodeTraining != null)
                        Dispatchers.buttonVisibility(del_fpsbooster, Visibility.Visible);

                    Dispatchers.toggleIsChecked(simplemodeToggle, true);
                    Dispatchers.labelContent(SimplemodeCurrentLbl, String.Format("Current: {0}", (simplemodeTraining != null) ? simplemodeTraining.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else if (pluginName == "lessloadingscreens")
                {
                    lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll");

                    if (lessLoadingScreen != null)
                        Dispatchers.buttonVisibility(del_lessloadingscreens, Visibility.Visible);

                    Dispatchers.toggleIsChecked(lessloadingToggle, true);
                    Dispatchers.labelContent(lessloadingCurrentLbl, String.Format("Current: {0}", (lessLoadingScreen != null) ? lessLoadingScreen.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else if (pluginName == "cutsceneremoval")
                {
                    cutscenePlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "cutsceneremoval.dll");

                    if (cutscenePlugin != null)
                        Dispatchers.buttonVisibility(del_cutsceneremoval, Visibility.Visible);

                    Dispatchers.toggleIsChecked(CutsceneremovalToggle, true);
                    Dispatchers.labelContent(CutsceneremovalCurrentLbl, String.Format("Current: {0}", (cutscenePlugin != null) ? cutscenePlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }
                else
                {
                    highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll");

                    if (highpriorityplugin != null)
                        Dispatchers.buttonVisibility(del_highpriority, Visibility.Visible);

                    Dispatchers.toggleIsChecked(HighpriorityToggle, true);
                    Dispatchers.labelContent(HighpriorityCurrentLbl, String.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
                }

                Dispatchers.btnIsEnabled((Button)sender, false);
            } catch (Exception)
            {
                //FUCK ASS CUNT
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () => await checkOnlineVersions());
        }

        private void refreshSomeShit()
        {
            //Get the file info and display version for pluginloader
            pluginloader = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + @"\bin\" + "winmm.dll");
            Dispatchers.labelContent(pluginloaderLabel, String.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

            bnspatchPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");
            Dispatchers.labelContent(bnspatchLabel, String.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
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
                    var client = new MegaApiClient();
                    await client.LoginAnonymousAsync();

                    if (!Directory.Exists("modpolice"))
                        Directory.CreateDirectory("modpolice");

                    ProgressControl.updateProgressLabel("Retrieving file list...");
                    IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                    INode currentNode = null;
                    IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(String.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));

                    //Find our latest nodes for download
                    INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                    INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnspatch_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                    INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^loader3_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

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

                    //pluginloader x86
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll");

                    ProgressControl.updateProgressLabel("Installing Loader3 x86");
                    await Task.Delay(750);

                    File.Move(@".\modpolice\bin\winmm.dll", SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll");

                    //pluginloader x64
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll");

                    ProgressControl.updateProgressLabel("Installing Loader3 x64");
                    await Task.Delay(750);

                    File.Move(@".\modpolice\bin64\winmm.dll", SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll");

                    //bnspatch x86
                    ProgressControl.updateProgressLabel("Checking if plugins folder exists for x86");
                    await Task.Delay(500);

                    if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86))
                        Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x86);

                    ProgressControl.updateProgressLabel("Installing bnspatch x86");
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");

                    File.Move(@".\modpolice\bin\plugins\bnspatch.dll", SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");

                    //bnspatch x64
                    ProgressControl.updateProgressLabel("Checking if plugins folder exists for x64");
                    await Task.Delay(500);

                    if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64))
                        Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x64);

                    ProgressControl.updateProgressLabel("Installing bnspatch x64");
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll");

                    File.Move(@".\modpolice\bin64\plugins\bnspatch.dll", SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll");

                    ProgressControl.updateProgressLabel("Searching for patches.xml");
                    await Task.Delay(500);

                    if(!File.Exists(patches_xml))
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
                pluginloader = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + @"\bin\" + "winmm.dll");
                Dispatchers.labelContent(pluginloaderLabel, String.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

                bnspatchPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");
                Dispatchers.labelContent(bnspatchLabel, String.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
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
                    {
                        throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                    }

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
            Process.Start(((Button)sender).Name == "openbin86" ? SystemConfig.SYS.BNS_DIR + @"\bin" : SystemConfig.SYS.BNS_DIR + @"\bin64");
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
                var client = new MegaApiClient();
                await client.LoginAnonymousAsync();

                if (!Directory.Exists("modpolice"))
                    Directory.CreateDirectory("modpolice");

                ProgressControl.updateProgressLabel("Retrieving file list...");
                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                INode currentNode = null;
                IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(String.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));

                //Find our latest nodes for download
                INode parent_node = nodes.Single(x => x.Type == NodeType.Root);
                INode bnsnogg_node = nodes.Where(x => x.Type == NodeType.File && x.ParentId == parent_node.Id && new Regex(@"^bnsnogg_(?<date>[\d\\.]{10}).*$").IsMatch(x.Name)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                if (bnsnogg_node == null)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel("Error retrieving pluginloader");
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

                //pluginloader x86
                if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x86 + "version.dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + bin_x86 + "version.dll");

                ProgressControl.updateProgressLabel("Installing bnsnogg x86");
                await Task.Delay(750);

                File.Move(@".\modpolice\bin\version.dll", SystemConfig.SYS.BNS_DIR + bin_x86 + "version.dll");

                //pluginloader x64
                if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x64 + "version.dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + bin_x64 + "version.dll");

                ProgressControl.updateProgressLabel("Installing bnsnogg x64");
                await Task.Delay(750);

                File.Move(@".\modpolice\bin64\version.dll", SystemConfig.SYS.BNS_DIR + bin_x64 + "version.dll");

                if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86))
                    Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x86);

                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnsnogg.dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnsnogg.dll");

                File.Move(@".\modpolice\bin\plugins\bnsnogg.dll", SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnsnogg.dll");

                if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64))
                    Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x64);

                if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll"))
                    File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll");

                File.Move(@".\modpolice\bin64\plugins\bnsnogg.dll", SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll");

                ProgressControl.updateProgressLabel("bnsnogg successfully installed");
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

                bnsnoggPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnsnogg.dll");
                Dispatchers.labelContent(bnsnogglocalLabel, String.Format("Current: {0}", (bnsnoggPlugin != null) ? bnsnoggPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
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
            ProgressControl.updateProgressLabel(String.Format("Removing plugin: {0}", senderName));
            await Task.Delay(500);

            string x86_path = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin", "plugins", senderName + ".dll");
            string x64_path = Path.Combine(SystemConfig.SYS.BNS_DIR, "bin64", "plugins", senderName + ".dll");

            try
            {
                if (File.Exists(x86_path))
                    File.Delete(x86_path);
                else
                    if (File.Exists(x86_path + ".off"))
                    File.Delete(x86_path + ".off");

                if (File.Exists(x64_path))
                    File.Delete(x64_path);
                else
                    if (File.Exists(x64_path + ".off"))
                    File.Delete(x64_path + ".off");
            }
            catch (Exception)
            { }

            ProgressControl.updateProgressLabel(String.Format("{0} removed", senderName));
            await checkOnlineVersions();

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }
    }
}