using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CG.Web.MegaApiClient;
using Path = System.IO.Path;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Modpolice.xaml
    /// </summary>
    public partial class Modpolice : Page
    {
        private static string bin_x86 = @"\bin\";
        private static string bin_x64 = @"\bin64\";
        private static string plugins_x86 = bin_x86 + @"plugins\";
        private static string plugins_x64 = bin_x64 + @"plugins\";
        private static string patches_xml = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BnS\patches.xml";
        private ProgressControl _progressControl;
        private static bool is_winnmm_installed;
        private static bool is_plugins_installed;

        public Modpolice()
        {
            InitializeComponent();

            BNS_LOCATION_BOX.Text = SystemConfig.SYS.BNS_DIR;
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

        private async void checkForUpdate( object sender, RoutedEventArgs e)
        {
            bool _bnspatchUpdate = false;
            bool _pluginloaderUpdate = false;

            await Task.Run(async () =>
            {
                if (!Directory.Exists("modpolice")) return;

                try
                {
                    var client = new MegaApiClient();
                    await client.LoginAnonymousAsync();

                    IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri("https://mega.nz/folder/WXhzUZ7Y#XzlqkPa8DU4X8xrILQDdZA"));
                    INode bnspatch_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("bnspatch")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();
                    INode pluginloader_node = nodes.Where(x => x.Type == NodeType.File && x.Name.Contains("pluginloader")).OrderByDescending(t => t.ModificationDate).FirstOrDefault();

                    if (pluginloader_node == null) return;
                    if (bnspatch_node == null) return;

                    string _BNSPATCH_VERSION = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\modpolice\").Select(x => Path.GetFileName(x))
                           .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("bnspatch"))
                                .OrderByDescending(d => new FileInfo(d).Name)
                                    .Select(Name => Path.GetFileName(Name)).First().ToString();

                    string _PLUGINLOADER_VERSION = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\modpolice\").Select(x => Path.GetFileName(x))
                               .Where(Name => Path.GetExtension(Name) == ".zip" && Name.Contains("pluginloader"))
                                   .OrderByDescending(d => new FileInfo(d).Name)
                                        .Select(Name => Path.GetFileName(Name)).First().ToString();

                    if(bnspatch_node.Name != _BNSPATCH_VERSION)
                        _bnspatchUpdate = true;

                    if (pluginloader_node.Name != _PLUGINLOADER_VERSION)
                        _pluginloaderUpdate = true;

                } catch (Exception)
                { }
            });

            if (_bnspatchUpdate)
                bnspatchLabel.Content = "BNS Patch: UPDATE AVAILABLE";
            if (_pluginloaderUpdate)
                pluginloaderLabel.Content = "Plugin Loader: UPDATE AVAILABLE";
        }

        private async void downloadClick(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

            await Task.Run(async () =>
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
                        ProgressControl.updateProgressLabel("Errorr retrieving pluginloader");
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
                        ProgressControl.updateProgressLabel("Errorr retrieving pluginloader");
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
                    ProgressControl.updateProgressLabel("Peepo berry happy!");
                    Thread.Sleep(1500);
                }
                catch (Exception ex)
                {
                    ProgressControl.errorSadPeepo(Visibility.Visible);
                    ProgressControl.updateProgressLabel(ex.Message);
                    Thread.Sleep(4000);
                }
            });

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            refreshSomeShit();
        }

        private void refreshSomeShit()
        {
            is_winnmm_installed = (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll"));
            is_plugins_installed = (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll") && File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll"));

            if (!is_winnmm_installed)
                pluginloaderLabel.Content = "Plugin Loader: Not installed";

            if (!is_plugins_installed)
                bnspatchLabel.Content = "BNS Patch: Not installed";
        }

        private async void installModPolice(object sender, RoutedEventArgs e)
        {
            _progressControl = new ProgressControl();
            ProgressGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            ProgressPanel.Children.Add(_progressControl);

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

                await Task.Run(() =>
                {
                    ProgressControl.updateProgressLabel("Unzipping " + _PLUGINLOADER_VERSION);
                    ExtractZipFileToDirectory(@".\modpolice\" + _PLUGINLOADER_VERSION + ".zip", @".\modpolice", true);

                    Thread.Sleep(750);
                    ProgressControl.updateProgressLabel("Unzipping " + _BNSPATCH_VERSION);
                    ExtractZipFileToDirectory(@".\modpolice\" + _BNSPATCH_VERSION + ".zip", @".\modpolice", true);


                    //pluginloader x86
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll");

                    ProgressControl.updateProgressLabel("Installing pluginloader x86");
                    Thread.Sleep(750);

                    File.Move(@".\modpolice\bin\winmm.dll", SystemConfig.SYS.BNS_DIR + bin_x86 + "winmm.dll");

                    //pluginloader x64
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll");

                    ProgressControl.updateProgressLabel("Installing pluginloader x64");
                    Thread.Sleep(750);

                    File.Move(@".\modpolice\bin64\winmm.dll", SystemConfig.SYS.BNS_DIR + bin_x64 + "winmm.dll");

                    //bnspatch x86
                    ProgressControl.updateProgressLabel("Checking if plugins folder exists for x86");
                    Thread.Sleep(500);

                    if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86))
                        Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x86);

                    ProgressControl.updateProgressLabel("Installing bnspatch x86");
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");

                    File.Move(@".\modpolice\bin\plugins\bnspatch.dll", SystemConfig.SYS.BNS_DIR + plugins_x86 + "bnspatch.dll");

                    //bnspatch x64
                    ProgressControl.updateProgressLabel("Checking if plugins folder exists for x64");
                    Thread.Sleep(500);

                    if (!Directory.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64))
                        Directory.CreateDirectory(SystemConfig.SYS.BNS_DIR + plugins_x64);

                    ProgressControl.updateProgressLabel("Installing bnspatch x64");
                    if (File.Exists(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll"))
                        File.Delete(SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll");

                    File.Move(@".\modpolice\bin64\plugins\bnspatch.dll", SystemConfig.SYS.BNS_DIR + plugins_x64 + "bnspatch.dll");

                    ProgressControl.updateProgressLabel("Searching for patches.xml");
                    Thread.Sleep(500);

                    if(!File.Exists(patches_xml))
                    {
                        ProgressControl.updateProgressLabel("patches.xml not found, installing...");
                        File.WriteAllText(patches_xml, Properties.Resources.patches);
                    }

                    ProgressControl.updateProgressLabel("pluginloader & bnspatch successfully installed");
                    Thread.Sleep(2000);
                });
            } catch (Exception ex)
            {
                ProgressControl.errorSadPeepo(Visibility.Visible);
                ProgressControl.updateProgressLabel(ex.Message);
                Thread.Sleep(7000);
            }

            ProgressGrid.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Visible;
            ProgressPanel.Children.Clear();
            _progressControl = null;

            pluginloaderLabel.Content = "Plugin Loader: Installed";
            bnspatchLabel.Content = "BNS Patch: Installed";

            refreshSomeShit();
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
    }
}