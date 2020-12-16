using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Net;
using MiscUtil.Compression.Vcdiff;
using System.Windows.Media.Animation;
using System.Windows.Media;
using BnS_Multitool;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for GameUpdater.xaml
    /// </summary>
    /// 

    public partial class GameUpdater : Page
    {
        private string localVersion;
        private string onlineVersion;
        private BackgroundWorker patchWorker = new BackgroundWorker();
        private string BASE_URL = @"http://live.patcher.bladeandsoul.com/BnS/";
        private string BNS_PATH = SystemConfig.SYS.BNS_DIR;
        private long currentBytes = 0L;
        private long totalBytes = 0L;
        private List<BnSFileInfo> BnSInfoMap;
        TaskCompletionSource<bool> downloadComplete;

        public class BnSFileInfo
        {
            public string path { get; set; }
            public string size { get; set; }
            public string hash { get; set; }
            public string flag { get; set; }
        }

        public GameUpdater()
        {
            InitializeComponent();

            patchWorker.DoWork += new DoWorkEventHandler(PatchGame);
            patchWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PatchGameFinished);
        }

        private void PatchGameFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            FilesProcessed(0);
            DownloadBtn.IsEnabled = true;
            DownloadBtn.Content = "File Check";
            ProgressGrid.Visibility = Visibility.Hidden;
            localVersionLabel.Content = onlineVersion.ToString();
            LocalGameLbl.Foreground = Brushes.Green;
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1, bool showSuffix = true)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "}{1}", 0, showSuffix ? " bytes" : ""); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                (showSuffix) ? SizeSuffixes[mag] : "");
        }

        private void PatchGame(object sender, DoWorkEventArgs e)
        {
            string FileInfoMap_URL = String.Format(@"{0}{1}/Patch/FileInfoMap_BnS_{1}.dat.zip", BASE_URL, onlineVersion);
            string PatchInfo_URL = String.Format(@"{0}{1}/Patch/PatchFileInfo_BnS_{1}.dat.zip", BASE_URL, onlineVersion);

            totalBytes = 0L;
            currentBytes = 0L;

            //We'll need this later
            string[] effectandanimations;
            effectandanimations = SystemConfig.SYS.CLASSES.SelectMany(entries => entries.ANIMATIONS).ToArray();
            effectandanimations = effectandanimations.Concat(SystemConfig.SYS.CLASSES.SelectMany(entries => entries.EFFECTS)).ToArray();
            effectandanimations = effectandanimations.Concat(SystemConfig.SYS.MAIN_UPKS).ToArray();

            WebClient client = new WebClient();
            BnSInfoMap = new List<BnSFileInfo>();

            //We need to make sure the version difference is not greater than 1, if it is that's a whole mess I can't be bothered to code.
            bool canDeltaPatch = ((int.Parse(onlineVersion) - int.Parse(localVersion)) < 2) && SystemConfig.SYS.DELTA_PATCHING == 1 && onlineVersion != localVersion;

            if (canDeltaPatch)
                DltPLbl.Dispatcher.BeginInvoke(new Action(() => { DltPLbl.Visibility = Visibility.Visible; }));
            else
                DltPLbl.Dispatcher.BeginInvoke(new Action(() => { DltPLbl.Visibility = Visibility.Hidden; }));
            try
            {
                if (!RemoteFileExists(PatchInfo_URL))
                    throw new Exception("Patch: " + onlineVersion + " does not exist");

                //Check if our patch manager directory exists, if not create it.
                if (!Directory.Exists(BNS_PATH + @"PatchManager\" + onlineVersion))
                    Directory.CreateDirectory(BNS_PATH + @"PatchManager\" + onlineVersion);

                Dispatchers.textBlock(ProgressBlock, "Fetching FileInfoMap_BnS_" + onlineVersion + ".dat");
                client.DownloadFile(FileInfoMap_URL, Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "FileInfoMap_BnS.dat.zip"));
                Dispatchers.textBlock(ProgressBlock, "Fetching PatchFileInfo_BnS_" + onlineVersion + ".dat");
                client.DownloadFile(PatchInfo_URL, Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "PatchFileInfo_BnS.dat.zip"));

                Dispatchers.textBlock(ProgressBlock, "Decompressing file maps");
                DecompressFileLZMA(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "FileInfoMap_BnS.dat.zip"), Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "FileInfoMap_BnS.dat"));
                DecompressFileLZMA(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "PatchFileInfo_BnS.dat.zip"), Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "PatchFileInfo_BnS.dat"));

                Dispatchers.textBlock(ProgressBlock, String.Format("Parsing {0}", (SystemConfig.SYS.DELTA_PATCHING == 1) ? "PatchFileInfo" : "FileMapInfo"));
                Thread.Sleep(100);

                string configDat = (canDeltaPatch) ? "PatchFileInfo_BnS.dat" : "FileInfoMap_BnS.dat";

                List<string> InfoMapLines = File.ReadLines(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, configDat)).ToList<string>();
                int totalFiles = InfoMapLines.Count<string>();
                int processedFiles = 0;
                //long totalBytes = 0L;
                int threadCount = SystemConfig.SYS.UPDATER_THREADS + 4;

                Parallel.ForEach<string>(InfoMapLines, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (string line)
                {
                    string[] lineData = line.Split(new char[] { ':' });
                    string FilePath = lineData[0];
                    string FileSize = lineData[1];
                    string FileHash = lineData[2];
                    string FileFlag = lineData[3];
                    string currentFilePath;
                    if (canDeltaPatch)
                        currentFilePath = FilePath.Substring(4, FilePath.Contains(".dlt") ? FilePath.Length - 12 : FilePath.Length - 8);
                    else
                        currentFilePath = FilePath;

                    FileInfo fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                    if (!fInfo.Exists && effectandanimations.Contains(Path.GetFileName(currentFilePath)))
                    {
                        currentFilePath = String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath));
                        fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                    }

                    if (canDeltaPatch)
                    {
                        if (FileFlag == "3")
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath))))
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath)));

                            if (fInfo.Exists)
                            {
                                BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                                Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                            }
                            /*
                            if (DownloadAndPatch(Path.Combine(BNS_PATH, currentFilePath), FilePath, true))
                            {
                                if (File.Exists(Path.Combine(BNS_PATH, currentFilePath)))
                                    File.Delete(Path.Combine(BNS_PATH, currentFilePath));

                                File.Move(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath.Substring(0, FilePath.Length - 8))
                                    , Path.Combine(BNS_PATH, currentFilePath));
                                Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                            }
                            */
                        }
                        else if (FileFlag == "5")
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath))))
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath)));

                            BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                            Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                            /*
                            if (DownloadAndPatch(Path.Combine(BNS_PATH, currentFilePath), FilePath, true))
                            {
                                if (File.Exists(Path.Combine(BNS_PATH, currentFilePath)))
                                    File.Delete(Path.Combine(BNS_PATH, currentFilePath));

                                File.Move(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath.Substring(0, FilePath.Length - 4))
                                    , Path.Combine(BNS_PATH, currentFilePath));

                                Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                            }
                            */
                        }
                    }
                    else
                    {
                        //This section (Non-Delta-Patching) acts more of a file repair than an actual game updater / patcher.
                        if (fInfo.Exists)
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath))))
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath)));

                            //Make sure the directory is not the web font directory, for whatever reason this is read-only
                            if (!FilePath.Contains(@"\web"))
                            {
                                if (fInfo.Length.ToString() != FileSize)
                                {
                                    BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                                    Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                                } else if (FileHash != SHA1HASH(Path.Combine(BNS_PATH, currentFilePath)))
                                {
                                    BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                                    Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                                }
                            }
                        }
                        else
                        {
                            BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                            Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                        }
                    }

                    Interlocked.Increment(ref processedFiles);
                    if (!canDeltaPatch)
                    {
                        FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                        Dispatchers.textBlock(ProgressBlock, String.Format("{0} / {1} Scanned", processedFiles, totalFiles));
                    }
                });

                Debug.WriteLine(String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));
                Dispatchers.textBlock(ProgressBlock, String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));

                totalFiles = BnSInfoMap.Count();
                processedFiles = 0;
                PatchingLabel.Dispatcher.BeginInvoke(new Action(() => { PatchingLabel.Visibility = Visibility.Visible; }));

                FilesProcessed(0);

                Dispatchers.labelContent(PatchingLabel, "Downloading...");

                Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (BnSFileInfo file)
                {
                    string fpath = Path.Combine(BNS_PATH, "PatchManager", onlineVersion, file.path);
                    if (!canDeltaPatch)
                        fpath += ".zip";

                    string currentFilePath;
                    if (canDeltaPatch)
                        currentFilePath = file.path.Substring(4, file.path.Contains(".dlt") ? file.path.Length - 12 : file.path.Length - 8);
                    else
                        currentFilePath = file.path;

                    FileInfo fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                    if (!fInfo.Exists && effectandanimations.Contains(Path.GetFileName(currentFilePath)))
                    {
                        currentFilePath = (File.Exists(Path.Combine(BNS_PATH, String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath))))) ? String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath)) : currentFilePath;

                        if (File.Exists(Path.Combine(BNS_PATH, currentFilePath)))
                            fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                    }

                    try
                    {
                        if (File.Exists(fpath))
                            File.Delete(fpath);

                        downloadComplete = new TaskCompletionSource<bool>();
                        if(DownloadContents(String.Format(@"{0}{1}/Patch/{2}", BASE_URL, onlineVersion, (canDeltaPatch) ? file.path : @"/Zip/" + file.path + ".zip"), fpath))
                            processedFiles++;

                        FilesProcessed((int)((double)processedFiles / totalFiles * 100));

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                });

                bool taskIsDone = false;
                //Create a task
                Task.Run(new Action(async () =>
                {
                    FilesProcessed(0);
                    processedFiles = 0;
                    await Task.Delay(1000);

                    Dispatchers.labelContent(PatchingLabel, "Patching");

                    Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (BnSFileInfo file)
                    {
                        string fpath = Path.Combine(BNS_PATH, "PatchManager", onlineVersion, file.path);
                        if (!canDeltaPatch)
                            fpath += ".zip";

                        string currentFilePath;
                        if (canDeltaPatch)
                            currentFilePath = file.path.Substring(4, file.path.Contains(".dlt") ? file.path.Length - 12 : file.path.Length - 8);
                        else
                            currentFilePath = file.path;

                        FileInfo fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                        if (!fInfo.Exists && effectandanimations.Contains(Path.GetFileName(currentFilePath)))
                        {
                            currentFilePath = (File.Exists(Path.Combine(BNS_PATH, String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath))))) ? String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath)) : currentFilePath;

                            if (File.Exists(Path.Combine(BNS_PATH, currentFilePath)))
                                fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                        }

                        try
                        {
                            DecompressFileLZMA(fpath, fpath.Substring(0, fpath.Length - 4));
                            File.Delete(fpath);
                            fpath = fpath.Substring(0, fpath.Length - 4);

                            //Delta Patch
                            if (canDeltaPatch && file.flag == "3")
                            {
                                if (DeltaPatch(Path.Combine(BNS_PATH, currentFilePath), fpath))
                                {
                                    File.Delete(fpath);

                                    if (File.Exists(Path.Combine(BNS_PATH, currentFilePath)))
                                        File.Delete(Path.Combine(BNS_PATH, currentFilePath));

                                    File.Move(fpath.Substring(0, fpath.Length - 4), Path.Combine(BNS_PATH, currentFilePath));
                                }
                            }
                            else
                            {
                                if (fInfo.Exists)
                                    fInfo.Delete();

                                File.Move(fpath, Path.Combine(BNS_PATH, currentFilePath));
                            }
                            processedFiles++;
                            FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                            Dispatchers.textBlock(ProgressBlock, String.Format("{0} ({1}%)", SizeSuffix(totalBytes, 2), (int)((double)processedFiles / totalFiles * 100)));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });

                    Dispatchers.labelContent(PatchingLabel, "Finishing up..");
                    taskIsDone = true;
                }));

                while (!taskIsDone)
                    Thread.Sleep(2000);

                if (File.Exists(Path.Combine(BNS_PATH, "FileInfoMap_BnS.dat")))
                    File.Delete(Path.Combine(BNS_PATH, "FileInfoMap_BnS.dat"));

                File.Move(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, "FileInfoMap_BnS.dat"), Path.Combine(BNS_PATH, "FileInfoMap_BnS.dat"));

                IniHandler hIni = new IniHandler(Path.Combine(BNS_PATH, "VersionInfo_BnS.ini"));
                hIni.Write("VersionInfo", "GlobalVersion", onlineVersion);
                Directory.Delete(Path.Combine(BNS_PATH, "PatchManager", onlineVersion), true);

                //onlineVersion = "165";

            } catch (Exception ex)
            {
                Dispatchers.textBlock(ProgressBlock, ex.Message);
            }
            Thread.Sleep(2500);
        }

        private bool DownloadContents(string url, string path)
        {
            WebClient client = new WebClient();
            try
            {
                client.DownloadFile(new Uri(url), path);
            } catch (WebException ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private void downloadCompleted(object sender, AsyncCompletedEventArgs e) => downloadComplete.SetResult(true);

        private void FilesProcessed(int value)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(1));

            currentProgress.Dispatcher.BeginInvoke(new Action(() => {
                System.Windows.Media.Animation.DoubleAnimation da = new System.Windows.Media.Animation.DoubleAnimation(value, duration);
                currentProgress.BeginAnimation(ProgressBar.ValueProperty, da);
            }));
        }

        private static string SHA1HASH(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (SHA1Managed sha1 = new SHA1Managed())
                    {
                        byte[] hash = sha1.ComputeHash(bs);
                        StringBuilder formatted = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                        {
                            formatted.AppendFormat("{0:x2}", b);
                        }
                        return formatted.ToString();
                    }
                }
            } catch(Exception)
            {
                return "";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            IniHandler VersionInfo_BnS = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR,"VersionInfo_BnS.ini"));
            localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            onlineVersion = Globals.onlineVersionNumber();
            //onlineVersion = "164";

            localVersionLabel.Content = localVersion.ToString();
            currentVersionLabel.Content = String.Format("{0}", (onlineVersion == "") ? "Error" : onlineVersion);

            //Redundant..? Doing it cause fuck it.
            if (onlineVersion == "")
            {
                OnlineGameLbl.Foreground = Brushes.Red;
                onlineVersion = localVersion;
            }
            else
                OnlineGameLbl.Foreground = Brushes.Green;

            if (onlineVersion != localVersion)
                LocalGameLbl.Foreground = Brushes.Red;
            else
            {
                LocalGameLbl.Foreground = Brushes.Green;
                DownloadBtn.Content = "File Check";
            }
        }

        private bool DeltaPatch(string original, string patch)
        {
            string targetFileName = Path.GetFileName(original);

            try
            {
                using (FileStream originalFile = File.OpenRead(original))
                using (FileStream patchFile = File.OpenRead(patch))
                using (FileStream targetFile = File.Open(Path.Combine(Path.GetDirectoryName(patch), targetFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    VcdiffDecoder.Decode(originalFile, patchFile, targetFile);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool RemoteFileExists(string url)
        {
            bool result;
            try
            {
                HttpWebRequest httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Method = "HEAD";
                HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                httpWebResponse.Close();
                result = (httpWebResponse.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                result = false;
            }
            return result;
        }

        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            IniHandler VersionInfo_BnS = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR, "VersionInfo_BnS.ini"));
            localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            DownloadBtn.IsEnabled = false;
            ProgressGrid.Visibility = Visibility.Visible;
            PatchingLabel.Visibility = Visibility.Hidden;
            patchWorker.RunWorkerAsync();
        }

        private static void DecompressFileLZMA(string inFile, string outFile)
        {
            using (FileStream input = new FileStream(inFile, FileMode.Open))
            {
                using (FileStream output = new FileStream(outFile, FileMode.Create))
                {
                    SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                    byte[] properties = new byte[5];
                    if (input.Read(properties, 0, 5) != 5)
                        throw (new Exception("input .lzma is too short"));
                    decoder.SetDecoderProperties(properties);

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = input.ReadByte();
                        if (v < 0)
                            throw (new Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    }
                    long compressedSize = input.Length - input.Position;

                    decoder.Code(input, output, compressedSize, outSize, null);
                }
            }

            File.Delete(inFile); //Cleanup
        }
    }
}
