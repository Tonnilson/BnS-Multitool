using BnS_Multitool.Extensions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
using System.Xml.Linq;

namespace BnS_Multitool.ViewModels
{
    public partial class PatchesViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly MultiTool _multiTool;
        private readonly SyncClient _sync;
        private readonly MessageService _messageService;
        private readonly ILogger<PatchesViewModel> _logger;

        public class XML_ITEM
        {
            private bool _isEnabled = false;
            public string Title { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public bool IsOnSync { get; set; }
            public bool IsEnabled
            {
                get { return this._isEnabled; }
                set
                {
                    this.FontColor = value ? System.Windows.Media.Brushes.CornflowerBlue : System.Windows.Media.Brushes.AliceBlue;
                    this._isEnabled = value;
                }
            }
            public System.Windows.Media.Brush FontColor { get; set; } = System.Windows.Media.Brushes.AliceBlue;
        }

        public PatchesViewModel(Settings settings, MultiTool multiTool, SyncClient sync, MessageService messageService, ILogger<PatchesViewModel> logger)
        {
            _settings = settings;
            _multiTool = multiTool;
            _sync = sync;
            _messageService = messageService;
            _logger = logger;

            if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager"));

            if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync"));

            if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "addons")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "addons"));

            if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches")))
                Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "patches"));

            BNSPatchPath = _settings.System.BNSPATCH_DIRECTORY;
        }

        [ObservableProperty]
        private bool showStatus = false;

        [ObservableProperty]
        private bool showBNSPatchWindow = false;

        [ObservableProperty]
        private string bNSPatchPath;

        [ObservableProperty]
        private XML_ITEM currentXmlItem;

        [ObservableProperty]
        private int currentXmlIndex;

        [ObservableProperty]
        private ObservableCollection<XML_ITEM> xmlCollection = new ObservableCollection<XML_ITEM>();

        [RelayCommand]
        async void UILoaded() => await Task.Run(UpdateList);

        private bool isBNSPatchXML(string file, bool ShowError = false)
        {
            if (file.EndsWith(".patch"))
                return false;

            try
            {
                XDocument xmlFile = XDocument.Parse(File.ReadAllText(file)); // Allows us to load all text files regardless of encoding versus below
                //XDocument xmlFile = XDocument.Load(file, LoadOptions.None);
                return xmlFile.Root.Name == "patches";
            }
            catch (Exception ex)
            {
                if (ShowError)
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = $"XML files should start with <?xml and not whitespaces or comments, fix the file format or delete it.\r\r{file}", IsError = true, UseBold = false });

                return false;
            }
        }

        async Task UpdateList()
        {
            ShowStatus = false;
            string MANAGER_DIR = Path.Combine(BNSPatchPath, "manager");
            string PATCHES_DIR = Path.Combine(BNSPatchPath, "patches");
            string ADDONS_DIR = Path.Combine(BNSPatchPath, "addons");

            try
            {
                List<XML_ITEM> XmlList = new List<XML_ITEM>();
                foreach (var file in Directory.EnumerateFiles(MANAGER_DIR, "*.*", SearchOption.AllDirectories).
                    Where(x => x.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith("patch", StringComparison.InvariantCultureIgnoreCase)).
                    OrderBy(x => Path.GetFileName(x)))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string fileExt = Path.GetExtension(file);
                    string directory = Path.GetDirectoryName(file);
                    string DisplayName = fileName;

                    XML_ITEM item = new XML_ITEM();
                    item.FilePath = file;
                    
                    // Check if the file is in the sync folder, is so check to see if it is apart of our synced list
                    if (directory.Contains("\\sync\\"))
                    {
                        //We need to make sure the user synced with this file
                        if (_settings.Sync.Synced == null || !_settings.Sync.Synced.Any(x => x.Name == fileName && x.Type == fileExt.Replace(".", ""))) continue;
                        var xml = _settings.Sync.Synced.First(x => x.Name == fileName);
                        DisplayName = await _sync.Base64UrlDecodeAsync(xml.Title); //If everything is fine set the Display Name
                        item.IsOnSync = true;
                        item.Category = xml.Category.GetDescription();
                    }

                    // Make sure the file isn't duplicated?
                    if (XmlList.Any(x => Path.GetFileName(x.FilePath) == Path.GetFileName(file)))
                        continue;

                    var IsPatch = isBNSPatchXML(file);
                    item.Type = IsPatch ? "xml" : "patch";
                    item.Title = DisplayName;

                    string pathCheck = IsPatch ? PATCHES_DIR : ADDONS_DIR;
                    item.IsEnabled = File.Exists(Path.Combine(pathCheck, Path.GetFileName(file)));
                    //item.FontColor = item.IsEnabled ? System.Windows.Media.Brushes.CornflowerBlue : System.Windows.Media.Brushes.AliceBlue;

                    XmlList.Add(item);
                }

                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    XmlCollection = new ObservableCollection<XML_ITEM>(XmlList.OrderBy(x => x.Title).ToList());
                    CurrentXmlIndex = -1;
                }));
            } catch (Exception ex)
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "A error occured, this can be related to either your anti-virus or OneDrive, if you use OneDrive consider changing BNSPatch Settings.", IsError = true });
                _logger.LogError(ex, "An error occured reading files for Patches tab");
            }
        }

        [RelayCommand]
        void KeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space && CurrentXmlIndex != -1)
            {
                CurrentXmlItem.IsEnabled = !CurrentXmlItem.IsEnabled;
                CollectionViewSource.GetDefaultView(XmlCollection).Refresh();
                e.Handled = true;
            }
        }

        [RelayCommand]
        void MouseDoubleClick()
        {
            if (CurrentXmlIndex == -1) return;
            CurrentXmlItem.IsEnabled = !CurrentXmlItem.IsEnabled;
            CollectionViewSource.GetDefaultView(XmlCollection).Refresh();
        }

        [RelayCommand]
        void ApplyPatches()
        {
            ShowStatus = false;
            string PATCHES_DIR = Path.Combine(BNSPatchPath, "patches");
            string ADDONS_DIR = Path.Combine(BNSPatchPath, "addons");

            try
            {
                foreach (var xml in XmlCollection)
                {
                    string directory = xml.Type == "xml" ? PATCHES_DIR : ADDONS_DIR;
                    if (xml.IsEnabled)
                    {
                        if (!File.Exists(Path.GetFullPath(Path.Combine(directory, Path.GetFileName(xml.FilePath)))))
                            if (File.Exists(Path.GetFullPath(xml.FilePath)))
                                SymbolicLinkSupport.SymbolicLink.CreateFileLink(Path.GetFullPath(Path.Combine(directory, Path.GetFileName(xml.FilePath))), Path.GetFullPath(xml.FilePath));
                    }
                    else
                        if (File.Exists(Path.GetFullPath(Path.Combine(directory, Path.GetFileName(xml.FilePath)))))
                        File.Delete(Path.GetFullPath(Path.Combine(directory, Path.GetFileName(xml.FilePath))));
                }
                ShowStatus = true;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying patches");
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "There was an error applying one of the patches, check the logs for more information", IsError = true, UseBold = true });
            }
        }

        [RelayCommand]
        void OpenBNSPatch()
        {
            BNSPatchPath = _settings.System.BNSPATCH_DIRECTORY;
            ShowBNSPatchWindow = true;
        }

        [RelayCommand]
        void CloseBNSPatch()
        {
            if (BNSPatchPath != _settings.System.BNSPATCH_DIRECTORY)
                BNSPatchPath = _settings.System.BNSPATCH_DIRECTORY;

            ShowBNSPatchWindow = false;
        }

        [RelayCommand]
        async void ApplyBNSPatch()
        {
            if (BNSPatchPath != _settings.System.BNSPATCH_DIRECTORY)
            {
                _settings.System.BNSPATCH_DIRECTORY = BNSPatchPath;
                await _settings.SaveAsync(Settings.CONFIG.Settings);
            }
            ShowBNSPatchWindow = false;
        }

        [RelayCommand]
        void Browse()
        {
            using (var FOLDER = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult RESULT = FOLDER.ShowDialog();

                if (RESULT == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(FOLDER.SelectedPath))
                    BNSPatchPath = FOLDER.SelectedPath + ((FOLDER.SelectedPath.Last() != '\\') ? "\\" : "");
            }
        }

        [RelayCommand]
        void OpenManager() => Process.Start(new ProcessStartInfo()
        {
            FileName = Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager"),
            UseShellExecute = true,
            Verb = "open"
        });

        [RelayCommand]
        async void RefreshPatches() => await UpdateList();

        [RelayCommand]
        void OpenSync() =>
            WeakReferenceMessenger.Default.Send(new NavigateMessage("Sync"));
    }
}
