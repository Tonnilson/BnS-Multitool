using BnS_Multitool.Extensions;
using BnS_Multitool.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static BnS_Multitool.Functions.Crypto;
using static BnS_Multitool.Functions.IO;

namespace BnS_Multitool.Models
{
    public class PluginData
    {
        private readonly httpClient _httpClient;
        private readonly BnS _bns;
        private readonly Settings _settings;
        private readonly ILogger<PluginData> _logger;
        private readonly MessageService _messageService;
        private readonly XmlModel _xml;

        public class PluginInfo
        {
            public string Name { get; set; }
            public string Author { get; set; }
            public string Title { get; set; }
            public string Hash { get; set; }
            public string FullName { get; set; }
            public string MEGA { get; set; }
            public string FilePath { get; set; }
            public long Date { get; set; }
            public string Description { get; set; }
            public System.Windows.Media.Brush FontColor { get; set; }
            public string DateLocal { get; set; }
            public string[] Regions { get; set; }
            public bool Hide { get; set; }
            public bool Deprecated { get; set; }
        }

        public class AvailablePlugins
        {
            public List<PluginInfo> PluginInfo { get; set; }
        }

        public DateTime _lastLookup { get; set; } = DateTime.Now;
        public AvailablePlugins? Plugins = null;

        public PluginData(httpClient httpClient, BnS bns, Settings settings, ILogger<PluginData> logger, MessageService messageService, XmlModel xmlModel)
        {
            _httpClient = httpClient;
            _bns = bns;
            _settings = settings;
            _logger = logger;
            _messageService = messageService;
            _xml = xmlModel;
        }

       public async Task<AvailablePlugins?> RetrieveOnlinePlugins()
       {
            try
            {
                if (DateTime.Now <= _lastLookup && Plugins != null) return Plugins;
                _lastLookup = DateTime.Now.AddMinutes(1);
                AvailablePlugins result = await _httpClient.DownloadJson<AvailablePlugins>($"{Properties.Resources.MainServerAddr}plugins/plugins_v3.json");
                return result;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get plugin data from server");
                if (Plugins != null)
                    return Plugins;
                else
                    return null;
            }
        }

        public bool IsPluginInstalled(string pluginName)
        {
            var plugin = Plugins.PluginInfo.FirstOrDefault(x => x.Name == pluginName);
            if (plugin == null) return false;

            string luginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));
            return File.Exists(luginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath)));
        }

        public async Task<bool> InstallPlugin(PluginInfo plugin)
        {
            var pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));

            if (File.Exists(pluginPath))
            {
                if (CRC32_File(pluginPath) == plugin.Hash) return true;
                File.Delete(pluginPath);
            }

            try
            {
                string url = $"{Properties.Resources.MainServerAddr}plugins/{plugin.FullName}";
                string download_path = Path.GetFullPath(Path.Combine("modpolice", plugin.FullName));
                bool downloadResult = await _httpClient.DownloadFileAsync(url, download_path);
                if (!downloadResult)
                {
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Failed to install plugin\r\rReason:\rFailed download, more info in logs", IsError = true, UseBold = false });
                    throw new Exception("Failed to download plugin");
                }

                var contentLength = await _httpClient.RemoteFileSize(url);

                FileInfo fileInfo = new FileInfo(download_path);
                if (fileInfo.Length < contentLength)
                {
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Failed to install plugin\r\rReason:\rContent length mismatch, possibily caused by Anti-Virus", IsError = true, UseBold = false });
                    throw new Exception("Content size does not match, did a antivirus stop this?");
                }

                ExtractZipFileToDirectory(download_path, _settings.System.BNS_DIR, true);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install plugin");
                return false;
            }
            return true;
        }

        public async Task UpdateInstalledPlugins()
        {
            var plugins = await RetrieveOnlinePlugins();
            if (plugins != null)
                Plugins = plugins;

            if (Plugins == null)
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Failed to update installed plugins, error with reaching server?", IsError = true, UseBold = false });
                await Task.CompletedTask; return;
            }

            foreach(var plugin in Plugins.PluginInfo)
            {
                if (plugin == null || plugin.Name.IsNullOrEmpty()) continue;

                string pluginPath = Path.GetFullPath(Path.Combine(_settings.System.BNS_DIR, plugin.FilePath));

                // Special condition for multitool_qol since I moved it into the normal plugin system
                if (plugin.Name == "multitool_qol")
                {
                    if (_settings.Account.AUTPATCH_QOL == 1) continue; // Skip

                    if (File.Exists(pluginPath))
                    {
                        if (CRC32_File(pluginPath) != plugin.Hash)
                            goto Install;
                        continue;
                    }

                Install:
                    await InstallPlugin(plugin);
                }

                // Plugin isn't "installed" so why continue
                if (!File.Exists(pluginPath)) continue;

                // Plugin is no longer supported / discontinued so auto-remove
                if (plugin.Deprecated)
                {
                    if (File.Exists(pluginPath))
                        File.Delete(pluginPath);

                    continue;
                }

                if (CRC32_File(pluginPath) != plugin.Hash)
                    await InstallPlugin(plugin);
            }

            await Task.CompletedTask;
        }
    }
}
