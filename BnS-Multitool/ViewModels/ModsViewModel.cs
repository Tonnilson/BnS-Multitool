using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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
using System.Windows.Input;
using static SymbolicLinkSupport.SymbolicLink;

namespace BnS_Multitool.ViewModels
{
    public partial class ModsViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly BnS _bns;
        private readonly ILogger<ModsViewModel> _logger;
        private readonly MessageService _messageService;

        public class MODS_CLASS
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
        }

        public ModsViewModel(Settings settings, BnS bns, ILogger<ModsViewModel> logger, MessageService messageService)
        {
            _settings = settings;
            _bns = bns;
            _logger = logger;
            _messageService = messageService;

            CurrentLanguage = _settings.Account.LANGUAGE;
        }

        [ObservableProperty]
        private bool isSuccessStateVisibile = false;

        [ObservableProperty]
        private int modSelectedIndex = -1;

        [ObservableProperty]
        private bool isActionMessageVisible = false;

        [ObservableProperty]
        private ObservableCollection<MODS_CLASS> modListCollection = new ObservableCollection<MODS_CLASS>();

        [ObservableProperty]
        private ELanguage currentLanguage;

        [RelayCommand]
        async Task UILoaded()
        {
            // This method does seem to run asynchronously but it's still tying up the UI thread for some reason.. Run it as another task I guess..?
            await Task.Run(PrepareModList);
        }

        [RelayCommand]
        void KeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space && ModSelectedIndex != -1)
            {
                CheckOrUnCheckEntry();
                e.Handled = true;
            }
        }

        [RelayCommand]
        void MouseDoubleClick()
        {
            if (ModSelectedIndex != -1)
                CheckOrUnCheckEntry();
        }

        [RelayCommand]
        async Task RefreshModList() =>
            await PrepareModList();

        [RelayCommand]
        void ApplyMods()
        {
            IsActionMessageVisible = false;
            try
            {
                foreach (var mod in ModListCollection)
                {
                    string modSource = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods", mod.Name);
                    string modDestination = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Mods", mod.Name);

                    if (mod == null) continue;
                    if (mod.isChecked)
                    {
                        if (Directory.Exists(modSource))
                            if (!Directory.Exists(modDestination))
                                CreateDirectoryLink(modDestination, modSource);
                    }
                    else
                    {
                        if (Directory.Exists(modDestination))
                            Directory.Delete(modDestination);
                    }
                }
                IsActionMessageVisible = true;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying mods");
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "There was an error applying mods, more details in log file", IsError = true, UseBold = true });
            }
        }

        [RelayCommand]
        void OpenModFolder() => Process.Start(new ProcessStartInfo()
        {
            FileName = Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods"),
            UseShellExecute = true,
            Verb = "open"
        });

        void CheckOrUnCheckEntry()
        {
            var selectedIndex = ModSelectedIndex;
            var mod = ModListCollection[ModSelectedIndex];
            if (mod != null)
            {
                if (Directory.GetFiles(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods", mod.Name), "*.upk").Length != 0)
                {
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = $"{mod.Name} contains files meant for UE3 and not UE4", IsError = true });
                    mod.isChecked = false;
                } else
                    mod.isChecked = !mod.isChecked;

                // There is no event raised when an item in the collection is modified so we need to refresh the view
                // Probably a better way to do this but invoking OnPropertyChanged didn't seem to work, if you know a better way with communitytoolkit.mvvm lmk
                CollectionViewSource.GetDefaultView(ModListCollection).Refresh();
            }
        }

        [RelayCommand]
        void CheckOrUncheckAll(string state)
        {
            bool bState = state == "true" ? true : false;
            foreach(var mod in ModListCollection)
            {
                if (mod == null) continue;

                if (Directory.GetFiles(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods", mod.Name), "*.upk").Length != 0)
                    mod.isChecked = false;
                else
                    mod.isChecked = bState;
            }
            CollectionViewSource.GetDefaultView(ModListCollection).Refresh();
        }

        private async Task PrepareModList()
        {
            IsActionMessageVisible = false;
            if (!Directory.Exists(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods"));

            if (!Directory.Exists(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Mods")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Mods"));

            string[] AVAILABLE_MODS = Directory.GetDirectories(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Mods"));
            string[] INSTALLED_MODS = Directory.GetDirectories(Path.Combine(_settings.System.BNS_DIR, "BNSR", "Content", "Paks", "Mods"));

            try
            {
                // Cleanup bad links
                foreach (var mod in INSTALLED_MODS)
                {
                    DirectoryInfo di = new DirectoryInfo(mod);
                    if (di.IsSymbolicLink() && !di.IsSymbolicLinkValid())
                        di.Delete(true);
                }
            } catch (IOException ex)
            {
                _logger.LogError(ex, "Symbolic Link Error");
            }

            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ModListCollection.Clear();
            }));

            foreach (string directory in AVAILABLE_MODS)
            {
                bool isEnabled = false;
                FileInfo fileInfo = new FileInfo(directory);

                foreach (var mod in INSTALLED_MODS)
                {
                    FileInfo mod_folder = new FileInfo(mod);
                    if (mod_folder.Name == fileInfo.Name)
                    {
                        isEnabled = true;
                        break;
                    }
                }
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ModListCollection.Add(new MODS_CLASS() { Name = fileInfo.Name, isChecked = isEnabled });
                }));
            }

            await Task.CompletedTask;
        }

        partial void OnCurrentLanguageChanged(ELanguage value)
        {
            _settings.Account.LANGUAGE = value;
            _settings.Save(Settings.CONFIG.Account);
        }
    }
}
