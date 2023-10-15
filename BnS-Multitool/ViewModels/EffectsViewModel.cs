using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using static BnS_Multitool.Functions.IO;

namespace BnS_Multitool.ViewModels
{
    public partial class EffectsViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly MessageService _messageService;
        private ILogger<EffectsViewModel> _logger;
        private PluginData _pluginData;

        public class CLASS_ITEM
        {
            public string ClassName { get; set; }
            public bool AnimationChecked { get; set; } = true;
            public bool EffectsChecked { get; set; } = true;
            public Settings.BNS_CLASS_STRUCT Data { get; set; }
        }

        public EffectsViewModel(Settings settings, MessageService messageService, ILogger<EffectsViewModel> logger, PluginData pluginData)
        {
            _settings = settings;
            _messageService = messageService;
            _logger = logger;
            _pluginData = pluginData;

            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Blade Master", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "blademaster") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Kung Fu Master", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "kungfumaster") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Force Master", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "forcemaster") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Destroyer", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "destroyer") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Assassin", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "assassin") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Summoner", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "summoner") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Blade Dancer", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "bladedancer") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Warlock", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "warlock") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Soul Fighter", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "soulfighter") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Gunslinger", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "gunslinger") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Warden", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "warden") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Archer", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "archer") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Astromancer", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "astromancer") });
            ClassToggleCollection.Add(new CLASS_ITEM { ClassName = "Dual Blade", Data = _settings.System.CLASS.First(x => x.CLASS.ToLower() == "dualblade") });
        }

        [ObservableProperty]
        private ObservableCollection<CLASS_ITEM> classToggleCollection = new ObservableCollection<CLASS_ITEM>();

        [ObservableProperty]
        private bool extendedOptionsInstalled = false;

        private void ExtractPakFiles()
        {
            try
            {
                if (Directory.Exists(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Removes")))
                    Directory.Delete(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Removes"), true);

                using (var ms = new MemoryStream(Properties.Resources.class_removes))
                    ExtractZipFileToDirectory(ms, Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Removes"), true, true);

                Directory.CreateDirectory(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Removes"));
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract contents of class removes");
            }
        }

        [RelayCommand]
        void UILoaded() 
        {
            try
            {
                ExtendedOptionsInstalled = File.Exists(Path.Combine(_settings.System.BNS_DIR, _pluginData.Plugins?.PluginInfo.First(x => x.Name == "extendedoptions").FilePath));
                if (!Directory.Exists(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content"))) return;
                string removalDirectory = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Removes");
                string removalPath = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Removes");

                // Do a check here if removalDirectory exists, if not create & extract
                if (!Directory.Exists(removalPath)) Directory.CreateDirectory(removalPath);
                if (!Directory.Exists(removalDirectory))
                {
                    Directory.CreateDirectory(removalDirectory);
                    ExtractPakFiles();
                }

                if (Directory.GetFiles(removalDirectory).Length == 0)
                    ExtractPakFiles();

                foreach (var item in ClassToggleCollection)
                {
                    item.AnimationChecked = !item.Data.ANIMATIONS.Any(file => File.Exists(Path.Combine(removalPath, file)));
                    item.EffectsChecked = !item.Data.EFFECTS.Any(file => File.Exists(Path.Combine(removalPath, file)));
                }

                CollectionViewSource.GetDefaultView(ClassToggleCollection).Refresh();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error while loading UI");
            }
        }

        [RelayCommand]
        void HandleToggles(string param)
        {
            foreach (var item in ClassToggleCollection)
            {
                if (param.StartsWith("all"))
                {
                    if (param.EndsWith("on"))
                    {
                        item.AnimationChecked = true;
                        item.EffectsChecked = true;
                    } else
                    {
                        item.AnimationChecked = false;
                        item.EffectsChecked = false;
                    }
                }
                else
                {
                    if (param.EndsWith("on"))
                    {
                        if (param.StartsWith("fx"))
                            item.EffectsChecked = true;
                        else
                            item.AnimationChecked = true;
                    } else
                    {
                        if (param.StartsWith("fx"))
                            item.EffectsChecked = false;
                        else
                            item.AnimationChecked = false;
                    }
                }
            }

            CollectionViewSource.GetDefaultView(ClassToggleCollection).Refresh();
        }

        [RelayCommand]
        void ApplyChanges()
        {
            string removalDirectory = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Removes");
            string removalPath = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Removes");

            try
            {
                foreach (var item in ClassToggleCollection)
                {
                    if (item.AnimationChecked)
                    {
                        item.Data.ANIMATIONS.ToList().ForEach(animation =>
                        {
                            if (File.Exists(Path.Combine(removalPath, animation)))
                                File.Delete(Path.Combine(removalPath, animation));

                            if (File.Exists(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(animation)}.sig")))
                                File.Delete(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(animation)}.sig"));
                        });
                    }
                    else
                    {
                        item.Data.ANIMATIONS.ToList().ForEach(animation =>
                        {
                            if (!File.Exists(Path.Combine(removalPath, animation)))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.Combine(removalPath, animation), Path.Combine(removalDirectory, animation));

                            if (!File.Exists(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(animation)}.sig")))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(animation)}.sig"), Path.Combine(removalDirectory, $"{Path.GetFileNameWithoutExtension(animation)}.sig"));
                        });
                    }

                    if (item.EffectsChecked)
                    {
                        item.Data.EFFECTS.ToList().ForEach(effect =>
                        {
                            if (File.Exists(Path.Combine(removalPath, effect)))
                                File.Delete(Path.Combine(removalPath, effect));

                            if (File.Exists(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(effect)}.sig")))
                                File.Delete(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(effect)}.sig"));
                        });
                    }
                    else
                    {
                        item.Data.EFFECTS.ToList().ForEach(effect =>
                        {
                            if (!File.Exists(Path.Combine(removalPath, effect)))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.Combine(removalPath, effect), Path.Combine(removalDirectory, effect));

                            if (!File.Exists(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(effect)}.sig")))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.Combine(removalPath, $"{Path.GetFileNameWithoutExtension(effect)}.sig"), Path.Combine(removalDirectory, $"{Path.GetFileNameWithoutExtension(effect)}.sig"));
                        });
                    }
                }
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Settings applied!", UseBold = true, IsError = false });
                UILoaded();
            }
            catch (Exception ex)
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "There was an error trying to apply the changes, check the logs for more details.", IsError = true, UseBold = false });
                _logger.LogError(ex, "error applying effects and animations");
            }
        }

        [RelayCommand]
        void NavigateExtendedOptions() => WeakReferenceMessenger.Default.Send(new NavigateMessage("ExtendedOptions"));
    }
}
