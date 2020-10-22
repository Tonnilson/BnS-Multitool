using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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
        private static bool toggleControl = false;

        public class pluginFileInfo
        {
            public DateTime creationTime { get; set; }
            public DateTime modificationTime { get; set; }

            public pluginFileInfo(string file)
            {
                creationTime = File.GetCreationTime(file);
                modificationTime = File.GetLastWriteTime(file);
            }
        }

        public Modpolice()
        {
            InitializeComponent();

            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;
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

            #region lessloadingscreen
            Dispatchers.btnIsEnabled(lessloadingInstall, false);
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "lessloadingscreens.dll")))
            {
                lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll");
                Dispatchers.toggleIsChecked(lessloadingToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "lessloadingscreens.dll.off")))
                lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll.off");

            Dispatchers.labelContent(lessloadingCurrentLbl, String.Format("Current: {0}", (lessLoadingScreen != null) ? lessLoadingScreen.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region simplemodetrainingroom
            Dispatchers.btnIsEnabled(simplemodeInstall, false);
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "simplemodetrainingroom.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "simplemodetrainingroom.dll")))
            {
                simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "simplemodetrainingroom.dll");
                Dispatchers.toggleIsChecked(simplemodeToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "simplemodetrainingroom.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "simplemodetrainingroom.dll.off")))
                simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "simplemodetrainingroom.dll.off");

            Dispatchers.labelContent(SimplemodeCurrentLbl, String.Format("Current: {0}", (simplemodeTraining != null) ? simplemodeTraining.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            #region highpriorityplugin
            Dispatchers.btnIsEnabled(HighpriorityInstall, false);
            if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "highpriority.dll")))
            {
                highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll");
                Dispatchers.toggleIsChecked(HighpriorityToggle, true);
            }
            else if ((File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll.off") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "highpriority.dll.off")))
                highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll.off");

            Dispatchers.labelContent(HighpriorityCurrentLbl, String.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            #endregion

            try
            {
                var client = new MegaApiClient();
                await client.LoginAnonymousAsync();

                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));
                INode lessloading_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("lessloadingscreens")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode simplemode_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("simplemodetrainingroom")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode highpriority_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("highpriority")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("bnspatch")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("pluginloader")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                #region checkforlessloadingscreens
                if (lessLoadingScreen != null)
                {
                    if (lessLoadingScreen.creationTime < lessloading_node.ModificationDate)
                        Dispatchers.btnIsEnabled(lessloadingInstall, true);
                    else
                    {
                        Dispatchers.labelContent(lessloadingCurrentLbl, String.Format("Current: {0}", lessloading_node.ModificationDate.Value.ToString("MM-dd-yy")));
                        Dispatchers.btnIsEnabled(lessloadingInstall, false);
                    }
                }
                else
                    Dispatchers.btnIsEnabled(lessloadingInstall, true);
                #endregion
                #region checkforsimplemode
                if (simplemodeTraining != null)
                {
                    if (simplemodeTraining.creationTime < simplemode_node.ModificationDate)
                        Dispatchers.btnIsEnabled(simplemodeInstall, true);
                    else
                    {
                        Dispatchers.labelContent(SimplemodeCurrentLbl, String.Format("Current: {0}", simplemode_node.ModificationDate.Value.ToString("MM-dd-yy")));
                        Dispatchers.btnIsEnabled(simplemodeInstall, false);
                    }
                }
                else
                    Dispatchers.btnIsEnabled(simplemodeInstall, true);
                #endregion
                #region checkforhighpriority
                if (highpriorityplugin != null)
                {
                    if (highpriorityplugin.creationTime < highpriority_node.ModificationDate)
                        Dispatchers.btnIsEnabled(HighpriorityInstall, true);
                    else
                    {
                        Dispatchers.labelContent(HighpriorityCurrentLbl, String.Format("Current: {0}", highpriority_node.ModificationDate.Value.ToString("MM-dd-yy")));
                        Dispatchers.btnIsEnabled(HighpriorityInstall, false);
                    }
                }
                else
                    Dispatchers.btnIsEnabled(HighpriorityInstall, true);
                #endregion

                Dispatchers.labelContent(lessloadingOnlineLbl, String.Format("Online: {0}", lessloading_node.ModificationDate.Value.ToString("MM-dd-yy")));
                Dispatchers.labelContent(SimplemodeOnlineLbl, String.Format("Online: {0}", simplemode_node.ModificationDate.Value.ToString("MM-dd-yy")));
                Dispatchers.labelContent(HighpriorityOnlineLbl, String.Format("Online: {0}", highpriority_node.ModificationDate.Value.ToString("MM-dd-yy")));
                Dispatchers.labelContent(bnspatchOnlineLbl, String.Format("Online: {0}", bnspatch_node.ModificationDate.Value.ToString("MM-dd-yy")));
                Dispatchers.labelContent(pluginloaderOnlineLbl, String.Format("Online: {0}", pluginloader_node.ModificationDate.Value.ToString("MM-dd-yy")));
            } catch (Exception)
            { 

            }

            toggleControl = true;
        }

        private void browseBnSLocation(object sender, RoutedEventArgs e)
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

            refreshSomeShit();
        }

        private void updateLocationsOnChange(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox currentBox = (System.Windows.Controls.TextBox)sender;

            SystemConfig.SYS.BNS_DIR = currentBox.Text;
            SystemConfig.appendChangesToConfig();

            refreshSomeShit();
        }

        private void togglePlugin(object sender, RoutedEventArgs e)
        {
            if(!toggleControl)
                return;

            string pluginName;
            switch (((HorizontalToggleSwitch)sender).Name)
            {
                case "simplemodeToggle":
                    pluginName = "simplemodetrainingroom";
                    break;
                case "lessloadingToggle":
                    pluginName = "lessloadingscreens";
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
                    pluginName = "simplemodetrainingroom";
                    break;
                case "lessloadingInstall":
                    pluginName = "lessloadingscreens";
                    break;
                default:
                    pluginName = "highpriority";
                    break;
            }

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
                IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));

                INode currentNode = null;
                IProgress<double> progress = new Progress<double>(x => ProgressControl.updateProgressLabel(String.Format("Downloading: {0} ({1}%)", currentNode.Name, Math.Round(x))));
                INode pluginNode = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains(pluginName)).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

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

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
            toggleControl = true;

            if(pluginName == "simplemodetrainingroom")
            {
                simplemodeTraining = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "simplemodetrainingroom.dll");
                Dispatchers.toggleIsChecked(simplemodeToggle, true);
                Dispatchers.labelContent(SimplemodeCurrentLbl, String.Format("Current: {0}", (simplemodeTraining != null) ? simplemodeTraining.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            } else if(pluginName == "lessloadingscreens")
            {
                lessLoadingScreen = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "lessloadingscreens.dll");
                Dispatchers.toggleIsChecked(lessloadingToggle, true);
                Dispatchers.labelContent(lessloadingCurrentLbl, String.Format("Current: {0}", (lessLoadingScreen != null) ? lessLoadingScreen.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            } else
            {
                highpriorityplugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "highpriority.dll");
                Dispatchers.toggleIsChecked(HighpriorityToggle, true);
                Dispatchers.labelContent(HighpriorityCurrentLbl, String.Format("Current: {0}", (highpriorityplugin != null) ? highpriorityplugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));
            }

            Dispatchers.btnIsEnabled((Button)sender, false);
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
                    INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("bnspatch")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                    INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("pluginloader")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

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
                           .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("pluginloader"))
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

                    ProgressControl.updateProgressLabel("Installing pluginloader x86");
                    await Task.Delay(750);

                    File.Move(@".\modpolice\bin\winmm.dll", SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll");

                    //pluginloader x64
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll");

                    ProgressControl.updateProgressLabel("Installing pluginloader x64");
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

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;

            //Get the file info and display version for pluginloader
            pluginloader = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + @"\bin\" + "winmm.dll");
            Dispatchers.labelContent(pluginloaderLabel, String.Format("Current: {0}", (pluginloader != null) ? pluginloader.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

            bnspatchPlugin = new pluginFileInfo(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");
            Dispatchers.labelContent(bnspatchLabel, String.Format("Current: {0}", (bnspatchPlugin != null) ? bnspatchPlugin.modificationTime.ToString("MM-dd-yy") : "Not Installed"));

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
    }
}