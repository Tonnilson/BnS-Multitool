using BnS_Multitool.Extensions;
using MiscUtil.Compression.Vcdiff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static BnS_Multitool.Functions.Crypto;
using static BnS_Multitool.Functions.FileExtraction;

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
        private string BASE_URL;
        private string BNS_PATH = SystemConfig.SYS.BNS_DIR;
        private long currentBytes = 0L;
        private long totalBytes = 0L;
        private List<BnSFileInfo> BnSInfoMap;
        private List<MultipartArchives> BnSMultiParts;
        private static List<string> errorLog;

        public class BnSFileInfo
        {
            public string path { get; set; }
            public string size { get; set; }
            public string hash { get; set; }
            public string flag { get; set; }
            public bool Downloaded { get; set; }
        }

        public struct PatchFile_FlagType
        {
            public const string Unknown = "0";
            public const string UnChanged = "1";
            public const string Changed = "2";
            public const string ChangedDiff = "3";
            public const string ChangedOriginal = "4";
            public const string Added = "5";
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

            patchWorker.DoWork += new DoWorkEventHandler(PatchGameWorker);
            patchWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PatchGameFinished);
            ServicePointManager.DefaultConnectionLimit = 50; //Raise the concurrent connection limit for WebClient
        }

        private void WriteError(string msg) => this.ErrorLog.Dispatcher.BeginInvoke(new Action(() => { ErrorLog.AppendText(msg + "\r"); ErrorLog.ScrollToEnd(); }));

        private void PatchGameFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            if (errorLog.Count > 0)
                WriteError("Running a file check can possibly resolve issues above, try that before pointing it out.");
            else
                WriteError("Welp.. didn't run into issues that we were tracking so assume your game is fine..?");

            FilesProcessed(0);
            DownloadBtn.IsEnabled = true;
            DownloadBtn.Content = "File Check";
            localVersion = onlineVersion;
            ProgressGrid.Visibility = Visibility.Hidden;
            localVersionLabel.Content = onlineVersion.ToString();
            LocalGameLbl.Foreground = Brushes.Green;
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(long value, int decimalPlaces = 1, bool showSuffix = true)
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

        private static bool HasFlags(string input, List<string> flags)
        {
            foreach (var flag in flags)
                if (input.EndsWith(flag))
                    return true;
            return false;
        }

        private void PatchGameWorker(object sender, DoWorkEventArgs e)
        {
            try
            {
                BNS_PATH = Path.GetFullPath(SystemConfig.SYS.BNS_DIR);
                string FileInfoName = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW) ? "FileInfoMap_TWBNSUE4.dat" : "FileInfoMap_BnS_UE4.dat";
                string PatchInfoName = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW) ? "PatchFileInfo_TWBNSUE4.dat" : "PatchFileInfo_BnS_UE4.dat";
                string targetVersion = "0";
            StartPatchThread:
                if (localVersion != "0")
                {
                    if (int.Parse(onlineVersion) - int.Parse(localVersion) > 1)
                        targetVersion = (int.Parse(localVersion) + 1).ToString();
                    else
                        targetVersion = onlineVersion;
                }
                else
                    targetVersion = onlineVersion;

                string FileInfoURL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, targetVersion, Path.GetFileNameWithoutExtension(FileInfoName));
                string PatchInfoURL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, targetVersion, Path.GetFileNameWithoutExtension(PatchInfoName));

                totalBytes = 0L;
                currentBytes = 0L;

                BnSInfoMap = new List<BnSFileInfo>();
                BnSMultiParts = new List<MultipartArchives>();
                var partArchives = new List<MultipartArchives>();
                errorLog = new List<string>();

                bool deltaPatch = localVersion != "0" && int.Parse(targetVersion) > int.Parse(localVersion) && SystemConfig.SYS.DELTA_PATCHING == 1;
                string PatchDirectory = Path.Combine(BNS_PATH, "PatchManager", targetVersion);

                DltPLbl.Dispatcher.BeginInvoke(new Action(() => { DltPLbl.Visibility = (deltaPatch) ? Visibility.Visible : Visibility.Hidden; }));

                if (!RemoteFileExists(PatchInfoURL))
                    throw new Exception(String.Format("PatchFileInfo for build #{0} could not be reached", onlineVersion));

                if (!Directory.Exists(PatchDirectory))
                    Directory.CreateDirectory(PatchDirectory);

                Dispatchers.textBlock(ProgressBlock, "Retrieving " + PatchInfoName);
                if (!DownloadContents(PatchInfoURL, Path.Combine(PatchDirectory, PatchInfoName + ".zip"), false))
                    throw new Exception("Failed to download " + PatchInfoName);

                Dispatchers.textBlock(ProgressBlock, "Retrieving " + FileInfoName);
                if (!DownloadContents(FileInfoURL, Path.Combine(PatchDirectory, FileInfoName + ".zip"), false))
                    throw new Exception("Failed to download " + FileInfoName);

                Dispatchers.textBlock(ProgressBlock, "Decompressing File Maps");
                DecompressFileLZMA(Path.Combine(PatchDirectory, FileInfoName + ".zip"), Path.Combine(PatchDirectory, FileInfoName));
                DecompressFileLZMA(Path.Combine(PatchDirectory, PatchInfoName + ".zip"), Path.Combine(PatchDirectory, PatchInfoName));

                // Fix for new installations and possibly another unknown bug?
                if(!File.Exists(Path.Combine(BNS_PATH, FileInfoName)))
                    File.Copy(Path.Combine(PatchDirectory, FileInfoName), Path.Combine(BNS_PATH, FileInfoName));

                List<string> OnlineFileInfoMap = File.ReadLines(Path.Combine(PatchDirectory, FileInfoName)).ToList<string>();
                List<string> PatchInfoMap = File.ReadLines(Path.Combine(PatchDirectory, PatchInfoName)).ToList<string>();
                List<string> CurrentFileInfoMap = File.ReadLines(Path.Combine(BNS_PATH, FileInfoName)).ToList<string>();

                int totalFiles = OnlineFileInfoMap.Count();
                int processedFiles = 0;
                int threadCount = SystemConfig.SYS.UPDATER_THREADS + 1;

                Parallel.ForEach<string>(OnlineFileInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (string line)
                {
                    string[] lineData = line.Split(new char[] { ':' });
                    string FilePath = lineData[0];
                    string FileSize = lineData[1];
                    string FileHash = lineData[2];
                    string FileFlag = lineData[3];

                    FileInfo fileInfo = new FileInfo(Path.Combine(BNS_PATH, FilePath));
                    string fHash = fileInfo.Exists ? SHA1_File(fileInfo.FullName) : "";
                    if (deltaPatch)
                    {
                        if (fileInfo.Exists && fHash == FileHash) goto FileInfoEnd;
                        // Make sure the hash matches current fileInfoMap hash, if not trigger full download
                        var oldF = CurrentFileInfoMap.FirstOrDefault(x => x.Split(new char[] { ':' })[0] == FilePath);

                        if (fileInfo.Exists && oldF != null && fHash != oldF.Split(new char[] { ':' })[2])
                        {
                            foreach (var file in PatchInfoMap.Where(f => f.Contains(FilePath) && f.DatFilePathMatches(FilePath) && (f.EndsWith(PatchFile_FlagType.Added)
                                || f.EndsWith(PatchFile_FlagType.ChangedOriginal)
                                || f.EndsWith(PatchFile_FlagType.UnChanged)))
                            )
                            {
                                string[] lData = file.Split(new char[] { ':' });
                                BnSInfoMap.Add(new BnSFileInfo { path = lData[0], size = lData[1], hash = lData[2], flag = lData[3], Downloaded = false });
                                Interlocked.Add(ref totalBytes, long.Parse(lData[1]));
                            }
                        }
                        else
                        {
                            List<string> flags;
                            if (fileInfo.Exists)
                                flags = new List<string> { PatchFile_FlagType.ChangedDiff, PatchFile_FlagType.Added };
                            else
                                flags = new List<string> { PatchFile_FlagType.Added, PatchFile_FlagType.ChangedOriginal, PatchFile_FlagType.UnChanged };

                            foreach (var file in PatchInfoMap.Where(f => f.Contains(FilePath) && f.DatFilePathMatches(FilePath) &&
                            HasFlags(f, flags)))
                            {
                                string[] lData = file.Split(new char[] { ':' });
                                BnSInfoMap.Add(new BnSFileInfo { path = lData[0], size = lData[1], hash = lData[2], flag = lData[3], Downloaded = false });
                                Interlocked.Add(ref totalBytes, long.Parse(lData[1]));
                            }
                        }
                    }
                    else
                    {
                        if (fileInfo.Exists && fHash == FileHash) goto FileInfoEnd;

                        foreach (var file in PatchInfoMap.Where(f => f.Contains(FilePath) && f.DatFilePathMatches(FilePath) && (f.EndsWith(PatchFile_FlagType.Added)
                                || f.EndsWith(PatchFile_FlagType.ChangedOriginal)
                                || f.EndsWith(PatchFile_FlagType.UnChanged)))
                            )
                        {
                            string[] lData = file.Split(new char[] { ':' });
                            BnSInfoMap.Add(new BnSFileInfo { path = lData[0], size = lData[1], hash = lData[2], flag = lData[3], Downloaded = false });
                            Interlocked.Add(ref totalBytes, long.Parse(lData[1]));
                        }
                    }

                FileInfoEnd:
                    Interlocked.Increment(ref processedFiles);
                    FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                    Dispatchers.textBlock(ProgressBlock, String.Format("{0} / {1} files scanned", processedFiles, totalFiles));
                });

                Dispatchers.textBlock(ProgressBlock, String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));
                totalFiles = BnSInfoMap.Count();
                if (totalFiles <= 0) goto Cleanup;

                processedFiles = 0;
                PatchingLabel.Dispatcher.BeginInvoke(new Action(() => { PatchingLabel.Visibility = Visibility.Visible; }));

                FilesProcessed(0);
                Dispatchers.labelContent(PatchingLabel, "Downloading...");
                Thread.Sleep(2000); //Create slack for progress bar to reset

                // Adding an extra thread on download just cause the download server limits speeds on each file so we'll get more bang for our buck by increasing download tasks
                Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount + 1 }, delegate (BnSFileInfo file)
                {
                    if (file == null)
                        return;

                    if (!Directory.Exists(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.path))))
                        Directory.CreateDirectory(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.path)));
                    try
                    {
                        // Check if the file exists
                        if(File.Exists(Path.Combine(PatchDirectory, file.path))) 
                        {
                            if (SHA1_File(Path.Combine(PatchDirectory, file.path)) == file.hash) goto FileDownloadComplete;
                            File.Delete(Path.Combine(PatchDirectory, file.path));
                        }
                        StartDownload:

                        // Downloads the file and validates it matches.
                        if (!DownloadContents(String.Format(@"{0}{1}/Patch/{2}", BASE_URL, targetVersion, file.path.Replace('\\', '/')), Path.Combine(PatchDirectory, file.path)))
                        {
                            errorLog.Add(string.Format("{0} failed to download, max retries also failed.", file.path));
                            goto EndOfThread;
                        } else
                        {
                            // I hate doing this because calculating a SHA1 hash is very expensive
                            if (file.hash != SHA1_File(Path.Combine(PatchDirectory, file.path)))
                                goto StartDownload;
                        }

                    FileDownloadComplete:
                        file.Downloaded = true;
                        Interlocked.Increment(ref processedFiles);
                        Interlocked.Add(ref currentBytes, long.Parse(file.size));

                        if (!file.path.EndsWith("zip"))
                        {
                            string FileName = Path.GetFileNameWithoutExtension(file.path);
                            int curIndex = partArchives.FindIndex(x => x.File == FileName);
                            try
                            {
                                if (curIndex == -1)
                                    partArchives.Add(new MultipartArchives() { File = FileName, Directory = Path.GetDirectoryName(file.path), Archives = new List<string>() { Path.GetFileName(file.path) } });
                                else
                                    partArchives[curIndex].Archives.Add(Path.GetFileName(file.path));

                            }
                            catch (Exception)
                            {
                                // Logging this is pointless, Parallel code will often trigger this and I don't know of any work around
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        Logger.log.Error("GameUpdater: {0}", ex.Message);
                    }

                    EndOfThread:
                    Dispatchers.labelContent(PatchingLabel, String.Format("{0} / {1}", SizeSuffix(currentBytes, 2), SizeSuffix(totalBytes, 2)));
                    FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                });

                Thread.Sleep(2000);
                if (partArchives.Count <= 0) goto PatchNormalFiles;

                FilesProcessed(0);
                Dispatchers.labelContent(PatchingLabel, "Patching Multi");
                Thread.Sleep(2000); //Create some slack for our progress bar to reset fully (visual).
                var totalFilesM = partArchives.Count();
                processedFiles = 0;

                /*
                    Handles multi-parted archives, KR has been splitting files into multiple archives greater than 20MB
                    but it was never a thing in NA/EU. This process is quite tasking and will take a large majority of time.
                    We'll read all the files and concat the file streams together and then run it through the LZMA decoder
                    to get our full file that is uncompressed.
                 */
                Parallel.ForEach<MultipartArchives>(partArchives, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (MultipartArchives archive)
                {
                    archive.Archives.Sort(); //We need to sort the list so that each file is loaded in the proper order
                    try
                    {
                        // Start from the index of \ + 1 to get the path to the file we'll be patching.
                        // Path can either start with patchnumber\ or Zip\ and we need to read the path after that
                        string destination = Path.GetFullPath(Path.Combine(SystemConfig.SYS.BNS_DIR, archive.Directory.Substring(archive.Directory.IndexOf("\\") + 1)));
                        var result = DecompressStreamLZMA(Path.Combine(PatchDirectory, archive.Directory), archive.Archives, archive.File); //Merge the split-archives and run through LZMA decoder

                        if(!result.IsNullOrEmpty())
                            throw new IOException(result);

                        if (File.Exists(Path.Combine(destination, archive.File)))
                            File.Delete(Path.Combine(destination, archive.File));

                        if (!Directory.Exists(destination))
                            Directory.CreateDirectory(destination);

                        if (deltaPatch && archive.File.EndsWith("dlt"))
                        {
                            //Logger.log.Info("Multi File");
                            if (DeltaPatch(Path.Combine(destination, Path.GetFileNameWithoutExtension(archive.File)), Path.Combine(PatchDirectory, archive.Directory, archive.File)))
                            {
                                File.Delete(Path.Combine(PatchDirectory, archive.Directory, archive.File));

                                if (File.Exists(Path.Combine(destination, Path.GetFileNameWithoutExtension(archive.File))))
                                    File.Delete(Path.Combine(destination, Path.GetFileNameWithoutExtension(archive.File)));

                                File.Move(Path.Combine(PatchDirectory, archive.Directory, Path.GetFileNameWithoutExtension(archive.File)), Path.Combine(destination, Path.GetFileNameWithoutExtension(archive.File)));
                            }
                            else
                                throw new Exception(string.Format("{0} failed to delta patch", archive.File));
                        }
                        else
                            File.Move(Path.Combine(PatchDirectory, archive.Directory, archive.File), Path.Combine(destination, archive.File));
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error("{0}\n{1}", ex.Message, ex.StackTrace);
                        errorLog.Add(ex.Message);
                    }
                    finally
                    {
                        Interlocked.Increment(ref processedFiles);
                        FilesProcessed((int)((double)processedFiles / totalFilesM * 100));
                    }
                });

            PatchNormalFiles:
                Thread.Sleep(2000);
                FilesProcessed(0);
                processedFiles = 0;
                Dispatchers.labelContent(PatchingLabel, "Patching");
                Thread.Sleep(1000);

                /*
                    Old style patching process, decompress the archive with LZMA decoder
                    if file is a dlt (Delta) file then patch the current file then move it
                    to where it needs to be and cleanup.
                */
                Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (BnSFileInfo file)
                {
                    if (file == null)
                        return;

                    if (!file.path.EndsWith("zip"))
                    {
                        Interlocked.Increment(ref processedFiles);
                        FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                        Dispatchers.textBlock(ProgressBlock, String.Format("{0} ({1}%)", SizeSuffix(totalBytes, 2), (int)((double)processedFiles / totalFiles * 100)));
                        return;
                    }

                    // Start from the index of \ + 1 to get the path to the file we'll be patching.
                    string destination = Path.GetFullPath(Path.GetDirectoryName(Path.Combine(BNS_PATH, file.path.Substring(file.path.IndexOf("\\") + 1))));
                    string directory = Path.GetDirectoryName(file.path);
                    string fileName = Path.GetFileNameWithoutExtension(file.path);

                    if (!Directory.Exists(destination))
                        Directory.CreateDirectory(destination);

                    try
                    {
                        DecompressFileLZMA(Path.Combine(PatchDirectory, directory, Path.GetFileName(file.path)), Path.Combine(PatchDirectory, directory, fileName));

                        //Delta Patch
                        if (deltaPatch && file.flag == PatchFile_FlagType.ChangedDiff && Path.GetFileName(file.path).Contains(".dlt"))
                        {
                            if (DeltaPatch(Path.Combine(destination, Path.GetFileNameWithoutExtension(fileName)), Path.Combine(PatchDirectory, directory, fileName)))
                            {
                                File.Delete(Path.Combine(PatchDirectory, directory, fileName));

                                if (File.Exists(Path.Combine(destination, Path.GetFileNameWithoutExtension(fileName))))
                                    File.Delete(Path.Combine(destination, Path.GetFileNameWithoutExtension(fileName)));

                                File.Move(Path.Combine(PatchDirectory, directory, Path.GetFileNameWithoutExtension(fileName)), Path.Combine(destination, Path.GetFileNameWithoutExtension(fileName)));
                            }
                        }
                        else
                        {
                            if (File.Exists(Path.Combine(destination, fileName)))
                                File.Delete(Path.Combine(destination, fileName));

                            File.Move(Path.Combine(PatchDirectory, directory, fileName), Path.Combine(destination, fileName));
                        }

                        Interlocked.Increment(ref processedFiles);
                        FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                        Dispatchers.textBlock(ProgressBlock, String.Format("{0} ({1}%)", SizeSuffix(totalBytes, 2), (int)((double)processedFiles / totalFiles * 100)));
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        Logger.log.Error("{0}\n{1}", ex.Message, ex.StackTrace);
                    }
                });

            Cleanup:
                Dispatchers.textBlock(ProgressBlock, "Internal Check");
                if (totalFiles > 0 && BnSInfoMap.Any(x => !x.Downloaded))
                    errorLog.Add("Download checks failed");

                Thread.Sleep(500);
                Dispatchers.textBlock(ProgressBlock, "Cleaning up");

                // Only delete working patch directory and change versionNumber over if successful.
                if (errorLog.Count == 0)
                {
                    if (File.Exists(Path.Combine(BNS_PATH, FileInfoName)))
                        File.Delete(Path.Combine(BNS_PATH, FileInfoName));

                    Dispatchers.labelContent(localVersionLabel, targetVersion);

                    File.Move(Path.Combine(PatchDirectory, FileInfoName), Path.Combine(BNS_PATH, FileInfoName));
                    IniHandler hIni = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
                    hIni.Write("VersionInfo", "GlobalVersion", targetVersion);
                    localVersion = targetVersion;
                    Directory.Delete(PatchDirectory, true);
                    if (targetVersion != onlineVersion)
                        goto StartPatchThread; // Loop back around to do yet another delta patch
                }
                else
                    goto StartPatchThread;

                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                errorLog.Add(ex.Message);
            }

            errorLog.ForEach(er => WriteError(er));

            // Force .NET garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private long RemoteFileSize(string url)
        {
            var req = WebRequest.Create(new Uri(url));
            req.Method = "GET";

            try
            {
                using (var response = req.GetResponse())
                    return response.ContentLength;
            }
            catch (Exception ex)
            {
                Logger.log.Error("Error Request: {0}\n{1}\n{2}", url, ex.Message, ex.StackTrace);
                return 0L;
            }
        }

        private bool DownloadContents(string url, string path, bool retry = true)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            long contentLength = 0L;
            int retries = 0;
        DownloadStart:
            if (retries >= 6) goto FailedCount;
            retries++;
            contentLength = RemoteFileSize(url);
            if (contentLength == 0)
            {
                Logger.log.Error("Could not retrieve content length for {0}", url);
                goto DownloadStart;
            }

            using (var client = new WebClient { Proxy = null })
            {
                try
                {
                    client.DownloadFile(new Uri(url), path);
                    return true;
                }
                catch (WebException ex)
                {
                    Logger.log.Error("Connection Errno: {0}\nResponse Code: {1}", ex.Status, ((HttpWebResponse)ex.Response).StatusCode);
                    Thread.Sleep(500);
                    goto DownloadStart;
                }
            }

        FailedCount:
            errorLog.Add(string.Format("Failed to download {0}, attempted tries: {1}", Path.GetFileName(path), retries));
            return false;
        }

        private void FilesProcessed(int value)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(1));

            currentProgress.Dispatcher.BeginInvoke(new Action(() => {
                System.Windows.Media.Animation.DoubleAnimation da = new System.Windows.Media.Animation.DoubleAnimation(value, duration);
                currentProgress.BeginAnimation(ProgressBar.ValueProperty, da);
            }));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW)
                BASE_URL = @"http://mmorepo.cdn.plaync.com.tw/TWBNSUE4/";
            else
                BASE_URL = @"http://d37ob46rk09il3.cloudfront.net/BnS_UE4/";

            string versionFile = Directory.GetFiles(BNS_PATH, "VersionInfo_*.ini").FirstOrDefault();
            if (string.IsNullOrEmpty(versionFile))
            {
                // For whatever stupid reason the export for WritePrivateProfileString is not working for blank ini files
                // So I have to write this manually...
                using (StreamWriter sw = File.CreateText(Path.Combine(BNS_PATH, string.Format("VersionInfo_{0}.ini", Globals.BnS_ServerInfo.Name))))
                {
                    sw.WriteLine("[VersionInfo]");
                    sw.WriteLine("GlobalVersion=0");
                    sw.WriteLine("DownloadIndex=0");
                }
                localVersion = "0";
            }
            else
            {
                IniHandler VersionInfo_BnS = new IniHandler(versionFile);
                localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            }

            onlineVersion = Globals.onlineVersionNumber();
            // Uncomment this if forcing a specific patch, note that patches not <> than 2 will force delta so turn off delta patching if downgrading a patch
            //onlineVersion = "9";

            localVersionLabel.Content = localVersion.ToString();
            currentVersionLabel.Content = string.Format("{0}", (onlineVersion == "") ? "Error" : onlineVersion);

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

            if (localVersion == "0")
                DownloadBtn.Content = "Install";
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
            catch (Exception ex)
            {
                Logger.log.Error("{0} Failed to delta - {1}", Path.GetFileName(patch), ex.Message);
                errorLog.Add(string.Format("{0} Failed to delta - {1}", Path.GetFileName(patch), ex.Message));
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
            ErrorLog.Document.Blocks.Clear();
            DownloadBtn.IsEnabled = false;
            ProgressGrid.Visibility = Visibility.Visible;
            PatchingLabel.Visibility = Visibility.Hidden;
            patchWorker.RunWorkerAsync();
        }
    }
}