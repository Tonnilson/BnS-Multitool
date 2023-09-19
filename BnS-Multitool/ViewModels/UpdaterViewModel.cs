using BnS_Multitool.Extensions;
using BnS_Multitool.Functions;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static BnS_Multitool.Functions.Crypto;
using static BnS_Multitool.Functions.IO;

namespace BnS_Multitool.ViewModels
{
    public partial class UpdaterViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly httpClient _httpClient;
        private readonly BnS _bns;
        private readonly ILogger<UpdaterViewModel> _logger;
        private string BASE_URL = string.Empty;
        private string BNS_PATH = string.Empty;
        private bool UpdaterEnabled = false;
        private BackgroundWorker _purpleWorker;
        private BackgroundWorker _nclauncherWorker;
        private string localVersion = string.Empty;
        private string onlineVersion = string.Empty;
        private long currentBytes = 0L;
        private long totalBytes = 0L;
        private BackgroundWorker _networkWorker = null;
        private bool _isPurple = false;

        public UpdaterViewModel(Settings settings, httpClient httpClient, ILogger<UpdaterViewModel> logger, BnS bns)
        {
            _settings = settings;
            _httpClient = httpClient;
            _logger = logger;
            _bns = bns;

            _purpleWorker = new BackgroundWorker();
            _purpleWorker.DoWork += new DoWorkEventHandler(PurpleGamePatcher);
            _purpleWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GameUpdaterFinished);

            _nclauncherWorker = new BackgroundWorker();
            _nclauncherWorker.DoWork += new DoWorkEventHandler(NCLauncherGamePatcher);
            _nclauncherWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GameUpdaterFinished);

            _networkWorker = new BackgroundWorker();
            _networkWorker.WorkerSupportsCancellation = true;
            _networkWorker.DoWork += new DoWorkEventHandler(NetworkMonitor);
        }

        private void NetworkMonitor(object? sender, DoWorkEventArgs e)
        {
            DateTime starttime = DateTime.Now;

            while (!(sender as BackgroundWorker).CancellationPending)
            {
                var timeDifferenceInSeconds = (DateTime.Now - starttime).TotalSeconds;
                var bytes = currentBytes;
                var speed = Convert.ToInt64(bytes / timeDifferenceInSeconds);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBlock = string.Format("Download Speed: {0}/s ", SizeSuffix(speed, 2));
                }));
                Thread.Sleep(1000);
            }
            e.Cancel = true;
        }

        [ObservableProperty]
        private string onlineBuildText = "Retrieving...";

        [ObservableProperty]
        private Brush onlineBuildColor = Brushes.White;

        [ObservableProperty]
        private string localBuildText = "Retrieving...";

        [ObservableProperty]
        private Brush localBuildColor = Brushes.White;

        [ObservableProperty]
        private string actionBtnText = "Update";

        [ObservableProperty]
        private string progressBlock;

        [ObservableProperty]
        private string patchingLabel;

        [ObservableProperty]
        private string thrownErrors;

        [ObservableProperty]
        private bool showProgressView = false;

        [ObservableProperty]
        private bool showDeltaPatch = false;

        [ObservableProperty]
        private double progressValue = 0.00;

        [ObservableProperty]
        private bool actionBtnEnabled = true;

        [RelayCommand]
        async Task UILoaded()
        {
            if (UpdaterEnabled) return;
            await Task.Run(PageLoaded);
        }

        private async Task PageLoaded()
        {
            BASE_URL = _bns.RepositoryServerAddress;

            // bool bIsPurple = _httpClient.RemoteFileExists($"http://{BASE_URL}/{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}/{_bns.BuildNumber}/Patch/files_info.json.zip");
            string localBuild = _bns.GetLocalBuild();
            LocalBuildText = localBuild.IsNullOrEmpty() ? "0" : localBuild;
            OnlineBuildText = _bns.BuildNumber;

            if (onlineBuildText.IsNullOrEmpty())
            {
                OnlineBuildColor = Brushes.Red;
                OnlineBuildText = "Error";
            }
            else
                OnlineBuildColor = Brushes.Green;

            if (OnlineBuildText != localBuild)
            {
                LocalBuildColor = Brushes.Red;
                ActionBtnText = "Update";
            }
            else
            {
                LocalBuildColor = Brushes.Green;
                ActionBtnText = "File Check";
            }

            if (LocalBuildText.IsNullOrEmpty() || LocalBuildText == "0")
                ActionBtnText = "Install";

            localVersion = LocalBuildText;
            onlineVersion = OnlineBuildText;
            await Task.CompletedTask;
        }

        private List<string> errorLog = new List<string>();

        private void GameUpdaterFinished(object sender, EventArgs e)
        {
            if (errorLog.Count > 0)
            {
                errorLog.Add("Running a file check can possibly resolve issues above, try that before reporting an issue.");
                ThrownErrors = string.Empty;
                string sb = string.Empty;
                errorLog.ForEach(error =>
                {
                    sb += $"{error}\r";
                });

                ThrownErrors = sb.ToString();
            }
            else
            {
                LocalBuildText = OnlineBuildText;
                LocalBuildColor = Brushes.Green;
                ThrownErrors = "No issues during the process";
            }

            UpdaterEnabled = false;
            ProgressValue = 0.00;
            ShowDeltaPatch = false;
            ShowProgressView = false;
            ActionBtnText = "File Check";
            ActionBtnEnabled = true;
        }

        [RelayCommand]
        void StartDownload()
        {
            if (UpdaterEnabled) return;
            ActionBtnEnabled = false;
            ThrownErrors = string.Empty;
            ProgressBlock = string.Empty;
            PatchingLabel = string.Empty;

            _isPurple = _httpClient.RemoteFileExists($"http://{BASE_URL}/{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}/{onlineVersion}/Patch/files_info.json.zip");
            UpdaterEnabled = true;
            ShowProgressView = true;
            if (_isPurple)
                _purpleWorker.RunWorkerAsync();
            else
                _nclauncherWorker.RunWorkerAsync();
        }

        public class UPDATE_FILE_ENTRY
        {
            public required BnS.PURPLE_FILES_STRUCT fileInfo { get; set; }
            public bool Downloaded { get; set; }
        }

        private void PurpleGamePatcher(object sender, DoWorkEventArgs e)
        {
            try
            {
                BNS_PATH = Path.GetFullPath(_settings.System.BNS_DIR);

                string fileInfo_name = "files_info.json";
                string targetVersion = "0";

            StartPatchThread:
                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressValue = 0.00; }));
                if (localVersion != "0")
                {
                    if (int.Parse(onlineVersion) - int.Parse(localVersion) > 1)
                        targetVersion = (int.Parse(localVersion) + 1).ToString();
                    else
                        targetVersion = onlineVersion;
                }
                else
                    targetVersion = onlineVersion;

                string FileInfo_URL = string.Format(@"http://{0}/{1}/{2}/Patch/{3}.zip", BASE_URL, _settings.Account.REGION.GetAttribute<GameIdAttribute>().Name, targetVersion, fileInfo_name);

                totalBytes = 0L;
                currentBytes = 0L;
                errorLog = new List<string>();

                List<UPDATE_FILE_ENTRY> update_file_map = new List<UPDATE_FILE_ENTRY>();

                int i_targetVersion = int.Parse(targetVersion);
                int i_localVersion = int.Parse(localVersion);
                bool deltaPatch = localVersion != "0" && (i_targetVersion - i_localVersion) == 1;
                string PatchDirectory = Path.Combine(BNS_PATH, "PatchManager", targetVersion);

                if (!_httpClient.RemoteFileExists(FileInfo_URL))
                    throw new Exception(String.Format("files_info.json for build #{0} could not be reached", targetVersion));

                if (!Directory.Exists(PatchDirectory))
                    Directory.CreateDirectory(PatchDirectory);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = $"Retreiving {fileInfo_name}"; }));
                if (!_httpClient.DownloadFile(FileInfo_URL, Path.Combine(PatchDirectory, fileInfo_name + ".zip"), false))
                    throw new Exception("Failed to download " + fileInfo_name);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = $"Decompressing {fileInfo_name}"; }));
                IO.DecompressFileLZMA(Path.Combine(PatchDirectory, fileInfo_name + ".zip"), Path.Combine(PatchDirectory, fileInfo_name));

                BnS.PURPLE_FILE_INFO file_info = JsonConvert.DeserializeObject<BnS.PURPLE_FILE_INFO>(File.ReadAllText(Path.Combine(PatchDirectory, fileInfo_name)));
                if (file_info == null)
                    throw new Exception("Failed to parse files_info.json");

                int totalFiles = file_info.files.Count;
                int processedFiles = 0;

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { PatchingLabel = "Scanning"; }));

                Parallel.ForEach<BnS.PURPLE_FILES_STRUCT>(file_info.files, new ParallelOptions { MaxDegreeOfParallelism = _settings.System.UPDATER_THREADS + 1 }, delegate (BnS.PURPLE_FILES_STRUCT fileData)
                {
                    if (fileData.patchType == PatchFile_FlagType.Unknown) return;

                    FileInfo fileInfo = new FileInfo(Path.Combine(BNS_PATH, fileData.path));

                    if (deltaPatch)
                    {
                        if (fileData.patchType == PatchFile_FlagType.Added || fileData.patchType == PatchFile_FlagType.ChangedOriginal)
                        {
                            if (fileData.patchType == PatchFile_FlagType.Added)
                                Interlocked.Add(ref totalBytes, long.Parse(fileData.encodedInfo.size));
                            else
                                Interlocked.Add(ref totalBytes, long.Parse(fileData.deltaInfo.size));

                            update_file_map.Add(new UPDATE_FILE_ENTRY { fileInfo = fileData, Downloaded = false });
                        }
                    }
                    else
                    {
                        string fHash = fileInfo.Exists ? Crypto.SHA1_File(fileInfo.FullName) : "";
                        if (deltaPatch)
                        {
                            if (fileInfo.Exists && fHash == fileData.hash) goto FileInfoEnd;
                            if (!fileInfo.Exists)
                                Interlocked.Add(ref totalBytes, long.Parse(fileData.encodedInfo.size));
                            else
                            {
                                if (fileData.patchType != PatchFile_FlagType.ChangedOriginal)
                                    Interlocked.Add(ref totalBytes, long.Parse(fileData.encodedInfo.size));
                                else
                                    Interlocked.Add(ref totalBytes, long.Parse(fileData.deltaInfo.size));
                            }

                            update_file_map.Add(new UPDATE_FILE_ENTRY { fileInfo = fileData, Downloaded = false });
                        }
                        else
                        {
                            if (fileInfo.Exists && fHash == fileData.hash) goto FileInfoEnd;

                            Interlocked.Add(ref totalBytes, long.Parse(fileData.encodedInfo.size));
                            update_file_map.Add(new UPDATE_FILE_ENTRY { fileInfo = fileData, Downloaded = false });
                        }
                    }

                FileInfoEnd:
                    Interlocked.Increment(ref processedFiles);
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                        ProgressBlock = $"{processedFiles} / {totalFiles} files scanned";
                    }));
                });

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBlock = $"Download Size: {IO.SizeSuffix(totalBytes, 2)} ({update_file_map.Count}) files";
                }));

                totalFiles = update_file_map.Count();
                if (totalFiles <= 0) goto Cleanup;
                //update_file_map.ForEach(x => Debug.WriteLine(x.fileInfo.path));

                processedFiles = 0;
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressValue = 0.00;
                    PatchingLabel = "Downloading...";
                }));

                Thread.Sleep(1000);

                // Network counter for this process
                _networkWorker.RunWorkerAsync();

                Parallel.ForEach<UPDATE_FILE_ENTRY>(update_file_map, new ParallelOptions { MaxDegreeOfParallelism = _settings.System.DOWNLOADER_THREADS + 1 }, delegate (UPDATE_FILE_ENTRY file)
                {
                    if (file == null)
                        return;

                    if (!Directory.Exists(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path))))
                        Directory.CreateDirectory(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path)));

                    try
                    {
                        // I may remove this in the future, this is just... not really needed since I handle the logic below but I got lost in the sauce when rewriting this
                        if (file.fileInfo.patchType == PatchFile_FlagType.Added)
                        {
                            if (file.fileInfo.encodedInfo.separates == null)
                            {
                                if (!_httpClient.DownloadFile(
                                    string.Format(@"http://{0}/{1}", BASE_URL, file.fileInfo.encodedInfo.path),
                                    Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.encodedInfo.path)),
                                    true, file.fileInfo.encodedInfo.hash))
                                {
                                    errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                    goto EndOfThread;
                                }
                            }
                            else
                            {
                                foreach (var f in file.fileInfo.encodedInfo.separates)
                                {
                                    if (!_httpClient.DownloadFile(string.Format(@"http://{0}/{1}", BASE_URL, f.path), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(f.path)), true, f.hash))
                                    {
                                        errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                        goto EndOfThread;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Check if we can delta patch, If we can delta patch then it may be a failed hash check.
                            if (!deltaPatch || (File.Exists(Path.Combine(BNS_PATH, file.fileInfo.path)) && file.fileInfo.patchType != PatchFile_FlagType.ChangedOriginal))
                            {
                                if (file.fileInfo.encodedInfo.separates == null)
                                {
                                    if (!_httpClient.DownloadFile(
                                    string.Format(@"http://{0}/{1}", BASE_URL, file.fileInfo.encodedInfo.path),
                                    Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.encodedInfo.path)), true, file.fileInfo.encodedInfo.hash))
                                    {
                                        errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                        goto EndOfThread;
                                    }
                                }
                                else
                                {
                                    foreach (var f in file.fileInfo.encodedInfo.separates)
                                    {
                                        if (!_httpClient.DownloadFile(string.Format(@"http://{0}/{1}", BASE_URL, f.path), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(f.path)), true, f.hash))
                                        {
                                            errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                            goto EndOfThread;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Delta patching part
                                BnS.PURPLE_ENCODED_INFO? files;
                                if (deltaPatch)
                                    files = file.fileInfo.deltaInfo ?? file.fileInfo.encodedInfo;
                                else
                                    files = file.fileInfo.encodedInfo;

                                if (files.separates == null)
                                {
                                    if (!_httpClient.DownloadFile(
                                        string.Format(@"http://{0}/{1}", BASE_URL, files.path),
                                        Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(files.path)), true, files.hash))
                                    {
                                        errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                        goto EndOfThread;
                                    }
                                }
                                else
                                {
                                    foreach (var f in files.separates)
                                    {
                                        if (!_httpClient.DownloadFile(string.Format(@"http://{0}/{1}", BASE_URL, f.path), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(f.path)), true, f.hash))
                                        {
                                            errorLog.Add($"{Path.GetFileName(file.fileInfo.path)} failed to download");
                                            goto EndOfThread;
                                        }
                                    }
                                }
                            }
                        }

                        file.Downloaded = true;
                        Interlocked.Increment(ref processedFiles);

                        if (deltaPatch && file.fileInfo.patchType == PatchFile_FlagType.ChangedOriginal)
                            Interlocked.Add(ref currentBytes, long.Parse(file.fileInfo.deltaInfo.size ?? file.fileInfo.encodedInfo.size));
                        else
                            Interlocked.Add(ref currentBytes, long.Parse(file.fileInfo.encodedInfo.size));
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        _logger.LogError(ex, "Error downloading in purple updater");
                        // Debug.WriteLine(ex);
                        //Logger.log.Error("GameUpdater: {0}", ex.Message);
                    }

                EndOfThread:
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                        PatchingLabel = String.Format("{0} / {1}", IO.SizeSuffix(currentBytes, 2), IO.SizeSuffix(totalBytes, 2));
                    }));
                });

                _networkWorker.CancelAsync();
                Thread.Sleep(2000);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressValue = 0.00;
                    PatchingLabel = "Patching";
                    ProgressBlock = "";
                }));

                Thread.Sleep(1500);
                processedFiles = 0;

                Parallel.ForEach<UPDATE_FILE_ENTRY>(update_file_map, new ParallelOptions { MaxDegreeOfParallelism = _settings.System.UPDATER_THREADS + 1 }, delegate (UPDATE_FILE_ENTRY file)
                {
                    try
                    {
                        string destination = Path.GetFullPath(Path.Combine(BNS_PATH, Path.GetDirectoryName(file.fileInfo.path)));
                        if (!Directory.Exists(destination))
                            Directory.CreateDirectory(destination);

                        // This is under the assumption that delta patching is supported for the patch we're targeting and the file was marked as changed, not added or unchanged (failed hash)
                        if (deltaPatch && file.fileInfo.patchType == PatchFile_FlagType.ChangedOriginal)
                        {
                            if (file.fileInfo.deltaInfo.separates != null)
                            {
                                // So we're building a list of file names because this is how I originally designed this function and I didn't really feel like changing it around to where I just pass the separates list
                                // But essentially we build a list of the file names that will be read in order (this should already be ordered) and we pass the directory where the files are, list of split archives and the desired decompressed file path
                                List<string> archives = new List<string>();
                                file.fileInfo.deltaInfo.separates.ForEach(e => archives.Add(Path.GetFileName(e.path)));
                                var result = IO.DecompressStreamLZMA(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path)), archives, $"{Path.GetFileName(file.fileInfo.path)}.dlt");
                            }
                            else
                            {
                                IO.DecompressFileLZMA(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.deltaInfo.path)), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), $"{Path.GetFileName(file.fileInfo.path)}.dlt"));
                            }

                            // Delta is unpacked
                            if (IO.DeltaPatch(Path.Combine(destination, Path.GetFileName(file.fileInfo.path)), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), $"{Path.GetFileName(file.fileInfo.path)}.dlt")))
                            {
                                File.Move(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.path)), Path.Combine(destination, Path.GetFileName(file.fileInfo.path)), true);
                            }
                            else
                                throw new Exception($"{Path.GetFileName(file.fileInfo.path)} failed to delta patch");
                        }
                        else
                        {
                            // Didn't meet delta patch conditions so it's either a newly added file or a file that failed the hash check so we need to check if there are split parts or if the file is one whole file for download.
                            if (file.fileInfo.encodedInfo.separates != null)
                            {
                                List<string> archives = new List<string>();
                                file.fileInfo.encodedInfo.separates.ForEach(e => archives.Add(Path.GetFileName(e.path)));
                                var result = IO.DecompressStreamLZMA(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path)), archives, Path.GetFileName(file.fileInfo.path), false);
                            }
                            else
                            {
                                IO.DecompressFileLZMA(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.encodedInfo.path)), Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.path)));
                            }

                            // File is unpacked so lets move it
                            File.Move(Path.Combine(PatchDirectory, Path.GetDirectoryName(file.fileInfo.path), Path.GetFileName(file.fileInfo.path)), Path.Combine(destination, Path.GetFileName(file.fileInfo.path)), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        _logger.LogError(ex, "Error patching in Purple Updater");
                        //Debug.WriteLine(ex);
                    }
                    finally
                    {
                        Interlocked.Increment(ref processedFiles);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                            ProgressBlock = $"{IO.SizeSuffix(totalBytes, 2)} ({ProgressValue}%)";
                        }));
                    }
                });

            Cleanup:
                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = "Internal Check"; }));
                if (totalFiles > 0 && update_file_map.Any(x => !x.Downloaded))
                    errorLog.Add("Download checks failed");

                if (errorLog.Count > 0)
                    errorLog.ForEach(x => Debug.WriteLine(x));

                Thread.Sleep(500);
                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = "Cleaning up"; }));

                if (errorLog.Count == 0)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { LocalBuildText = targetVersion; }));

                    // Run UE4 Prereq installer in silent mode for people that "installed" the game
                    string pre_req = Path.Combine(_settings.System.BNS_DIR, "Engine", "Extras", "Redist", "en-us", "UE4PrereqSetup_x64.exe");
                    if (localVersion == "0" && File.Exists(pre_req))
                    {
                        try
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ProgressBlock = "Running UE4 Prereq's Installer";
                            }));

                            var proc = Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(_settings.System.BNS_DIR, "Engine", "Extras", "Redist", "en-us", "UE4PrereqSetup_x64.exe"),
                                Verb = "runas",
                                Arguments = "/silent"
                            });

                            proc.WaitForExit();
                            proc.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to run Prereq installer");
                        }
                    }

                    localVersion = targetVersion;
                    Directory.Delete(PatchDirectory, true);

                    // Update xml
                    _bns.WriteLocalBuild(targetVersion, true);

                    // Make sure the patch we are targeting matches the online patch #, if not we're multi-patching so loop back
                    if (targetVersion != onlineVersion)
                        goto StartPatchThread;
                }
                else
                    goto StartPatchThread;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Purple updater");
                //Debug.WriteLine(ex);
            }
        }

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

        private List<BnSFileInfo> BnSInfoMap;
        private List<MultipartArchives> BnSMultiParts;

        private static bool HasFlags(string input, List<string> flags)
        {
            foreach (var flag in flags)
                if (input.EndsWith(flag))
                    return true;
            return false;
        }

        private void NCLauncherGamePatcher(object sender, DoWorkEventArgs e)
        {
            try
            {
                BNS_PATH = Path.GetFullPath(_settings.System.BNS_DIR);
                string FileInfoName = $"FileInfoMap_{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}.dat";
                string PatchInfoName = $"PatchFileInfo_{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}.dat";

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

                string FileInfoURL = $"http://{BASE_URL}/{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}/{targetVersion}/Patch/{Path.GetFileNameWithoutExtension(FileInfoName)}_{targetVersion}.dat.zip";
                //string FileInfoURL = string.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, targetVersion, Path.GetFileNameWithoutExtension(FileInfoName));
                string PatchInfoURL = $"http://{BASE_URL}/{_settings.Account.REGION.GetAttribute<GameIdAttribute>().Name}/{targetVersion}/Patch/{Path.GetFileNameWithoutExtension(PatchInfoName)}_{targetVersion}.dat.zip";
                //string PatchInfoURL = string.Format(@"{0}{1}/Patch/{2}_{1}.dat.zip", BASE_URL, targetVersion, Path.GetFileNameWithoutExtension(PatchInfoName));

                totalBytes = 0L;
                currentBytes = 0L;

                BnSInfoMap = new List<BnSFileInfo>();
                BnSMultiParts = new List<MultipartArchives>();
                var partArchives = new List<MultipartArchives>();
                errorLog = new List<string>();

                bool deltaPatch = localVersion != "0" && int.Parse(targetVersion) > int.Parse(localVersion) && _settings.System.DELTA_PATCHING;
                string PatchDirectory = Path.Combine(BNS_PATH, "PatchManager", targetVersion);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ShowDeltaPatch = deltaPatch; }));

                if (!_httpClient.RemoteFileExists(PatchInfoURL))
                    throw new Exception(string.Format("PatchFileInfo for build #{0} could not be reached", onlineVersion));

                if (!Directory.Exists(PatchDirectory))
                    Directory.CreateDirectory(PatchDirectory);

                _logger.LogInformation($"PatchInfoURL: {PatchInfoURL}");
                _logger.LogInformation($"Path: {Path.Combine(PatchDirectory, PatchInfoName + ".zip")}");

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = $"Retrieving {PatchInfoName}"; }));
                if (!_httpClient.DownloadFile(PatchInfoURL, Path.Combine(PatchDirectory, PatchInfoName + ".zip"), false))
                    throw new Exception("Failed to download " + PatchInfoName);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = $"Retrieving {FileInfoName}"; }));
                if (!_httpClient.DownloadFile(FileInfoURL, Path.Combine(PatchDirectory, FileInfoName + ".zip"), false))
                    throw new Exception("Failed to download " + FileInfoName);

                Application.Current.Dispatcher.BeginInvoke(new Action(() => { ProgressBlock = "Decompressing File Maps"; }));
                IO.DecompressFileLZMA(Path.Combine(PatchDirectory, FileInfoName + ".zip"), Path.Combine(PatchDirectory, FileInfoName));
                IO.DecompressFileLZMA(Path.Combine(PatchDirectory, PatchInfoName + ".zip"), Path.Combine(PatchDirectory, PatchInfoName));

                // Fix for new installations and possibly another unknown bug?
                if(!File.Exists(Path.Combine(BNS_PATH, FileInfoName)))
                    File.Copy(Path.Combine(PatchDirectory, FileInfoName), Path.Combine(BNS_PATH, FileInfoName));

                List<string> OnlineFileInfoMap = File.ReadLines(Path.Combine(PatchDirectory, FileInfoName)).ToList<string>();
                List<string> PatchInfoMap = File.ReadLines(Path.Combine(PatchDirectory, PatchInfoName)).ToList<string>();
                List<string> CurrentFileInfoMap = File.ReadLines(Path.Combine(BNS_PATH, FileInfoName)).ToList<string>();

                int totalFiles = OnlineFileInfoMap.Count();
                int processedFiles = 0;
                int threadCount = _settings.System.UPDATER_THREADS + 1;

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
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                    }));

                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressBlock = $"{processedFiles} / {totalFiles} files scanned";
                    }));
                });

                Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                {
                    ProgressBlock = $"Download Size: {SizeSuffix(totalBytes, 2)} ({BnSInfoMap.Count()}) files";
                }));

                totalFiles = BnSInfoMap.Count();
                if (totalFiles <= 0) goto Cleanup;

                processedFiles = 0;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressValue = 0;
                    PatchingLabel = "Downloading...";
                }));

                Thread.Sleep(2000); //Create slack for progress bar to reset

                // Create and start download speed label tick
                _networkWorker.RunWorkerAsync();

                // Adding an extra thread on download just cause the download server limits speeds on each file so we'll get more bang for our buck by increasing download tasks
                Parallel.ForEach<BnSFileInfo>(BnSInfoMap, new ParallelOptions { MaxDegreeOfParallelism = _settings.System.DOWNLOADER_THREADS }, delegate (BnSFileInfo file)
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
                        if (!_httpClient.DownloadFile(string.Format(@"http://{0}/{3}/{1}/Patch/{2}", BASE_URL, targetVersion, file.path.Replace('\\', '/'), _settings.Account.REGION.GetAttribute<GameIdAttribute>().Name), Path.Combine(PatchDirectory, file.path)))
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
                            int curIndex = partArchives.FindIndex(x => x.File == FileName && x.Directory == Path.GetDirectoryName(file.path));
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
                        _logger.LogError(ex, "Download error in NCLauncher Game Patcher");
                    }

                EndOfThread:
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PatchingLabel = $"{SizeSuffix(currentBytes, 2)} / {SizeSuffix(totalBytes, 2)}";
                        ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                    }));
                });

                _networkWorker.CancelAsync();
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBlock = "Download Complete";
                }));

                Thread.Sleep(2000);
                if (partArchives.Count <= 0) goto PatchNormalFiles;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PatchingLabel = "Patching Multi";
                    ProgressValue = 0;
                }));

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
                        string destination = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, archive.Directory.Substring(archive.Directory.IndexOf("\\") + 1)));
                        var result = DecompressStreamLZMA(Path.Combine(PatchDirectory, archive.Directory), archive.Archives, archive.File); //Merge the split-archives and run through LZMA decoder

                        if (!result.IsNullOrEmpty())
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
                        _logger.LogError(ex, "Error doing multi-part patch in NCLauncher game patcher");
                        errorLog.Add(ex.Message);
                    }
                    finally
                    {
                        Interlocked.Increment(ref processedFiles);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProgressValue = (int)((double)processedFiles / totalFilesM * 100);
                        }));
                    }
                });

            PatchNormalFiles:
                Thread.Sleep(2000);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressValue = 0;
                    PatchingLabel = "Patching";
                }));
                processedFiles = 0;
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
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                            ProgressBlock = $"{SizeSuffix(totalBytes, 2)} ({ProgressValue}%)";
                        }));

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
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProgressValue = (int)((double)processedFiles / totalFiles * 100);
                            ProgressBlock = $"{SizeSuffix(totalBytes, 2)} ({ProgressValue}%)";
                        }));
                    }
                    catch (Exception ex)
                    {
                        errorLog.Add(ex.Message);
                        _logger.LogError(ex, "Error patching in NCLauncher Game Patcher");
                    }
                });

            Cleanup:
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBlock = "Internal Check";
                }));
                if (totalFiles > 0 && BnSInfoMap.Any(x => !x.Downloaded))
                    errorLog.Add("Download checks failed");

                Thread.Sleep(500);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBlock = "Cleaning up";
                }));

                // Only delete working patch directory and change versionNumber over if successful.
                if (errorLog.Count == 0)
                {
                    if (File.Exists(Path.Combine(BNS_PATH, FileInfoName)))
                        File.Delete(Path.Combine(BNS_PATH, FileInfoName));

                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { LocalBuildText = targetVersion; }));

                    File.Move(Path.Combine(PatchDirectory, FileInfoName), Path.Combine(BNS_PATH, FileInfoName));
                    _bns.WriteLocalBuild(targetVersion, _isPurple);

                    // Run UE4 Prereq installer in silent mode for people that "installed" the game
                    string pre_req = Path.Combine(_settings.System.BNS_DIR, "Engine", "Extras", "Redist", "en-us", "UE4PrereqSetup_x64.exe");
                    if (localVersion == "0" && File.Exists(pre_req))
                    {
                        try
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ProgressBlock = "Running UE4 Prereq's Installer";
                            }));

                            var proc = Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(_settings.System.BNS_DIR, "Engine", "Extras", "Redist", "en-us", "UE4PrereqSetup_x64.exe"),
                                Verb = "runas",
                                Arguments = "/silent"
                            });

                            proc.WaitForExit();
                            proc.Dispose();
                        } catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to run Prereq installer");
                        }
                    }

                    localVersion = targetVersion;
                    Directory.Delete(PatchDirectory, true);
                    if (targetVersion != onlineVersion)
                        goto StartPatchThread; // Loop back around to do yet another delta patch
                }
                else if (targetVersion != onlineVersion)
                    goto StartPatchThread;

                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                errorLog.Add(ex.Message);
                _logger.LogError(ex, "Error in NCLauncher Game Patcher");
            }

            //errorLog.ForEach(er => WriteError(er));

            // Force .NET garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
