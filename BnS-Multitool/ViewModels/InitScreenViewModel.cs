using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Security;
using System;
using System.IO;
using System.Threading.Tasks;
using static BnS_Multitool.Models.Settings;
using System.Windows;
using BnS_Multitool.Extensions;
using BnS_Multitool.View;

namespace BnS_Multitool.ViewModels
{
    public partial class InitScreenViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        public InitScreenViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [ObservableProperty]
        private string statusText = "Loading...";

        [ObservableProperty]
        private bool preloadElement = true;

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS")))
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS"));

            // This is a dummy element used to load some COM interfaces in so there is no stuttering later on when they do get loaded
            // Leaving in as a oh shit the routed event keyframe failed which probably won't happen
            PreloadElement = false;

            // Initialize some of our instances
            StatusText = "Loading Settings";
            var _settings = _serviceProvider.GetRequiredService<Settings>();
            _settings.Initialize();

            if (_settings.System.FINGERPRINT.IsNullOrEmpty())
            {
                _settings.System.FINGERPRINT = FingerPrint.Value();
                _settings.Save(CONFIG.Settings);
            }

            // First time setup
            if (_settings.System.BNS_DIR.IsNullOrEmpty())
            {
                StatusText = "First Time Setup";
                await Task.Delay(200);

                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var dialog = _serviceProvider.GetRequiredService<FirstTimeView>().ShowDialog();
                });
            }

            _serviceProvider.GetRequiredService<MultiTool>().Initialize();

            StatusText = "Getting ServiceInfo";
            var bns = _serviceProvider.GetRequiredService<BnS>();
            bns.Initialize();

            StatusText = "Fetching Plugins";
            var pluginData = _serviceProvider.GetRequiredService<PluginData>();
            var plugins = await pluginData.RetrieveOnlinePlugins();
            if (plugins != null)
            {
                pluginData.Plugins = plugins;
                if (_settings.System.AUTO_UPDATE_PLUGINS)
                {
                    StatusText = "Checking plugins";
                    await pluginData.UpdateInstalledPlugins();
                }
            }

            StatusText = "Initializing...";
            await _serviceProvider.GetRequiredService<MainViewModel>().InitializeAsync();

            if (!Directory.Exists("modpolice"))
                Directory.CreateDirectory("modpolice");

            // Load multitool_qol xml & extended_options xml
            await _serviceProvider.GetRequiredService<XmlModel>().InitalizeAsync();

            // Do discord auth
            if (!_settings.Sync.AUTH_KEY.IsNullOrEmpty())
            {
                StatusText = "Authorizing Discord";
                var _sync = _serviceProvider.GetRequiredService<SyncClient>();
                try
                {
                    if (_settings.Sync.EXPIRES == null)
                        throw new Exception();

                    // Tokens last for 7 days, we want to refresh the token every 2 days or so
                    var expireTime = _settings.Sync.EXPIRES - DateTime.Now;
                    if (expireTime.Value.Days < 6)
                        throw new Exception();
                    
                    await _sync.AuthDiscordAsync();
                } catch (Exception ex)
                {
                    if (!_settings.Sync.AUTH_REFRESH.IsNullOrEmpty())
                    {
                        var refresh = await _sync.DiscordRefreshToken();
                        if (refresh != null && !refresh.access_token.IsNullOrEmpty())
                        {
                            _settings.Sync.AUTH_REFRESH = refresh.refresh_token;
                            _settings.Sync.AUTH_KEY = refresh.access_token;
                            _settings.Sync.EXPIRES = DateTime.Now.AddSeconds(refresh.expires_in);
                            _sync.Auth_Token = refresh.access_token;
                            await _sync.AuthDiscordAsync();
                            await _settings.SaveAsync(CONFIG.Sync);
                        } else
                        {
                            StatusText = "New auth key needed";
                            await Task.Delay(500);
                        }
                    } else
                    {
                        StatusText = "New auth key needed";
                    }
                }
            }

            StatusText = "Finalizing...";

            // Prep 7z lib
            if (!File.Exists("7za.dll"))
                File.WriteAllBytes("7za.dll", Properties.Resources._7za);

            SevenZip.SevenZipBase.SetLibraryPath("7za.dll");

            //await Task.Delay(3500);
        }
    }
}
