using BnS_Multitool.Extensions;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.__Internals;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using static BnS_Multitool.Functions.Crypto;
using static BnS_Multitool.Models.PluginData;

namespace BnS_Multitool.ViewModels
{
    public partial class PluginsViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly BnS _bns;
        private readonly ILogger<PluginsViewModel> _logger;
        private readonly PluginData _plugins;
        private readonly MessageService _messageService;

        public PluginsViewModel(Settings settings, BnS bns, ILogger<PluginsViewModel> logger, PluginData plugins, MessageService messageService)
        {
            _settings = settings;
            _bns = bns;
            _logger = logger;
            _plugins = plugins;
            isAutoUpdate = _settings.System.AUTO_UPDATE_PLUGINS;
            _messageService = messageService;
        }

        [ObservableProperty]
        private bool isPluginInfoVisible = false;

        [ObservableProperty]
        private bool isAutoUpdate;

        [ObservableProperty]
        private ObservableCollection<PluginData.PluginInfo> pluginViewCollection = new ObservableCollection<PluginData.PluginInfo>();

        [ObservableProperty]
        private int pluginSelectedIndex = -1;

        [ObservableProperty]
        private string pluginAuthor;

        [ObservableProperty]
        private string pluginDescription;

        [ObservableProperty]
        private string pluginActionText = "Install";

        [ObservableProperty]
        private bool isRemoveBtnVisible = false;

        [RelayCommand]
        async Task UILoaded()
        {
            // This method does seem to run asynchronously but it's still tying up the UI thread for some reason.. Run it as another task I guess..?
            PluginViewCollection.Clear();
            await Task.Run(PageLoaded);
        }

        private async Task PageLoaded()
        {
            _plugins.Plugins = await _plugins.RetrieveOnlinePlugins();

            if (_plugins.Plugins == null)
                return;

            foreach(var plugin in _plugins.Plugins.PluginInfo)
            {
                // Check if our region is not allowed to see this plugin
                if (plugin.Regions != null && plugin.Regions.Count() > 0 && plugin.Regions.Contains(_settings.Account.REGION.GetDescription())) continue;

                // Do not display plugins with an invalid filename, this is for when I add new plugins and the server hasn't retrieved info about it yet
                if (plugin.FullName.IsNullOrEmpty()) continue;

                // Something new cause I wanted to incorperate multitool_qol in without it showing
                if (plugin.Hide) continue;

                var pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
                if (File.Exists(pluginPath))
                {
                    if (CRC32_File(pluginPath) == plugin.Hash)
                        plugin.FontColor = Brushes.CornflowerBlue;
                    else
                        plugin.FontColor = Brushes.Yellow;
                }
                else
                    plugin.FontColor = Brushes.White;

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PluginViewCollection.Add(new PluginInfo
                    {
                        Name = plugin.Name,
                        Author = plugin.Author,
                        Title = plugin.Title,
                        Hash = plugin.Hash,
                        FontColor = plugin.FontColor,
                        FilePath = plugin.FilePath,
                        FullName = plugin.FullName,
                        Date = plugin.Date,
                        Description = plugin.Description,
                        DateLocal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(plugin.Date).ToLocalTime().ToString("g")
                    });
                }));
            }
        }

        [RelayCommand]
        void OpenBinLocation() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { 
            FileName = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Binaries", "Win64"),
            UseShellExecute = true,
            Verb = "open"
        });

        [RelayCommand]
        async Task PluginAction()
        {
            if (PluginSelectedIndex == -1) return;
            var plugin = PluginViewCollection[PluginSelectedIndex];
            if (plugin == null) return;

            string pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
            try
            {
                if (await _plugins.InstallPlugin(plugin))
                {
                    // Special condition for bnspatch
                    if (plugin.Name == "bnspatch")
                    {
                        if (!File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml")))
                        {
                            await File.WriteAllTextAsync(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml"), Properties.Resources.patches);
                            if (!File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml")))
                                throw new Exception("Failed to create patches.xml");
                        }
                    }

                    if (File.Exists(pluginPath))
                    {
                        if (CRC32_File(pluginPath) == plugin.Hash)
                        {
                            plugin.FontColor = Brushes.CornflowerBlue;
                            PluginActionText = "Update";
                            IsRemoveBtnVisible = true;
                        }
                        else
                        {
                            plugin.FontColor = Brushes.Orange;
                            throw new Exception($"Hash check failed for {Path.GetFileName(pluginPath)}");
                        }

                        CollectionViewSource.GetDefaultView(PluginViewCollection).Refresh(); // MVVM confuses me and it doesn't help that I am using a source generator so this is a work-around for updating the view..?
                    }
                    else
                        throw new Exception($"{pluginPath} was not found");
                } else
                    throw new Exception($"Failed to download {plugin.Title}");
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Plugins::PluginActionCommand");
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = $"Failed to install plugin\r\rReason:\r{ex.Message}", IsError = true, UseBold = false });
            }
        }

        [RelayCommand]
        void PluginRemove()
        {
            if (PluginSelectedIndex == -1) return;
            var plugin = PluginViewCollection[PluginSelectedIndex];
            if (plugin == null) return;

            string pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
            if (File.Exists(pluginPath))
                File.Delete(pluginPath);

            // Special condition
            if (plugin.Name == "bnspatch")
            {
                if (File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml")))
                    File.Delete(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches.xml"));
            }

            plugin.FontColor = Brushes.White;
            CollectionViewSource.GetDefaultView(PluginViewCollection).Refresh(); // MVVM confuses me and it doesn't help that I am using a source generator so this is a work-around for updating the view..?
            IsPluginInfoVisible = false;
            PluginSelectedIndex = -1;
        }

        partial void OnIsAutoUpdateChanged(bool value)
        {
            _settings.System.AUTO_UPDATE_PLUGINS = value;
            _settings.Save(Settings.CONFIG.Settings);
        }

        partial void OnPluginSelectedIndexChanged(int value)
        {
            if (value == -1)
            {
                IsPluginInfoVisible = false;
                return;
            }

            var plugin = PluginViewCollection[value];
            if (plugin != null)
            {
                var pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
                if (File.Exists(pluginPath))
                {
                    PluginActionText = "Update";
                    IsRemoveBtnVisible = true;
                } else
                {
                    PluginActionText = "Install";
                    IsRemoveBtnVisible = false;
                }

                PluginAuthor = $"Author: {plugin.Author}";
                PluginDescription = plugin.Description;
                IsPluginInfoVisible = true;
            }
        }
    }
}
