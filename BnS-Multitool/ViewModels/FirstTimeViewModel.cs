using BnS_Multitool.Extensions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BnS_Multitool.ViewModels
{
    public partial class FirstTimeViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly BnS _bns;
        private readonly ILogger<FirstTimeViewModel> _logger;

        public class NCClient
        {
            public string Region { get; set; }
            public string GamePath { get; set; }
            public ERegion RegionInfo { get; set; }
        }

        public FirstTimeViewModel(Settings settings, BnS bns, ILogger<FirstTimeViewModel> logger) 
        {
            _settings = settings;
            _bns = bns;
            _logger = logger;

            DownloadThreads = _settings.System.DOWNLOADER_THREADS;
            UpdaterThreads = _settings.System.UPDATER_THREADS;
        }

        [ObservableProperty]
        private ObservableCollection<NCClient> clientsCollection = new ObservableCollection<NCClient>();

        [ObservableProperty]
        private NCClient selectedClient;

        [ObservableProperty]
        private int updaterThreads;

        [ObservableProperty]
        private int downloadThreads;

        [ObservableProperty]
        private ERegion currentRegion;

        [ObservableProperty]
        private ELanguage currentLanguage;

        [ObservableProperty]
        private bool showClientOptions = false;

        [ObservableProperty]
        private bool showRegionSelector = false;

        [ObservableProperty]
        private bool showLanguageSelector = false;

        [ObservableProperty]
        private string gameDirectory;

        [ObservableProperty]
        private bool showManualSetup = false;

        [ObservableProperty]
        private bool showDetectedClients = false;

        [RelayCommand]
        async Task FinishSetup()
        {
            if (string.IsNullOrEmpty(GameDirectory)) return;

            _settings.System.UPDATER_THREADS = UpdaterThreads;
            _settings.System.DOWNLOADER_THREADS = DownloadThreads;
            _settings.Account.REGION = CurrentRegion;
            _settings.Account.LANGUAGE = CurrentLanguage;
            _settings.System.BNS_DIR = GameDirectory;
            await _settings.SaveAsync(Settings.CONFIG.Settings);
            await _settings.SaveAsync(Settings.CONFIG.Account);

            WeakReferenceMessenger.Default.Send(new CloseMessage(true));
        }

        [RelayCommand]
        async Task UILoaded()
        {
            // This method does seem to run asynchronously but it's still tying up the UI thread for some reason.. Run it as another task I guess..?
            await Task.Run(InitializeAsync);
        }

        [RelayCommand]
        void BrowseGame()
        {
            using (var FOLDER = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                    GameDirectory = FOLDER.SelectedPath + ((FOLDER.SelectedPath.Last() != '\\') ? "\\" : "");
            }
        }

        partial void OnSelectedClientChanged(NCClient value)
        {
            if (value == null) return;

            switch (value.RegionInfo)
            {
                case ERegion.TW:
                    ShowRegionSelector = false;
                    ShowLanguageSelector = false;
                    CurrentLanguage = ELanguage.ZHTW;
                    CurrentRegion = ERegion.TW;
                    break;

                case ERegion.JP:
                    ShowRegionSelector = false;
                    ShowLanguageSelector = false;
                    CurrentLanguage = ELanguage.JP;
                    CurrentRegion = ERegion.JP;
                    break;

                default:
                    ShowRegionSelector = true;
                    ShowLanguageSelector = true;
                    CurrentLanguage = ELanguage.EN;
                    CurrentRegion = ERegion.NA;
                    break;
            }

            GameDirectory = value.GamePath;
            ShowClientOptions = true;
        }

        private async Task InitializeAsync()
        {
            foreach (var region in Enum.GetValues<ERegion>())
            {
                if (region.GetDescription() == "EU") continue;

                RegistryKey rKey = Registry.LocalMachine.OpenSubKey(region.GetAttribute<RegistryPathAttribute>().Path);
                if (rKey == null) continue;

                string gamePath = rKey.GetValue("BaseDir").ToString();
                if (gamePath == null) continue;
                if (!Directory.Exists(gamePath)) continue;

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ClientsCollection.Add(new NCClient { Region = region.GetAttribute<RegionAttribute>().Name, GamePath = gamePath, RegionInfo = region });
                }));
            }

            ShowDetectedClients = ClientsCollection.Count > 0;
            ShowManualSetup = ClientsCollection.Count == 0;

            // Would do a loop if I supported other regions
            await Task.CompletedTask;
        }
    }
}
