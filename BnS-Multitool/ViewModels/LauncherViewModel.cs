using BnS_Multitool.Extensions;
using BnS_Multitool.Functions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Process = System.Diagnostics.Process;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using static BnS_Multitool.Models.Settings;
using System.Collections.Generic;
using System.Windows.Threading;
using static BnS_Multitool.Extensions.ProcessExtension;

namespace BnS_Multitool.ViewModels
{
    public partial class LauncherViewModel : ObservableValidator, IRecipient<LaunchMessage>
    {
        public class SESSION_LIST
        {
            public string EMAIL { get; set; }
            public ERegion REGION { get; set; }
            public Process PROCESS { get; set; }
            public string DisplayName { get; set; }
        }

        private readonly Settings _settings;
        private readonly BnS _bns;
        private readonly ILogger<LauncherViewModel> _logger;
        private readonly MessageService _message;
        private readonly MultiTool _mt;
        private readonly PluginData _pluginData;
        private readonly DispatcherTimer _memoryCleaner = new DispatcherTimer();

        private BackgroundWorker _processMonitor = new BackgroundWorker();

        public LauncherViewModel(Settings settings, BnS bns, ILogger<LauncherViewModel> logger, MessageService ms, MultiTool mt, PluginData pluginData)
        {
            _settings = settings;
            _bns = bns;
            _logger = logger;
            _message = ms;
            _mt = mt;
            _pluginData = pluginData;

            CurrentLanguage = _settings.Account.LANGUAGE;
            CurrentRegion = _settings.Account.REGION;
            IsAllCoresEnabled = _settings.Account.USE_ALL_CORES;
            IsTextureStreamingEnabled = _settings.Account.USE_TEXTURE_STREAMING;
            AccountList = new ObservableCollection<BNS_SAVED_ACCOUNTS_STRUCT>(_settings.Account.Saved);
            AccountListSelected = _settings.Account.LAST_USED_ACCOUNT;
            MemoryCleanerItem = _settings.Account.MEMORY_CLEANER;

            WeakReferenceMessenger.Default.RegisterAll(this);

            if (AccountListSelected != -1)
            {
                LaunchParams = AccountList[accountListSelected].PARAMS ?? string.Empty;
                EnvironmentParams = AccountList[accountListSelected].ENVARS ?? string.Empty;
            }

            _processMonitor.DoWork += MonitorActiveProcesses;
            _memoryCleaner.Tick += new EventHandler(MemoryCleanerThread);

            if (_settings.Account.MEMORY_CLEANER != MemoryCleaner_Timers.off)
            {
                _memoryCleaner.IsEnabled = true;
                _memoryCleaner.Interval = TimeSpan.FromMinutes((int)_settings.Account.MEMORY_CLEANER.GetDefaultValue());
                _memoryCleaner.Start();
            }
        }

        private void MemoryCleanerThread(object? sender, EventArgs e)
        {
            Process[] allProcs = Process.GetProcessesByName("BNSR");
            if(allProcs.Count() > 0)
            {
                foreach (var proc in allProcs)
                {
                    try
                    {
                        EmptyWorkingSet(proc.Handle);
                    }
                    catch { }
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [RelayCommand]
        void CleanMemory()
        {
            MemoryCleanerThread(null, null);
        }

        [RelayCommand]
        async Task UILoaded()
        {
            // This method does seem to run asynchronously but it's still tying up the UI thread for some reason.. Run it as another task I guess..?
            await Task.Run(PageLoaded);
        }

        private async Task PageLoaded()
        {
            try
            {
                var isLoginAvailable = _bns.LoginAvailable;
                // var BuildNumber = _bns.GetVersionInfoRelease(_settings.Account.REGION)?.GlobalVersion.ToString();
                // var isLoginAvailable = await _bns.NCLauncherService<bool>(BnS.ServiceRequest.Login);
                var BuildNumber = _bns.BuildNumber;
                string localBuild = _bns.GetLocalBuild();

                if (localBuild.IsNullOrEmpty())
                {
                    _message.Enqueue(new MessageService.MessagePrompt { Message = "Cannot find the version file for game, did you select the correct directory for your game?", IsError = true });
                    return;
                }

                if (localBuild != BuildNumber || !isLoginAvailable)
                {
                    bool loginExcept = _settings.Account.REGION == ERegion.TW && localBuild == BuildNumber;
                    if (loginExcept) goto PluginVersionCheck;

                    _message.Enqueue(new MessageService.MessagePrompt
                    {
                        Message = string.Format("{0}{1}", (!isLoginAvailable && _settings.Account.REGION != ERegion.TW) ? "The server is currently undergoing maintenance.\r" : "", localBuild != BuildNumber ? "A game update is available" : ""),
                        IsError = true,
                        UseBold = true
                    });
                }

                PluginVersionCheck:
                await CheckOnlineVersion();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        [ObservableProperty]
        private ELanguage currentLanguage;

        [ObservableProperty]
        private ERegion currentRegion;

        [ObservableProperty]
        private bool isAllCoresEnabled;

        [ObservableProperty]
        private bool isTextureStreamingEnabled;

        [ObservableProperty]
        private MemoryCleaner_Timers memoryCleanerItem;

        [ObservableProperty]
        [Required]
        [EmailAddress]
        private string newAccountEmail;

        [ObservableProperty]
        [Required]
        [MinLength(3)]
        private string newAccountPassword;

        [ObservableProperty]
        private string newAccountPin;

        [ObservableProperty]
        private ObservableCollection<BNS_SAVED_ACCOUNTS_STRUCT> accountList;

        [ObservableProperty]
        private int accountListSelected;

        [ObservableProperty]
        private string launchParams;

        [ObservableProperty]
        private Visibility pinCodeInfoVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility environmentVariableInfoVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private bool showProgressView = false;

        [ObservableProperty]
        private string progressStatusText = "";

        partial void OnLaunchParamsChanged(string value)
        {
            if (AccountListSelected == -1) return;
            if (AccountList[AccountListSelected].PARAMS == value) return;
            AccountList[AccountListSelected].PARAMS = value;
            _settings.Account.Saved = AccountList.ToList();
            _settings.Save(CONFIG.Account);
        }

        [ObservableProperty]
        private string environmentParams;

        partial void OnEnvironmentParamsChanged(string value)
        {
            if (AccountListSelected == -1) return;
            if (AccountList[AccountListSelected].ENVARS == value) return;
            AccountList[AccountListSelected].ENVARS = value;
            _settings.Account.Saved = AccountList.ToList();
            _settings.Save(CONFIG.Account);
        }

        [ObservableProperty]
        private ObservableCollection<SESSION_LIST> activeClientList = new ObservableCollection<SESSION_LIST>();

        [ObservableProperty]
        private int activeClientIndex = -1;

        partial void OnAccountListSelectedChanged(int value)
        {
            if (value == -1)
            {
                _settings.Account.LAST_USED_ACCOUNT = value;
                return;
            }

            if (value > accountList.Count)
                return;

            _settings.Account.LAST_USED_ACCOUNT = value;
            LaunchParams = AccountList[accountListSelected].PARAMS ?? string.Empty;
            EnvironmentParams = AccountList[accountListSelected].ENVARS ?? string.Empty;
            _settings.Save(CONFIG.Account);
        }

        partial void OnCurrentRegionChanged(ERegion value)
        {
            _settings.Account.REGION = value;
            _settings.Save(CONFIG.Account);

            Task.Run(async() => await PageLoaded());
        }

        partial void OnCurrentLanguageChanged(ELanguage value)
        {
            _settings.Account.LANGUAGE = value;
            _settings.Save(CONFIG.Account);
        }

        partial void OnIsAllCoresEnabledChanged(bool value)
        {
            _settings.Account.USE_ALL_CORES = value;
            _settings.Save(CONFIG.Account);
        }

        partial void OnIsTextureStreamingEnabledChanged(bool value)
        {
            _settings.Account.USE_TEXTURE_STREAMING = value;
            _settings.Save(CONFIG.Account);
        }

        partial void OnMemoryCleanerItemChanged(MemoryCleaner_Timers value)
        {
            _settings.Account.MEMORY_CLEANER = value;
            _settings.Save(CONFIG.Account);

            if (value == MemoryCleaner_Timers.off)
            {
                _memoryCleaner.IsEnabled = false;
                _memoryCleaner.Stop();
            } else
            {
                _memoryCleaner.IsEnabled = true;
                _memoryCleaner.Interval = TimeSpan.FromMinutes((int)value.GetDefaultValue());
                _memoryCleaner.Start();
            }
        }

        [RelayCommand]
        async Task DeleteAccount()
        {
            if (AccountListSelected == -1)
                return;

            AccountList.RemoveAt(AccountListSelected);
            _settings.Account.Saved = AccountList.ToList();
            await _settings.SaveAsync(CONFIG.Account);
        }

        [RelayCommand]
        void KillAllProcesses()
        {
            //ActiveClientList.Clear();
            foreach (Process proc in Process.GetProcessesByName("BNSR"))
                KillProcessAndChildrens(proc.Id);
        }

        [RelayCommand]
        void OpenPinCodeInfo() => PinCodeInfoVisibility = Visibility.Visible;

        [RelayCommand]
        void ClosePinCodeInfo() => PinCodeInfoVisibility = Visibility.Collapsed;

        [RelayCommand]
        void OpenEnvVarInfo() => EnvironmentVariableInfoVisibility = Visibility.Visible;

        [RelayCommand]
        void CloseEnvVarInfo() => EnvironmentVariableInfoVisibility = Visibility.Collapsed;

        [RelayCommand]
        void KillSelected()
        {
            if (AccountListSelected == -1) return;

            try
            {
                string email = AccountList[AccountListSelected].EMAIL;
                var ActiveClient = ActiveClientList.Where(x => x.EMAIL == email && x.REGION == CurrentRegion).FirstOrDefault();
                if (ActiveClient != null)
                {
                    if (!ActiveClient.PROCESS.HasExited)
                    {
                        ActiveClient.PROCESS.Kill();
                        ActiveClient = null;
                    }
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill process with last selected account");
            }
        }

        [RelayCommand]
        async Task AddAccount(object? obj)
        {
            // This is bad but I couldn't be bothered to go a more mvvm friendly route
            if (obj is System.Windows.Controls.PasswordBox pwbox)
            {
                NewAccountPassword = pwbox.Password;
                pwbox.Password = string.Empty;
            }

            obj = null;

            ValidateAllProperties();
            if(HasErrors)
            {
                string errorMsg = string.Join(Environment.NewLine, GetErrors().Select(e => e.ErrorMessage.Replace("NewAccount","")));
                _message.Enqueue(new MessageService.MessagePrompt { Message = errorMsg, IsError = true });
                ClearErrors();
            } else
            {
                AccountList.Add(new BNS_SAVED_ACCOUNTS_STRUCT 
                { 
                    EMAIL = NewAccountEmail, 
                    PASSWORD = NewAccountPassword, 
                    PINCODE = NewAccountPin, 
                    ENVARS = string.Empty, 
                    PARAMS = string.Empty 
                });

                _settings.Account.Saved = AccountList.ToList();
                await _settings.SaveAsync(CONFIG.Account);
                // Clear the fields
                NewAccountEmail = string.Empty;
                NewAccountPassword = string.Empty;
                NewAccountPin = string.Empty;
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        async Task LaunchGame()
        {
            int processCount = -1;
            try
            {
                processCount = Process.GetProcessesByName("BNSR").Length;
            } catch (Exception ex)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Error getting process count, anti-virus related..? Aborting launch to prevent further problems", IsError = true, UseBold = true });
                _logger.LogError(ex, "Error getting process count");
                return;
            }

            // Anti-cheat check (controlled by me)
            if (_mt.MT_Info.ANTI_CHEAT_ENABLED && CurrentRegion != ERegion.TW)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "The anti-cheat for this region will block use of multi-tool, therefor launching has been disabled temporarily", IsError = true });
                return;
            }

            if (AccountListSelected == -1)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "No account selected, select an account before attempting to launch a new client", IsError = true });
                return;
            } else if (processCount == 0 && !_pluginData.IsPluginInstalled("loader3"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Loader3 is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }
            else if (processCount == 0 && !_pluginData.IsPluginInstalled("loginhelper"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "LoginHelper is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }
            else if (processCount == 0 && !_pluginData.IsPluginInstalled("bnsnogg"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "GameGuard Bypass is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }

            // Check for plugin updates and other stuff
            try
            {
                if (_settings.System.AUTO_UPDATE_PLUGINS && processCount == 0)
                {
                    ShowProgressView = true;
                    ProgressStatusText = "Checking for plugin updates";
                    await _pluginData.UpdateInstalledPlugins();
                    await Task.Delay(1500);
                    ShowProgressView = false;
                }
                else if (processCount == 0 && !_settings.Account.AUTPATCH_QOL)
                {
                    // Special condition for multitool_qol
                    ShowProgressView = true;
                    ProgressStatusText = "Checking for qol plugin update";
                    await Task.Delay(200);
                    var plugins = await _pluginData.RetrieveOnlinePlugins();
                    if (plugins != null)
                        _pluginData.Plugins = plugins;

                    var qol_plugin = _pluginData.Plugins?.PluginInfo.FirstOrDefault(x => x.Name == "multitool_qol");
                    if (qol_plugin != null)
                        await _pluginData.InstallPlugin(qol_plugin);

                    await Task.Delay(1500);
                    ShowProgressView = false;
                }
            } catch (Exception ex)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = $"Failed to update plugin(s)\r{ex.Message}", IsError = true });
                _logger.LogError(ex, "Failed to update plugin(s)");
                ShowProgressView = false;
            }

            // Continue launching
            try
            {
                var account = AccountList[AccountListSelected];
                var ActiveClient = ActiveClientList.Where(x => x.EMAIL == account.EMAIL && x.REGION == CurrentRegion).FirstOrDefault();
                if (ActiveClient == null)
                {
                    // Launch game
                    await LaunchNewGameClient();
                }
                else
                {
                    if (ActiveClient.PROCESS.HasExited)
                    {
                        ActiveClient = null;
                        await LaunchNewGameClient();
                    }
                    else
                    {
                        ActiveClient.PROCESS.Kill();
                        await LaunchNewGameClient();
                    }
                }

                if (ActiveClientList.Count > 0 && !_processMonitor.IsBusy)
                    _processMonitor.RunWorkerAsync();

            } catch (Exception ex)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "There was an error trying to launch a client, check the logs for specifics", IsError = true });
                _logger.LogError(ex, "Failed to launch new game");
            }
        }

        private async Task LaunchNewGameClient()
        {
            try
            {
                var account = AccountList[AccountListSelected];
                string EMAIL = account.EMAIL;
                var ActiveClient = ActiveClientList.FirstOrDefault(x => x.EMAIL == EMAIL && x.REGION == CurrentRegion);

                Process proc = new Process();
                proc.StartInfo.FileName = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe");

                // base arguments needed
                proc.StartInfo.ArgumentList.Add("/sesskey");
                proc.StartInfo.ArgumentList.Add("/LaunchByLauncher");
                proc.StartInfo.ArgumentList.Add("/LoginMode");
                proc.StartInfo.ArgumentList.Add("-FIXPROGRAMID");
                proc.StartInfo.ArgumentList.Add("-unattended");

                // Additional params to add
                if (IsAllCoresEnabled) proc.StartInfo.ArgumentList.Add("-USEALLAVAILABLECORES");
                if (IsTextureStreamingEnabled) proc.StartInfo.ArgumentList.Add("-NOTEXTURESTREAMING");

                // Region other than TW
                if(CurrentRegion != ERegion.TW && CurrentRegion != ERegion.JP)
                {
                    proc.StartInfo.ArgumentList.Add($"-lang:{CurrentLanguage.GetDescription()}");
                    proc.StartInfo.ArgumentList.Add($"-region:{(int)CurrentRegion}");
                }

                // Finally add the additional params that a user may specify
                if (!LaunchParams.IsNullOrEmpty()) proc.StartInfo.ArgumentList.Add(LaunchParams);

                // If region is NCW truncate the password to a max length of 16, TW is unknown if character limit is 16
                string password = CurrentRegion != ERegion.TW ? account.PASSWORD.Truncate(16) : account.PASSWORD;

                // Setup environment variables for loginhelper
                proc.StartInfo.UseShellExecute = false; // Required for setting environment variables
                proc.StartInfo.EnvironmentVariables["BNS_PROFILE_USERNAME"] = account.EMAIL;
                proc.StartInfo.EnvironmentVariables["BNS_PROFILE_PASSWORD"] = password;

                if (int.TryParse(account.PINCODE, out _))
                    proc.StartInfo.EnvironmentVariables["BNS_PROFILE_PIN"] = account.PINCODE;
                else if (!account.PINCODE.IsNullOrEmpty())
                    proc.StartInfo.EnvironmentVariables["BNS_PROFILE_OTP_SECRET"] = account.PINCODE;

                // Check if bns patch directory is being changed
                if (_settings.System.BNSPATCH_DIRECTORY != Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS"))
                    proc.StartInfo.EnvironmentVariables["BNS_PROFILE_XML"] = Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml");

                // Set additional environment params specified by user
                if(!EnvironmentParams.IsNullOrEmpty())
                {
                    var variables = EnvironmentParams.Split(';');
                    try
                    {
                        foreach (var variable in variables)
                        {
                            var key = variable.Split('=')[0];
                            var value = variable.Split("=")[1];
                            proc.StartInfo.EnvironmentVariables[key] = value;
                        }
                    }
                    catch { }
                }

                // If I ever want to redirect the output standard outputs for multi-tool to read flip to true
                proc.StartInfo.RedirectStandardError = false;
                proc.StartInfo.RedirectStandardOutput = false;

                // Finally.. launch the client
                proc.Start();
                
                if (ActiveClient == null)
                {
                    ActiveClientList.Add(new SESSION_LIST
                    {
                        EMAIL = account.EMAIL,
                        REGION = CurrentRegion,
                        PROCESS = proc,
                        DisplayName = $"{account.EMAIL} - {CurrentRegion}"
                    });
                } else
                    ActiveClient.PROCESS = proc;

                if (proc.HasExited) goto GarbageCollection;

                switch(_settings.System.NEW_GAME_OPTION)
                {
                    case EStartNewGame.Nothing: break;
                    case EStartNewGame.MinimizeLauncher:
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        break;
                    case EStartNewGame.CloseLauncher:
                        Application.Current.Shutdown();
                        break;
                    default:
                        WeakReferenceMessenger.Default.Send(new NotifyIconMessage(true));
                        break;
                }

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch new game client");
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Failed to launch game client\rCheck the logs for more information", IsError = true });
            } finally
            {
                await Task.CompletedTask;
            }

        GarbageCollection:
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void MonitorActiveProcesses(object s, DoWorkEventArgs e)
        {
            while(ActiveClientList.Count > 0)
            {
                try
                {
                    foreach (var session in ActiveClientList.Select((item, index) => new { index, item }).ToList())
                    {
                        if (!IsProcessAlive(session.item.PROCESS.Id))
                        {
                            App.Current.Dispatcher.BeginInvoke((Action)delegate
                            {
                                if (activeClientList.ElementAtOrDefault(session.index) != null)
                                    ActiveClientList.RemoveAt(session.index);
                            });
                        }
                    }
                } catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                
                System.Threading.Thread.Sleep(1000);
            }

            if (_settings.System.NEW_GAME_OPTION == EStartNewGame.MinimizeLauncher || _settings.System.NEW_GAME_OPTION == EStartNewGame.MinimizeTray)
                WeakReferenceMessenger.Default.Send(new NotifyIconMessage(false));
        }

        [RelayCommand]
        void ClientsDoubleClick()
        {
            if(ActiveClientIndex != -1 && ActiveClientList.Count > 0)
            {
                var client = ActiveClientList[ActiveClientIndex];
                client.PROCESS.Kill();
                ActiveClientList.RemoveAt(ActiveClientIndex);
                ActiveClientIndex = -1;
            }
        }

        class requiredList
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Hash { get; set; }
        }

        [ObservableProperty]
        private string pluginInfoText;

        private async Task CheckOnlineVersion()
        {
            var plugins = await _pluginData.RetrieveOnlinePlugins();
            if (plugins == null)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Failed to retrieve plugins from server", IsError = true, UseBold = true });
                return;
            }

            _pluginData.Plugins = plugins;
            var loader3 = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("loader3"));
            var bnsnogg = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("bnsnogg"));
            var loginhelper = plugins.PluginInfo.FirstOrDefault(x => x.Name.Equals("loginhelper"));

            if (loader3 == null || bnsnogg == null || loginhelper == null)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Error getting information about one of the plugins", IsError = true, UseBold = true });
                return;
            }

            bool GameRunning = ActiveClientList.Count > 0 || Process.GetProcessesByName("BNSR").Length > 0;

            var requiredList = new List<requiredList>
            {
                new requiredList { Name = "Loader3", Path = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, loader3.FilePath)), Hash = loader3.Hash},
                new requiredList { Name = "GameGuard Bypass", Path = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, bnsnogg.FilePath)), Hash = bnsnogg.Hash},
                new requiredList { Name = "LoginHelper", Path = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, loginhelper.FilePath)), Hash = loginhelper.Hash}
            };

            PluginInfoText = "";

            foreach (var item in requiredList)
            {
                if (File.Exists(item.Path))
                {
                    if (Crypto.CRC32_File(item.Path) == item.Hash || GameRunning)
                    {
                        System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE10B");
                        text.Foreground = System.Windows.Media.Brushes.Green;
                        PluginInfoText += text.Text;
                    }
                    else
                    {
                        System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE118");
                        text.Foreground = System.Windows.Media.Brushes.Yellow;

                        PluginInfoText += text.Text;
                    }
                }
                else
                {
                    System.Windows.Documents.Run text = new System.Windows.Documents.Run("\uE10A");
                    text.Foreground = System.Windows.Media.Brushes.Red;
                    PluginInfoText += text.Text;
                }

                System.Windows.Documents.Run name = new System.Windows.Documents.Run(" " + item.Name + "\r\n");
                PluginInfoText += name.Text;
            }
        }

        [RelayCommand]
        async Task InstallRequired()
        {
            try
            {
                PluginInfoText = "";
                //ProgressControl.updateProgressLabel("Checking for plugin updates");
                var plugins = await _pluginData.RetrieveOnlinePlugins();
                if (plugins != null)
                {
                    _pluginData.Plugins = plugins;
                    foreach (var plugin in plugins.PluginInfo)
                    {
                        if (plugin.FullName.IsNullOrEmpty()) continue;
                        var path = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
                        if (!File.Exists(path)) continue;

                        if (Crypto.CRC32_File(path) != plugin.Hash)
                        {
                            //ProgressControl.updateProgressLabel(string.Format("Updating {0}", plugin.Title));
                            await _pluginData.InstallPlugin(plugin);
                        }
                    }
                }

                _message.Enqueue(new MessageService.MessagePrompt { Message = "Plugins installed or up-to-date", IsError = false, UseBold = true });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing or updating required plugins");
                _message.Enqueue(new MessageService.MessagePrompt { Message = "An error occured installing or updating required plugins", IsError = true, UseBold = false });
            }

            await Task.Delay(100);
            await CheckOnlineVersion();
        }

        [RelayCommand]
        void NavigateQoL() => WeakReferenceMessenger.Default.Send(new NavigateMessage("QoL"));

        private void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
                foreach (ManagementObject mo in processCollection)
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Launcher::KillProcessAndChildrens");
                //Logger.log.Error("Launcher::KillProcessAndChildrens::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
            }
        }

        void IRecipient<LaunchMessage>.Receive(LaunchMessage message) =>
            Task.Run(async () => await LaunchGame());
    }
}
