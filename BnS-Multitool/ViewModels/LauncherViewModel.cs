using BnS_Multitool.Extensions;
using BnS_Multitool.Functions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NLog;
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

namespace BnS_Multitool.ViewModels
{
    public partial class LauncherViewModel : ObservableValidator
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
        private bool _init = false;

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

            if (AccountListSelected != -1)
            {
                LaunchParams = AccountList[accountListSelected].PARAMS ?? string.Empty;
                EnvironmentParams = AccountList[accountListSelected].ENVARS ?? string.Empty;
            }

            _processMonitor.DoWork += MonitorActiveProcesses;
            _init = true;
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
                //var BuildNumber = _bns.GetVersionInfoRelease(_settings.Account.REGION)?.GlobalVersion.ToString();
                //var isLoginAvailable = await _bns.NCLauncherService<bool>(BnS.ServiceRequest.Login);
                var BuildNumber = _bns.BuildNumber;
                string localBuild = string.Empty;

                var vInfo_path = Path.Combine(_settings.System.BNS_DIR, $"VersionInfo_{CurrentRegion.GetAttribute<GameIdAttribute>().Name}.ini");
                if (!File.Exists(vInfo_path))
                {
                    _message.Enqueue(new MessageService.MessagePrompt { Message = "Cannot find the version file for game, did you select the correct directory for your game?", IsError = true });
                    return;
                }

                var reader = new IniReader(vInfo_path);
                localBuild = reader.Read("VersionInfo", "GlobalVersion");

                if (localBuild != BuildNumber || !isLoginAvailable)
                {
                    _message.Enqueue(new MessageService.MessagePrompt
                    {
                        Message = string.Format("{0}{1}", !isLoginAvailable ? "The server is currently undergoing maintenance.\r" : "", localBuild != BuildNumber ? "A game update is available" : ""),
                        IsError = true,
                        UseBold = true
                    });
                }

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

            if (_init)
                _settings.Save(CONFIG.Account);
        }

        partial void OnCurrentRegionChanged(ERegion value)
        {
            _settings.Account.REGION = value;
            if (_init)
                _settings.Save(CONFIG.Account);
        }

        partial void OnCurrentLanguageChanged(ELanguage value)
        {
            _settings.Account.LANGUAGE = value;
            if (_init)
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
            var processCount = Process.GetProcessesByName("BNSR").Count();

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
            } else if (processCount == 0 && _pluginData.IsPluginInstalled("loader3"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Loader3 is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }
            else if (processCount == 0 && _pluginData.IsPluginInstalled("loginhelper"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "LoginHelper is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }
            else if (processCount == 0 && _pluginData.IsPluginInstalled("bnsnogg"))
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = "GameGuard Bypass is missing, you can click the install button below to get the required plugins", IsError = true, UseBold = true });
                return;
            }

            // Check for plugin updates and other stuff
            try
            {
                if (_settings.System.AUTO_UPDATE_PLUGINS && processCount == 0)
                    await _pluginData.UpdateInstalledPlugins();
                else if (processCount == 0 && _settings.Account.AUTPATCH_QOL == 0)
                {
                    // Special condition for multitool_qol
                    var plugins = await _pluginData.RetrieveOnlinePlugins();
                    if (plugins != null)
                        _pluginData.Plugins = plugins;

                    var qol_plugin = _pluginData.Plugins?.PluginInfo.FirstOrDefault(x => x.Name == "multitool_qol");
                    if (qol_plugin != null)
                        await _pluginData.InstallPlugin(qol_plugin);
                }
            } catch (Exception ex)
            {
                _message.Enqueue(new MessageService.MessagePrompt { Message = $"Failed to update plugin(s)\r{ex.Message}", IsError = true });
                _logger.LogError(ex, "Failed to update plugin(s)");
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
                if(CurrentRegion != ERegion.TW)
                {
                    proc.StartInfo.ArgumentList.Add($"-lang:{CurrentLanguage.GetDescription()}");
                    proc.StartInfo.ArgumentList.Add($"-region:{CurrentRegion}");
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
                    case EStartNewGame.CloseLauncher:
                        App.Current.Shutdown();
                        break;
                    default:
                        WeakReferenceMessenger.Default.Send(new NotifyIconMessage(true));
                        break;
                }

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch new game client");
                _message.Enqueue(new MessageService.MessagePrompt { Message = "Failed to launch game client\rCheck the logs for more information", IsError = true });
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
                        Process? proc = null;
                        try
                        {
                            proc = Process.GetProcessById(session.item.PROCESS.Id);
                        }
                        catch { }

                        if (proc == null || proc.HasExited)
                        {
                            App.Current.Dispatcher.BeginInvoke((Action)delegate
                            {
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
    }
}
