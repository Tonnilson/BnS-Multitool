using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;
using BnS_Multitool.Extensions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BnS_Multitool.ViewModels
{
    public partial class MainViewModel : ObservableValidator, IRecipient<ShowMessagePrompt>, IRecipient<NotifyIconMessage>, IRecipient<NavigateMessage>
    {
        /// <summary>
        /// Error Message Prompt Variables
        /// </summary>
        [ObservableProperty]
        private bool showError = false;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private FontWeight errorWeight = FontWeights.Normal;

        [ObservableProperty]
        private double errorSize = 14;

        [ObservableProperty]
        private string errorPicture = "/Images/worry/feelsworry.png";

        /// <summary>
        /// Other observable variables
        /// </summary>
        [ObservableProperty]
        private bool isUpdateAvailable = false;

        [ObservableProperty]
        private WindowState currentWindowState;

        [ObservableProperty]
        private bool showTaskbar = true;

        [ObservableProperty]
        private bool isWindowVisible = true;

        [ObservableProperty]
        private string versionTitleBar = "BnS Multi Tool: 5.0.0";

        private readonly Settings _settings;
        private readonly NotifyIconService _notifyService;
        private readonly MultiTool _mt;
        private readonly ILogger<MainViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly XmlModel _xmlModel;
        private readonly MessageService _message;

        [RelayCommand]
        void CloseWindow()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
       void MinimizeWindow()
        {
            if (_settings.System.MINIMZE_ACTION == EMinimizeWindow.Minimize)
                CurrentWindowState = WindowState.Minimized;
            else
            {
                App.Current.MainWindow.Hide();
                _notifyService.TaskTrayControl(true);
                IsWindowVisible = false;
                CurrentWindowState = WindowState.Minimized;
                ShowTaskbar = false;
            }
        }

        [RelayCommand]
        void CloseErrorPrompt()
        {
            ErrorMessage = string.Empty;
            ShowError = false;
            _message.MessageClosed.TrySetResult(true);
        }

        [RelayCommand]
        void OpenSettings()
        {
            MessagePrompt("Generic error message");
        }

        public void MessagePrompt(string message, bool isError = false, bool bold = false)
        {
            ErrorMessage = message;
            ErrorWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            errorSize = !isError ? 16 : 14;

            if (_settings.System.THEME == 0)
                ErrorPicture = !isError ? "/Images/worry/poggies.png" : "/Images/worry/feelsworry.png";
            else
                ErrorPicture = !isError ? "/Images/agon/agonHappy.png" : "/Images/agon/agonSob.png";

            ShowError = true;
        }

        /// <summary>
        /// Navigation stuff
        /// </summary>
        [ObservableProperty]
        private object currentView;

        [RelayCommand]
        void Navigate(string dest)
        {
            // Make sure we're not trying to go to the same location
            if (CurrentView.GetType().Name.Contains(dest))
                return;

            // Need to tell the MainPage VM that we're unloading, using the event does not operate how we want it to so this is needed.
            if(CurrentView.GetType().Name == "MainPageViewModel")
                WeakReferenceMessenger.Default.Send(new UnloadedMainPageVM(true));

            // This is kind of bad, needs to and hopefully will be replaced with navigationService
            switch(dest)
            {
                case "MainPage":
                    CurrentView = _serviceProvider.GetRequiredService<MainPageViewModel>();
                    break;
                case "Launcher":
                    CurrentView = _serviceProvider.GetRequiredService<LauncherViewModel>();
                    break;
                case "Plugins":
                    CurrentView = _serviceProvider.GetRequiredService<PluginsViewModel>();
                    break;
                case "Mods":
                    CurrentView = _serviceProvider.GetRequiredService<ModsViewModel>();
                    break;
                case "Sync":
                    CurrentView = _serviceProvider.GetRequiredService<SyncViewModel>();
                    break;
                case "Patches":
                    CurrentView = _serviceProvider.GetRequiredService<PatchesViewModel>();
                    break;
                case "Effects":
                    CurrentView = _serviceProvider.GetRequiredService<EffectsViewModel>();
                    break;
                case "ExtendedOptions":
                    CurrentView = _serviceProvider.GetRequiredService<ExtendedOptionsViewModel>();
                    break;
            }

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        public MainViewModel(Settings settings, NotifyIconService notifyService, MultiTool mt, ILogger<MainViewModel> logger, IServiceProvider serviceProvider, XmlModel xmlModel, MessageService ms)
        {
            _settings = settings;
            _notifyService = notifyService;
            _mt = mt;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _xmlModel = xmlModel;
            _message = ms;
            VersionTitleBar = $"BnS Multi Tool: {_settings.VersionInfo()}";
            CurrentTheme = _settings.System.THEME;
            CurrentNewGameOption = _settings.System.NEW_GAME_OPTION;
            CurrentMinimizeOption = _settings.System.MINIMZE_ACTION;
            CurrentGamePath = _settings.System.BNS_DIR;
            IsRelayEnabled = _settings.System.BUILD_RELAY;
            IsPingEnabled = _settings.System.PING_CHECK;
            IsDeltaPatchingEnabled = _settings.System.DELTA_PATCHING;
            UpdaterThreads = _settings.System.UPDATER_THREADS;
            DownloadThreads = _settings.System.DOWNLOADER_THREADS;

            // Force a theme update to match the config
            UpdateThemes();

            // Allows communications without coupling from other sources.
            WeakReferenceMessenger.Default.RegisterAll(this);
            _xmlModel = xmlModel;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var serverVersion = await _mt.VersionInfo();
                if (serverVersion == null)
                    throw new Exception("Failed to retrieve info from server");

                if (serverVersion.VERSION != _settings.VersionInfo())
                {
                    // Update notification
                    IsUpdateAvailable = true;
                    _message.Enqueue(new MessageService.MessagePrompt { Message = $"A new update is available!\rBe sure to read the change log for any important changes.\r\rNew: {serverVersion.VERSION}\rCurrent: {_settings.VersionInfo()}", IsError = false, UseBold = true });
                }

            }
            catch (Exception ex)
            {
                // Log
                _logger.LogError(ex, "Failed to retrieve info from main server");
            }

            CurrentView = _serviceProvider.GetRequiredService<MainPageViewModel>();
            //await _xmlModel.InitalizeAsync();

            try
            {
                // Applying a needed fix for older patches.xml, eventually there will be multiple children with the same name so we need to change select-node to select-nodes
                if (File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml")))
                {
                    XDocument patches = XDocument.Load(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml"));
                    var child = patches.XPathSelectElement("//select-node[contains(@query, 'self-restraint-gauge-time')]");
                    if (child != null)
                    {
                        child.Name = "select-nodes";

                        child = patches.XPathSelectElement("//select-node[contains(@query, 'rapid-decompose-duration')]");
                        if (child != null)
                            child.Name = "select-nodes";

                        patches.Save(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml"));
                    }
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load and modify patches.xml");
            }

            _logger.LogInformation("Finished initializing MainView");
        }

        [ObservableProperty]
        private bool isSettingsOpen = false;

        [ObservableProperty]
        private EThemes currentTheme;

        [ObservableProperty]
        private EStartNewGame currentNewGameOption;

        [ObservableProperty]
        private EMinimizeWindow currentMinimizeOption;

        [ObservableProperty]
        private string currentGamePath;

        [ObservableProperty]
        private bool isPingEnabled;

        [ObservableProperty]
        private bool isRelayEnabled;

        [ObservableProperty]
        private bool isDeltaPatchingEnabled;

        [ObservableProperty]
        private int updaterThreads;

        [ObservableProperty]
        private int downloadThreads;

        [RelayCommand]
        void ClearFTH()
        {
            try
            {
                var state = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH\State", true);
                var stateobj = state.GetValue(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                if (stateobj != null)
                    state.DeleteValue(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));

                MessagePrompt("Any FTH State for BNSR.exe has been cleared", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear FTH state");
            }
        }

        [RelayCommand]
        void FTHExclusion()
        {
            try
            {
                RegistryKey FTH = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH", true);
                var exclusionObj = FTH.GetValue("ExclusionList");
                var exclusionList = new List<string>(exclusionObj as string[]);

                if (!exclusionList.Any(exe => exe.Contains("BNSR.exe")))
                {
                    exclusionList.Add(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                    FTH.SetValue("ExclusionList", exclusionList.ToArray());

                    // Purge the state for BNSR.exe (Requires full path to exe)
                    var state = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\FTH\State", true);
                    var stateobj = state.GetValue(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));
                    if (stateobj != null)
                        state.DeleteValue(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64", "BNSR.exe"));

                    // Prompt user that an exclusion was set, pass true for Poggies dialog
                    MessagePrompt("FTH Exclusion set for BNSR.exe", true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to add FTH Exclusion");
                MessagePrompt(ex.Message);
            }
        }

        [RelayCommand]
        void RemoveCompatOptions()
        {
            try
            {
                // Get rid of compatibility options on BNSR.exe
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                var keys = key.GetValueNames().Where(x => x.Contains(@"BNSR\Binaries\Win64\BNSR.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

                key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                keys = key.GetValueNames().Where(x => x.Contains(@"BNSR\Binaries\Win64\BNSR.exe"));
                foreach (var v in keys)
                    key.DeleteValue(v);

                MessagePrompt("Compatibility Options have been cleared for BNSR.exe", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove compatibility options from registry");
            }
        }

        [RelayCommand]
        void BrowseDirectory()
        {
            using (var FOLDER = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                    CurrentGamePath = FOLDER.SelectedPath + ((FOLDER.SelectedPath.Last() != '\\') ? "\\" : "");
            }
        }

        [RelayCommand]
        void CloseSettings()
        {
            CurrentTheme = _settings.System.THEME;
            CurrentNewGameOption = _settings.System.NEW_GAME_OPTION;
            CurrentMinimizeOption = _settings.System.MINIMZE_ACTION;
            CurrentGamePath = _settings.System.BNS_DIR;
            IsRelayEnabled = _settings.System.BUILD_RELAY;
            IsPingEnabled = _settings.System.PING_CHECK;
            IsDeltaPatchingEnabled = _settings.System.DELTA_PATCHING;
            UpdaterThreads = _settings.System.UPDATER_THREADS;
            DownloadThreads = _settings.System.DOWNLOADER_THREADS;
            IsSettingsOpen = false;
        }

        [RelayCommand]
        async void SaveSettings()
        {
            if(_settings.System.THEME != CurrentTheme)
            {
                _settings.System.THEME = CurrentTheme;
                UpdateThemes();
            }

            _settings.System.NEW_GAME_OPTION = CurrentNewGameOption;
            _settings.System.MINIMZE_ACTION = CurrentMinimizeOption;
            _settings.System.BNS_DIR = CurrentGamePath;
            _settings.System.BUILD_RELAY = IsRelayEnabled;
            _settings.System.PING_CHECK = IsPingEnabled;
            _settings.System.DELTA_PATCHING = IsDeltaPatchingEnabled;
            _settings.System.UPDATER_THREADS = UpdaterThreads;
            _settings.System.DOWNLOADER_THREADS = DownloadThreads;
            await _settings.SaveAsync(Settings.CONFIG.Settings);
            IsSettingsOpen = false;
        }

        [RelayCommand]
        void ShowSettings() =>
            IsSettingsOpen = true;

        /// <summary>
        /// Theme Stuff
        /// </summary>
        [ObservableProperty]
        private string naviMainIcon = EThemeIcons.Main.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviPlayIcon = EThemeIcons.Play.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviPatchesIcon = EThemeIcons.Patches.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviEffectsIcon = EThemeIcons.Effects.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviModsIcon = EThemeIcons.Mods.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviPluginsIcon = EThemeIcons.Plugins.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviUpdaterIcon = EThemeIcons.Updater.GetAttribute<WorryTheme>().Name;
        [ObservableProperty]
        private string naviHeaderIcon = EThemeIcons.Header.GetAttribute<WorryTheme>().Name;

        void UpdateThemes()
        {
            if(_settings.System.THEME == EThemes.Worry)
            {
                 NaviMainIcon = EThemeIcons.Main.GetAttribute<WorryTheme>().Name;
                 NaviPlayIcon = EThemeIcons.Play.GetAttribute<WorryTheme>().Name;
                 NaviPatchesIcon = EThemeIcons.Patches.GetAttribute<WorryTheme>().Name;
                 NaviEffectsIcon = EThemeIcons.Effects.GetAttribute<WorryTheme>().Name;
                 NaviModsIcon = EThemeIcons.Mods.GetAttribute<WorryTheme>().Name;
                 NaviPluginsIcon = EThemeIcons.Plugins.GetAttribute<WorryTheme>().Name;
                 NaviUpdaterIcon = EThemeIcons.Updater.GetAttribute<WorryTheme>().Name;
                 NaviHeaderIcon = EThemeIcons.Header.GetAttribute<WorryTheme>().Name;
            } else
            {
                NaviMainIcon = EThemeIcons.Main.GetAttribute<AgonTheme>().Name;
                NaviPlayIcon = EThemeIcons.Play.GetAttribute<AgonTheme>().Name;
                NaviPatchesIcon = EThemeIcons.Patches.GetAttribute<AgonTheme>().Name;
                NaviEffectsIcon = EThemeIcons.Effects.GetAttribute<AgonTheme>().Name;
                NaviModsIcon = EThemeIcons.Mods.GetAttribute<AgonTheme>().Name;
                NaviPluginsIcon = EThemeIcons.Plugins.GetAttribute<AgonTheme>().Name;
                NaviUpdaterIcon = EThemeIcons.Updater.GetAttribute<AgonTheme>().Name;
                NaviHeaderIcon = EThemeIcons.Header.GetAttribute<AgonTheme>().Name;
            }
        }

        // Message receiver for display message prompts
        void IRecipient<ShowMessagePrompt>.Receive(ShowMessagePrompt message) => MessagePrompt(message.Value.Message, message.Value.IsError, message.Value.UseBold);

        void IRecipient<NotifyIconMessage>.Receive(NotifyIconMessage message)
        {
            if (message.Value)
                MinimizeWindow();
            else
            {
                IsWindowVisible = true;
                CurrentWindowState = WindowState.Normal;
                ShowTaskbar = true;
                _notifyService.TaskTrayControl(false);
            }
        }

        void IRecipient<NavigateMessage>.Receive(NavigateMessage message) =>
            Navigate(message.Value);
    }
}
