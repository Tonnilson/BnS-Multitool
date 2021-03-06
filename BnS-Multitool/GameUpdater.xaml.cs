﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Net;
using MiscUtil.Compression.Vcdiff;
using System.Windows.Media;
using BnS_Multitool.Extensions;
using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for GameUpdater.xaml
    /// </summary>

    public partial class GameUpdater : Page
    {
        private string localVersion;
        private string onlineVersion;
        private BackgroundWorker patchWorker = new BackgroundWorker();
        private string BASE_URL = @"http://d37ob46rk09il3.cloudfront.net/BnS/";
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

        public enum PatchFile_FlagType
        {
            Unknown, // 0
            UnChanged, // 1
            Changed, // 2
            ChangedDiff, // 3
            ChangedOrigial, // 4
            Added // 5
        }

        public class MultipartArchives
        {
            public string File { get; set; }
            public string Directory { get; set; }
            public List<string> Archives { get; set; }
        }

        public GameUpdater()
        {
            InitializeComponent();

            patchWorker.DoWork += new DoWorkEventHandler(PatchGame);
            patchWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PatchGameFinished);
            ServicePointManager.DefaultConnectionLimit = 30; //Raise the concurrent connection limit for WebClient
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
            string FileInfoStr = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.KR) ? "FileInfoMap_BNS_KOR.dat" : "FileInfoMap_BnS.dat";
            string PatchInfoStr = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.KR) ? "PatchFileInfo_BNS_KOR.dat" : "PatchFileInfo_BnS.dat";

            string FileInfoMap_URL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, onlineVersion, FileInfoStr.Substring(0, FileInfoStr.Length - 4));
            string PatchInfo_URL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, onlineVersion, PatchInfoStr.Substring(0, PatchInfoStr.Length - 4));

            totalBytes = 0L;
            currentBytes = 0L;

            //We'll need this later
            string[] effectandanimations;
            effectandanimations = SystemConfig.SYS.CLASSES.SelectMany(entries => entries.ANIMATIONS).ToArray();
            effectandanimations = effectandanimations.Concat(SystemConfig.SYS.CLASSES.SelectMany(entries => entries.EFFECTS)).ToArray();
            effectandanimations = effectandanimations.Concat(SystemConfig.SYS.MAIN_UPKS).ToArray();

            BnSInfoMap = new List<BnSFileInfo>();
            List<MultipartArchives> partArchives = new List<MultipartArchives>();

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
                if (!DownloadContents(FileInfoMap_URL, Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FileInfoStr + ".zip")))
                    throw new Exception("Failed to download FileInfoMap_BnS.dat.zip");

                Dispatchers.textBlock(ProgressBlock, "Fetching PatchFileInfo_BnS_" + onlineVersion + ".dat");
                if (!DownloadContents(PatchInfo_URL, Path.Combine(BNS_PATH, "PatchManager", onlineVersion, PatchInfoStr + ".zip")))
                    throw new Exception("Failed to downlooad PatchFileInfo_BnS.dat.zip");

                Dispatchers.textBlock(ProgressBlock, "Decompressing file maps");
                DecompressFileLZMA(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FileInfoStr + ".zip"), Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FileInfoStr));
                DecompressFileLZMA(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, PatchInfoStr + ".zip"), Path.Combine(BNS_PATH, "PatchManager", onlineVersion, PatchInfoStr));

                Dispatchers.textBlock(ProgressBlock, String.Format("Parsing {0}", (SystemConfig.SYS.DELTA_PATCHING == 1) ? "PatchFileInfo" : "FileMapInfo"));
                Thread.Sleep(100);

                string configDat = (canDeltaPatch) ? PatchInfoStr : FileInfoStr;

                List<string> InfoMapLines = File.ReadLines(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, configDat)).ToList<string>();
                List<string> local_hashes = File.ReadLines(Path.Combine(BNS_PATH, Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "FileInfoMap_*.dat").First()))
                    .Where(x => Path.GetFileName(x.Split(new char[] { ':' })[0]).Contains("local")).ToList<string>();

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

                    //Check if the file is an animation or effect animation and reset the path if it is so they're updated.
                    if (!fInfo.Exists && effectandanimations.Contains(Path.GetFileName(currentFilePath)))
                    {
                        currentFilePath = String.Format(@"contents\bns\backup\{0}", Path.GetFileName(currentFilePath));
                        fInfo = new FileInfo(Path.Combine(BNS_PATH, currentFilePath));
                    }

                    if (canDeltaPatch)
                    {
                        /*
                         * Check for modified local*.dat file
                         * If modified or not matching current local hash flag it
                        */
                        if(Path.GetFileName(currentFilePath).Contains("local") && fInfo.Exists && (PatchFile_FlagType)int.Parse(FileFlag) == PatchFile_FlagType.ChangedOrigial)
                        {
                            Debug.WriteLine("Checking {0}", currentFilePath);
                            string localFileInfo = local_hashes.First(x => x.Split(new char[] { ':' })[0] == currentFilePath);
                            string localHash = SHA1HASH(Path.Combine(BNS_PATH, currentFilePath));
                            if(localHash != localFileInfo.Split(new char[] { ':' })[2])
                            {
                                FilePath = Path.Combine("Zip", FilePath.Substring(4, FilePath.Length - 4));
                                FileFlag = "5";
                                Debug.WriteLine("File: {0}", FilePath);
                            }
                        } else if (Path.GetFileName(currentFilePath).Contains("local") && (PatchFile_FlagType)int.Parse(FileFlag) == PatchFile_FlagType.ChangedDiff)
                        {
                            string localFileInfo = local_hashes.First(x => x.Split(new char[] { ':' })[0] == currentFilePath);
                            string localHash = SHA1HASH(Path.Combine(BNS_PATH, currentFilePath));
                            if (localHash != localFileInfo.Split(new char[] { ':' })[2])
                                FileFlag = "0";
                        }

                        //181\contents\Local\NCWEST\ENGLISH\data\local.dat.dlt.zip:15709460:e4e70b66a7f28f899d9b9bfa0cf3bbcbb8d44e4f:3
                        //Zip\contents\Local\NCWEST\ENGLISH\data\local.dat.zip:16323936:3ec45a6e1004ea7a70878f540adb7bda60b2346b:4
                        if ((PatchFile_FlagType)int.Parse(FileFlag) == PatchFile_FlagType.ChangedDiff)
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath))))
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath)));

                            if (fInfo.Exists)
                            {
                                BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                                Interlocked.Add(ref totalBytes, long.Parse(FileSize));
                            }
                        }
                        else if ((PatchFile_FlagType)int.Parse(FileFlag) == PatchFile_FlagType.Added)
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath))))
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FilePath)));

                            BnSInfoMap.Add(new BnSFileInfo() { path = FilePath, size = FileSize, hash = FileHash, flag = FileFlag });
                            Interlocked.Add(ref totalBytes, long.Parse(FileSize));
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
                                if (FileHash != SHA1HASH(Path.Combine(BNS_PATH, currentFilePath)))
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

                //Debug.WriteLine(String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));
                Dispatchers.textBlock(ProgressBlock, String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));

                totalFiles = BnSInfoMap.Count();
                processedFiles = 0;
                PatchingLabel.Dispatcher.BeginInvoke(new Action(() => { PatchingLabel.Visibility = Visibility.Visible; }));

                FilesProcessed(0);
                Dispatchers.labelContent(PatchingLabel, "Downloading...");
                Thread.Sleep(2000); //Create some slack for our progress bar to reset fully (visual).

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
                        if(DownloadContents(String.Format(@"{0}{1}/Patch/{2}", BASE_URL, onlineVersion, (canDeltaPatch) ? file.path : @"Zip/" + file.path + ".zip"), fpath))
                            Interlocked.Increment(ref processedFiles);

                        //Debug.WriteLine("{0}", file.path);
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

                                //Cleanup the local bk
                                if (currentFilePath.Contains("local") && currentFilePath.EndsWith("dat"))
                                    if (File.Exists(Path.Combine(BNS_PATH, currentFilePath + ".bk")))
                                        File.Delete(Path.Combine(BNS_PATH, currentFilePath + ".bk"));
                            }
                            Interlocked.Increment(ref processedFiles);
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

                if (File.Exists(Path.Combine(BNS_PATH, FileInfoStr)))
                    File.Delete(Path.Combine(BNS_PATH, FileInfoStr));

                File.Move(Path.Combine(BNS_PATH, "PatchManager", onlineVersion, FileInfoStr), Path.Combine(BNS_PATH, FileInfoStr));

                IniHandler hIni = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
                hIni.Write("VersionInfo", "GlobalVersion", onlineVersion);
                Directory.Delete(Path.Combine(BNS_PATH, "PatchManager", onlineVersion), true);

                //onlineVersion = "177";

            } catch (Exception ex)
            {
                Dispatchers.textBlock(ProgressBlock, ex.Message);
            }
            Thread.Sleep(2500);
        }

        private bool DownloadContents(string url, string path)
        {
            WebClient client = new WebClient{Proxy = null};
            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36"); //Set a user agent incase they are disallowing connections with no agent specified.

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                client.DownloadFile(new Uri(url), path);
            } catch (WebException ex)
            {
                Debug.WriteLine(url);
                //Maybe it was a fluke, lets try again.
                using (new WebClient())
                {
                    try
                    {
                        client.DownloadFile(new Uri(url), path);
                    }
                    catch (WebException)
                    {
                        return false;
                    }
                }
            }
            finally
            {
                client.Dispose();
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

        public static string SHA1HASH(string filePath)
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
                return String.Empty;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.KR)
                BASE_URL = @"http://bnskor.ncupdate.com/BNS_LIVE/";
            else
                BASE_URL = @"http://d37ob46rk09il3.cloudfront.net/BnS/";

            IniHandler VersionInfo_BnS = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
            localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            onlineVersion = Globals.onlineVersionNumber();
            //onlineVersion = "183";

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
                    VcdiffDecoder.Decode(originalFile, patchFile, targetFile);

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
            HttpWebRequest httpWebRequest;
            HttpWebResponse httpWebResponse = null;
            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Method = "HEAD";
                httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                result = httpWebResponse.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                result = false;
            }
            finally
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();
                    httpWebResponse.Dispose();
                }
            }
            return result;
        }

        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            IniHandler VersionInfo_BnS = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
            localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            DownloadBtn.IsEnabled = false;
            ProgressGrid.Visibility = Visibility.Visible;
            PatchingLabel.Visibility = Visibility.Hidden;
            patchWorker.RunWorkerAsync();
        }

        private static void DecompressStreamLZMA(string directory, List<string> files, string outFile)
        {
            if (File.Exists(outFile))
                File.Delete(outFile);

            using (var output = new FileStream(outFile, FileMode.Create))
            using (var input = new ConcatStream(files.Select(file => File.OpenRead(Path.Combine(directory, file)))))
            {
                var decoder = new SevenZip.Compression.LZMA.Decoder();

                byte[] properties = new byte[5];
                if (input.Read(properties, 0, 5) != 5)
                    throw (new Exception("input .lzma is too short"));
                decoder.SetDecoderProperties(properties);

                byte[] sizeBytes = new byte[8];
                if (input.Read(sizeBytes, 0, 8) != 8)
                    throw (new Exception("input .lzma is too short"));

                long outSize = BitConverter.ToInt64(sizeBytes, 0);
                long compressedSize = input.Length - 13;
                decoder.Code(input, output, compressedSize, outSize, null);
            }
        }

        private static void DecompressFileLZMA(string inFile, string outFile)
        {
            if (File.Exists(outFile))
                File.Delete(outFile);

            using (FileStream input = new FileStream(inFile, FileMode.Open))
            using (FileStream output = new FileStream(outFile, FileMode.Create))
            {
                SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                byte[] properties = new byte[5];
                if (input.Read(properties, 0, 5) != 5)
                    throw (new Exception("input .lzma is too short"));
                decoder.SetDecoderProperties(properties);

                byte[] sizeBytes = new byte[8];
                if (input.Read(sizeBytes, 0, 8) != 8)
                    throw (new Exception("input .lzma is too short"));

                long outSize = BitConverter.ToInt64(sizeBytes, 0);
                long compressedSize = input.Length - input.Position;
                decoder.Code(input, output, compressedSize, outSize, null);
            }
        }
    }
}
