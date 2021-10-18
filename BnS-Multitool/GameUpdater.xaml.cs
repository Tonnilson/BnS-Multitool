using System;
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
            ServicePointManager.DefaultConnectionLimit = 30; //Raise the concurrent connection limit for WebClient
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

        private void PatchGameWorker(object sender, DoWorkEventArgs e)
        {
            try
            {
                string FileInfoName = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW) ? "FileInfoMap_TWBNSUE4.dat" : "FileInfoMap_BnS_UE4.dat";
                string PatchInfoName = ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW) ? "PatchFileInfo_TWBNSUE4.dat" : "PatchFileInfo_BnS_UE4.dat";

                string FileInfoURL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, onlineVersion, Path.GetFileNameWithoutExtension(FileInfoName));
                string PatchInfoURL = String.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, onlineVersion, Path.GetFileNameWithoutExtension(PatchInfoName));

                totalBytes = 0L;
                currentBytes = 0L;

                BnSInfoMap = new List<BnSFileInfo>();
                BnSMultiParts = new List<MultipartArchives>();
                var partArchives = new List<MultipartArchives>();
                errorLog = new List<string>();

                bool deltaPatch = ((int.Parse(onlineVersion) - int.Parse(localVersion)) < 2) && onlineVersion != localVersion && SystemConfig.SYS.DELTA_PATCHING == 1;
                string PatchDirectory = Path.Combine(BNS_PATH, "PatchManager", onlineVersion);

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

                List<string> FileInfoMap = File.ReadLines(Path.Combine(PatchDirectory, FileInfoName)).ToList<string>();
                List<string> PatchInfoMap = File.ReadLines(Path.Combine(PatchDirectory, PatchInfoName)).ToList<string>();

                int totalFiles = FileInfoMap.Count();
                int processedFiles = 0;
                int threadCount = SystemConfig.SYS.UPDATER_THREADS + 1;

                Parallel.ForEach<string>(FileInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (string line)
                {
                    string[] lineData = line.Split(new char[] { ':' });
                    string FilePath = lineData[0];
                    string FileSize = lineData[1];
                    string FileHash = lineData[2];
                    string FileFlag = lineData[3];

                    FileInfo fileInfo = new FileInfo(Path.Combine(BNS_PATH, FilePath));

                    if (deltaPatch)
                    {
                        foreach (var file in PatchInfoMap.Where(f => f.Contains(FilePath) && f.DatFilePathMatches(FilePath) && (f.EndsWith(PatchFile_FlagType.ChangedDiff) || f.EndsWith(PatchFile_FlagType.Added))))
                        {
                            string[] lData = file.Split(new char[] { ':' });
                            BnSInfoMap.Add(new BnSFileInfo { path = lData[0], size = lData[1], hash = lData[2], flag = lData[3] });
                            Interlocked.Add(ref totalBytes, long.Parse(lData[1]));
                        }
                    }
                    else
                    {
                        if (fileInfo.Exists && SHA1HASH(fileInfo.FullName) == FileHash) goto FileInfoEnd;

                        foreach (var file in PatchInfoMap.Where(f => f.Contains(FilePath) && f.DatFilePathMatches(FilePath) && (f.EndsWith(PatchFile_FlagType.Added)
                                || f.EndsWith(PatchFile_FlagType.ChangedOriginal)
                                || f.EndsWith(PatchFile_FlagType.UnChanged)))
                            )
                        {
                            string[] lData = file.Split(new char[] { ':' });
                            BnSInfoMap.Add(new BnSFileInfo { path = lData[0], size = lData[1], hash = lData[2], flag = lData[3] });
                            Interlocked.Add(ref totalBytes, long.Parse(lData[1]));
                        }
                    }

                FileInfoEnd:

                    Interlocked.Increment(ref processedFiles);

                    if (!deltaPatch)
                    {
                        FilesProcessed((int)((double)processedFiles / totalFiles * 100));
                        Dispatchers.textBlock(ProgressBlock, String.Format("{0} / {1} Scanned", processedFiles, totalFiles));
                    }
                });

                Dispatchers.textBlock(ProgressBlock, String.Format("Download Size: {0} ({1}) files", SizeSuffix(totalBytes, 2), BnSInfoMap.Count()));
                totalFiles = BnSInfoMap.Count();
                if (totalFiles <= 0) goto Cleanup;

                processedFiles = 0;
                PatchingLabel.Dispatcher.BeginInvoke(new Action(() => { PatchingLabel.Visibility = Visibility.Visible; }));

                FilesProcessed(0);
                Dispatchers.labelContent(PatchingLabel, "Downloading...");
                Thread.Sleep(2000); //Create slack for progress bar to reset

                Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, delegate (BnSFileInfo file)
                {
                    if (file == null)
                        return;

                    if (!Directory.Exists(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.path))))
                        Directory.CreateDirectory(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.path)));
                    try
                    {
                        if (File.Exists(Path.Combine(PatchDirectory, file.path)))
                            File.Delete(Path.Combine(PatchDirectory, file.path));

                        ManualResetEvent resetEvent = new ManualResetEvent(false);
                        resetEvent.Reset();

                        if (DownloadContents(String.Format(@"{0}{1}/Patch/{2}", BASE_URL, onlineVersion, file.path), Path.Combine(PatchDirectory, file.path)))
                        {
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

                                }
                            }
                        }
                        else
                            errorLog.Add(string.Format("{0} failed to download, max retries also failed.", file.path));
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        Debug.WriteLine(ex.ToString());
                    }

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
                        string destination = Path.Combine(BNS_PATH, archive.Directory.Substring(archive.File.EndsWith("dlt") ? 2 : 4));
                        DecompressStreamLZMA(Path.Combine(PatchDirectory, archive.Directory), archive.Archives, archive.File); //Merge the split-archives and run through LZMA decoder

                        if (File.Exists(Path.Combine(destination, archive.File)))
                            File.Delete(Path.Combine(destination, archive.File));

                        if (!Directory.Exists(destination))
                            Directory.CreateDirectory(destination);

                        if (deltaPatch && archive.File.EndsWith("dlt"))
                        {
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
                        errorLog.Add(ex.Message);
                        Debug.WriteLine(ex.Message);
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

                    string destination = Path.GetDirectoryName(Path.Combine(BNS_PATH, file.path.Substring((file.path.Contains(".dlt")) ? 2 : 4)));
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
                        Debug.WriteLine(ex.Message);
                    }
                });

            Cleanup:

                Dispatchers.textBlock(ProgressBlock, "Cleaning up");
                if (File.Exists(Path.Combine(BNS_PATH, FileInfoName)))
                    File.Delete(Path.Combine(BNS_PATH, FileInfoName));

                File.Move(Path.Combine(PatchDirectory, FileInfoName), Path.Combine(BNS_PATH, FileInfoName));
                IniHandler hIni = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
                hIni.Write("VersionInfo", "GlobalVersion", onlineVersion);

                Directory.Delete(PatchDirectory, true);
                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                errorLog.Add(ex.Message);
            }

            errorLog.ForEach(er => WriteError(er));
        }

        private bool DownloadContents(string url, string path, bool retry = true)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            int retries = 0;
            while (true)
            {
                using (var client = new WebClient { Proxy = null })
                {
                    try
                    {
                        client.DownloadFile(new Uri(url), path);
                        return true;
                    }
                    catch (WebException ex)
                    {
                        Debug.WriteLine(url);
                        Debug.WriteLine("{0} Retries", retries);
                        if (!retry || retries >= 6) return false;
                        retries++;
                    }
                }
            }
        }

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
                return string.Empty;
            }
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
                //For whatever stupid reason the export for WritePrivateProfileString is not working for blank ini files
                //So I have to write this manually...
                using (StreamWriter sw = File.CreateText(Path.Combine(BNS_PATH, ((Globals.BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION == Globals.BnS_Region.TW) ? "VersionInfo_TWBNSUE4.ini" : "VersionInfo_BnS_UE4.ini")))
                {
                    sw.WriteLine("[VersionInfo]");
                    sw.WriteLine("GlobalVersion=0");
                    sw.WriteLine("DownloadIndex=0");
                    sw.WriteLine("LanguagePackage=en-US");
                }
                localVersion = "0";
            }
            else
            {
                IniHandler VersionInfo_BnS = new IniHandler(versionFile);
                localVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            }

            onlineVersion = Globals.onlineVersionNumber();
            //onlineVersion = "5";

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
                errorLog.Add(string.Format("{0} Failed to delta - {1}", Path.GetFileName(patch), ex.Message));
                return false;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
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

        private static void DecompressStreamLZMA(string directory, List<string> files, string outFile)
        {
            string fullOutFile = Path.Combine(directory, outFile);
            if (File.Exists(fullOutFile))
                File.Delete(fullOutFile);

            try
            {
                using (var output = new FileStream(fullOutFile, FileMode.Create))
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
            catch (Exception ex)
            {
                errorLog.Add(string.Format("Failed to create {0}, Data Error due to missing parts", outFile));
            }
            files.ForEach(f => File.Delete(Path.Combine(directory, f)));
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

            File.Delete(inFile);
        }
    }
}