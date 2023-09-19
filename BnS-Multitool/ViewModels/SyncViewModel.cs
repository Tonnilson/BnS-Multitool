using BnS_Multitool.Extensions;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WatsonWebsocket;
using BnS_Multitool.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using static BnS_Multitool.SyncData;
using static BnS_Multitool.Functions.Crypto;
using System.IO;
using System.Windows.Data;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.Messaging;
using BnS_Multitool.Messages;

namespace BnS_Multitool.ViewModels
{
    public partial class SyncViewModel : ObservableValidator
    {
        private readonly Settings _settings;
        private readonly SyncClient _sync;
        private readonly httpClient _httpClient;
        private readonly MessageService _messageService;
        private readonly ILogger<SyncViewModel> _logger;

        private readonly WatsonWsServer _watsonWsServer = new WatsonWsServer("localhost", 42069, false);
        private TaskCompletionSource<bool> AuthorizationCompleted = new TaskCompletionSource<bool>();
        private List<XML_VIEW_RESPONSE> _Downloaded_XMLS = new List<XML_VIEW_RESPONSE>();
        private bool _firstTime = false;
        private DateTime _lastRefresh = DateTime.Now;

        public SyncViewModel(Settings settings, SyncClient sync, httpClient httpClient, ILogger<SyncViewModel> logger, MessageService messageService)
        {
            _settings = settings;
            _sync = sync;
            _httpClient = httpClient;
            _logger = logger;
            _messageService = messageService;

            _watsonWsServer.MessageReceived += WatsonWsServer_MessageRececived;
        }

        private void WatsonWsServer_MessageRececived(object? sender, MessageReceivedEventArgs e)
        {
            try
            {
                var packet = JsonConvert.DeserializeObject<SyncData.SocketReceivedData>(Encoding.UTF8.GetString(e.Data));
                if (packet == null) throw new Exception();
                _settings.Sync.AUTH_REFRESH = packet.refresh_token;
                _settings.Sync.AUTH_KEY = packet.access_token;
                _settings.Save(Settings.CONFIG.Sync);
                AuthorizationCompleted.SetResult(true);

                // Let the main window know to pull it's self back up to the foreground.
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    WeakReferenceMessenger.Default.Send(new ActivateWindowMessage(true));
                }));
                //MainWindow.mainWindow.Activate();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in websocket message receiver");
            }
            finally
            {
                _watsonWsServer.Stop();
            }
        }

        private bool SkipAuth = false;

        [ObservableProperty]
        private bool showPublishView = false;

        [ObservableProperty]
        private bool showSyncMain = false;

        [ObservableProperty]
        private bool showSyncAuth = false;

        [ObservableProperty]
        private bool showProgressView = false;

        [ObservableProperty]
        private bool showResyncBtn = false;

        [ObservableProperty]
        private string progressStatusText;

        [ObservableProperty]
        private string authToken;

        [ObservableProperty]
        private string discordUsername;

        [ObservableProperty]
        private string discordAvatar;

        [ObservableProperty]
        private int categoryFilterIndex = 0;

        [ObservableProperty]
        private int sortByIndex = 0;

        [ObservableProperty]
        private ObservableCollection<SyncData.XML_View_List> xmlCollection = new ObservableCollection<SyncData.XML_View_List>();

        [ObservableProperty]
        private XML_View_List xmlSelectedItem;

        [ObservableProperty]
        private bool showPublishBtn = false;

        [RelayCommand]
        void UILoaded()
        {
            // Hacky trick to not lock the UI thread, Even if I run it with Task.Run it locks, not sure why, if you know why let me know.
            new System.Threading.Thread(async () => { System.Threading.Thread.CurrentThread.IsBackground = true; await OnPageLoad(); }).Start();
        }

        private async Task OnPageLoad()
        {
            if (!_sync.Authorized && !_settings.Sync.AUTH_KEY.IsNullOrEmpty()) await AuthorizeDiscord();
            if (!_sync.Authorized)
                await SetupAuthKey();
            else
            {
                if (DiscordUsername.IsNullOrEmpty())
                    DiscordUsername = _sync.Discord.username;

                if (DiscordAvatar.IsNullOrEmpty() && !_sync.Discord.avatar.IsNullOrEmpty())
                    DiscordAvatar = string.Format("{0}{1}/{2}.png?size=128", _sync.Avatar_url, _sync.Discord.id, _sync.Discord.avatar);
            }

            ShowSyncMain = true;
            ShowSyncAuth = false;
            
            CategoryFilterIndex = 0;

            if (_sync.Authorized)
                ShowPublishBtn = true;

            if (!_firstTime)
            {
                _firstTime = true;
                await RefreshAllXmls(true);
            } else
                await RefreshAllXmls();
        }

        private async Task RefreshAllXmls(bool bForced = false, bool bRefresh = false)
        {
            var category = CategoryFilterIndex - 1;
            if (!bForced && DateTime.Now <= _lastRefresh && !bRefresh) return;
            
            try
            {
                XMLS_RESPONSE? availableXMLs = null;
                if (!bForced && bRefresh) goto RebuildList;
                NameValueCollection nvc = new NameValueCollection();

                if (_sync.Discord != null && sortByIndex == 3)
                    nvc.Add("uid", _sync.Discord.id.ToString());
                else
                    nvc.Add("q", category == -1 ? "all" : category.ToString());

                string responseData = await _sync.Fetch_XMLS(nvc);
                if (responseData.IsNullOrEmpty())
                {
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = "There was an error reaching the host: sync.bns.tools", IsError = true, UseBold = true });
                    return;
                }

                availableXMLs = JsonConvert.DeserializeObject<XMLS_RESPONSE>(responseData);
                if (availableXMLs?.response != HttpStatusCode.OK)
                {
                    _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Error reaching the database, try again later?", IsError = true, UseBold = true });
                    return;
                }

                ProgressStatusText = "Retrieving from server...";
                ShowProgressView = true;
                RebuildList:
                await Application.Current.Dispatcher.BeginInvoke(new Action(XmlCollection.Clear));

                if (!bRefresh)
                    _Downloaded_XMLS = availableXMLs.xmls;

                var sortedXmls = SortXmlsBy();

                foreach (var xml in sortedXmls)
                {
                    Brush color;
                    string syncButtonText = '\uE1DF'.ToString();
                    bool isSynced = _settings.Sync.Synced != null && _sync.IsSynced(xml);
                    Visibility owner = Visibility.Hidden;
                    if (_sync.Discord != null && xml.Discord_id == _sync.Discord.id)
                    {
                        isSynced = false;
                        syncButtonText = '\uE104'.ToString(); // Pencil Icon Segoe UI Symbol
                        color = Brushes.Orange;
                        owner = Visibility.Visible;
                    }
                    else if (isSynced && _settings.Sync.Synced != null)
                        color = Brushes.Green;
                    else if (_settings.Sync.Synced != null && _settings.Sync.Synced.Any(x => x.ID == xml.ID) && !isSynced)
                        color = Brushes.Red;
                    else
                        color = Brushes.White;

                    await Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        XmlCollection.Add(new XML_View_List
                        {
                            ID = xml.ID,
                            Title = await _sync.Base64UrlDecodeAsync(xml.Title),
                            User = xml.Username,
                            DiscordID = xml.Discord_id,
                            Date = _sync.UnixToDateTime(long.Parse(xml.Date)).ToString("g"),
                            Type = xml.Type,
                            SyncColor = color,
                            CategoryText = xml.Category.GetDescription(),
                            Synced = !isSynced, // Remember the logic is flipped
                            Downloads = xml.Downloads,
                            Description = xml.Description,
                            SyncButton = syncButtonText,
                            FileName = xml.Name,
                            CanRemove = owner
                        });
                    }));
                }

                _lastRefresh = DateTime.Now.AddMinutes(2);
                ShowProgressView = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync view refresh failed");
            }
        }

        private async Task SetupAuthKey()
        {
            ShowSyncAuth = true;
            await AuthorizationCompleted.Task;
            if (!SkipAuth)
            {
                if (!AuthToken.IsNullOrEmpty())
                {
                    _settings.Sync.AUTH_REFRESH = AuthToken.Split(';')[1];
                    _settings.Sync.AUTH_KEY = AuthToken.Split(';')[0];
                    await _settings.SaveAsync(Settings.CONFIG.Sync);

                    // Check if the websocket server is currently running if so turn it off
                    if (_watsonWsServer.IsListening)
                        _watsonWsServer.Stop();
                }

                _sync.Auth_Token = _settings.Sync.AUTH_KEY;
                await AuthorizeDiscord();
                ShowSyncAuth = false;
                ShowSyncMain = true;
            }
        }

        private async Task AuthorizeDiscord()
        {
            ShowProgressView = true;
            ProgressStatusText = "Authorizing Discord...";

            try
            {
                if (!_settings.Sync.AUTH_KEY.IsNullOrEmpty())
                {
                    try
                    {
                        await _sync.AuthDiscordAsync();
                    } catch (Exception ex)
                    {
                        if (_settings.Sync.AUTH_REFRESH.IsNullOrEmpty())
                            throw new Exception("Failed to authorize with token, generate a new one");

                        var refresh = await _sync.DiscordRefreshToken();
                        if (refresh == null)
                            throw new Exception("Failed to authorize with token, generate a new one");

                        if (refresh.access_token.IsNullOrEmpty())
                            throw new Exception("Failed to authorize with token, generate a new one");

                        _settings.Sync.AUTH_REFRESH = refresh.refresh_token;
                        _settings.Sync.AUTH_KEY = refresh.access_token;
                        _sync.Auth_Token = refresh.access_token;
                        await _sync.AuthDiscordAsync();
                    }

                    if (_sync.Authorized)
                    {
                        ShowResyncBtn = false;
                        DiscordUsername = _sync.Discord.username;
                        if (!_sync.Discord.avatar.IsNullOrEmpty())
                            DiscordAvatar = string.Format("{0}{1}/{2}.png?size=128", _sync.Avatar_url, _sync.Discord.id, _sync.Discord.avatar);
                    } else
                        throw new Exception("Failed to authorize with token, generate a new one.");
                }
            } catch (Exception ex)
            {
                ProgressStatusText = ex.Message;
                await Task.Delay(3000);
            }
            ShowProgressView = false;
        }

        [RelayCommand]
        async void GetAuthToken()
        {
            await _watsonWsServer.StartAsync();
            Process.Start(new ProcessStartInfo(@"http://sync.bns.tools/discord_auth/?login") { UseShellExecute = true });
        }

        private List<XML_VIEW_RESPONSE> SortXmlsBy()
        {
            List<XML_VIEW_RESPONSE> orderedSelection;

            switch (SortByIndex)
            {
                case 1:
                    orderedSelection = _Downloaded_XMLS.OrderBy(x => x.Date).ToList();
                    break;
                case 2:
                    orderedSelection = _Downloaded_XMLS.OrderByDescending(x => x.Downloads).ToList();
                    break;
                case 3:
                    if (_sync.Discord != null)
                        orderedSelection = _Downloaded_XMLS.Where(x => x.Discord_id == _sync.Discord.id).ToList();
                    else
                        orderedSelection = _Downloaded_XMLS;
                    break;
                default:
                    orderedSelection = _Downloaded_XMLS.OrderByDescending(x => x.Date).ToList();
                    break;
            }

            return orderedSelection;
        }

        [RelayCommand]
        void SkipAuthorzation()
        {
            SkipAuth = true;
            ShowResyncBtn = true;
            AuthorizationCompleted.SetResult(true);
        }

        [RelayCommand]
        void UseAuthorizationToken() => AuthorizationCompleted.SetResult(true);

        [RelayCommand]
        async Task RestartSync()
        {
            ShowSyncMain = false;
            SkipAuth = false;
            ShowResyncBtn = false;
            AuthorizationCompleted = new TaskCompletionSource<bool>();
            await SetupAuthKey();
            if (!SkipAuth)
                await _settings.SaveAsync(Settings.CONFIG.Sync);
        }

        [ObservableProperty]
        private int editingXMLId;

        [ObservableProperty]
        [MaxLength(2000)]
        private string editingXML_Description;

        [ObservableProperty]
        [MinLength(3)]
        private string editingXML_Title;

        [ObservableProperty]
        private string editingXML_Ext;

        [ObservableProperty]
        private string editingXML_FileName;

        [ObservableProperty]
        private int editingXML_Category;

        [RelayCommand]
        async Task SyncFromServer(XML_View_List xml)
        {
            // Check if we're editing
            if (_sync.Discord != null && xml.DiscordID == _sync.Discord.id)
            {
                EditingXMLId = xml.ID;
                EditingXML_Description = await _sync.Base64UrlDecodeAsync(xml.Description);
                EditingXML_Title = xml.Title;
                EditingXML_Ext = xml.Type;
                EditingXML_FileName = xml.FileName;
                EditingXML_Category = xml.Category;
                OpenEditView();
                //ShowPublishWindowEdit();
                return;
            }

            ProgressStatusText = $"Syncing {xml.Title}...";
            ShowProgressView = true;

            try
            {
                string response = await _sync.Fetch_XMLAsync(xml.ID, true);
                if (response.IsNullOrEmpty()) throw new Exception("Error retrieving data from server");
                XML_VIEW_RESPONSE xmlResponse = JsonConvert.DeserializeObject<XMLS_RESPONSE>(response).xml;


                if (xml == null) throw new Exception("Error retrieving data from server");
                if (xml.Title.IsNullOrEmpty()) throw new Exception("Error retrieving data from server");

                var xmlStruct = new Settings.Synced_Xmls
                {
                    ID = xmlResponse.ID,
                    Username = xmlResponse.Username,
                    Discord_id = xmlResponse.Discord_id,
                    Date = xmlResponse.Date,
                    Title = xmlResponse.Title,
                    Type = xmlResponse.Type,
                    Category = xmlResponse.Category,
                    Hash = xmlResponse.Hash,
                    Description = xmlResponse.Description,
                    Name = xmlResponse.Name,
                    Version = xmlResponse.Version
                };

                if (_settings.Sync.Synced != null && !_settings.Sync.Synced.Any(x => x.ID == xmlResponse.ID))
                    _settings.Sync.Synced.Add(xmlStruct);
                else if (_settings.Sync.Synced == null)
                    _settings.Sync.Synced = new List<Settings.Synced_Xmls> { xmlStruct };
                else
                {
                    var synced_xml = _settings.Sync.Synced.FirstOrDefault(x => x.ID == xmlResponse.ID);
                    synced_xml = xmlStruct;
                }

                await _settings.SaveAsync(Settings.CONFIG.Sync);

                if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xmlResponse.Discord_id.ToString())))
                    Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xmlResponse.Discord_id.ToString()));

                var downloadResult = await _httpClient.DownloadFileAsync(
                    string.Format("http://sync.bns.tools/xml_data/{0}/{1}.{2}", xmlResponse.Discord_id, xmlResponse.Name, xmlResponse.Type),
                        Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xmlResponse.Discord_id.ToString(), xmlResponse.Name + "." + xmlResponse.Type), false);


                if (_sync.Discord == null)
                    xml.SyncColor = Brushes.Green;
                else if (xmlResponse.Discord_id != _sync.Discord.id)
                    xml.SyncColor = Brushes.Green;

                xml.Synced = false;

                // Invoke UI update of collection
                CollectionViewSource.GetDefaultView(XmlCollection).Refresh();
            }
            catch (Exception ex)
            {
                ProgressStatusText = ex.Message;
                await Task.Delay(3000);
            }

            ShowProgressView = false;
        }

        [RelayCommand]
        async Task DeleteFromSync(XML_View_List xml)
        {
            var result = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel("Are you sure you want to remove this from Sync? This will only remove it from the listing.", "Remove from Sync", "Remove", "Cancel");
            if (result == MessageBoxResult.OK)
            {
                // Continue
                if (_sync.Discord != null && xml.DiscordID == _sync.Discord.id)
                {
                    ProgressStatusText = "Sending delete request...";
                    ShowProgressView = true;

                    string responseData = "";
                    try
                    {
                        // POST data that we need to send, serialize it as json-format.
                        var postData = JsonConvert.SerializeObject(new SyncData.REMOVE_XML_POST
                        {
                            Auth_Code = _settings.Sync.AUTH_KEY,
                            XML_ID = xml.ID
                        });

                        // Submit a request for a security token to upload, Tokens last only 10 minutes
                        responseData = await _sync.PostDataToServer(_sync.RemoveXML_URL, postData);
                        var response = JObject.Parse(responseData);
                        //Debug.WriteLine(responseData);

                        if ((HttpStatusCode)(int)response["response"] != HttpStatusCode.OK)
                            throw new WebException((string)response["response_msg"]);

                        ProgressStatusText = $"Removed {xml.Title} from Sync";
                        await Task.Delay(1000);
                        await RefreshAllXmls(true);

                    }
                    catch (Exception ex)
                    {
                        ProgressStatusText = ex.GetType() == typeof(JsonReaderException) ? responseData : ex.Message;
                        _logger.LogError(ex, "Error deleting xml from sync");
                        await Task.Delay(5000);
                    }
                    finally
                    {
                        //await RefreshAllXmls();
                        ShowProgressView = false;
                    }
                }
            }
        }

        [RelayCommand]
        async Task UnsyncAction()
        {
            if (_settings.Sync.Synced == null) return;

            var xml = _settings.Sync.Synced.FirstOrDefault(x => x.ID == XmlSelectedItem.ID);
            if (xml == null) return;

            _settings.Sync.Synced.Remove(xml);
            await _settings.SaveAsync(Settings.CONFIG.Sync);

            var result = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel("You have unsubscribed, Do you also want to delete the file?", "Delete local file?", "Yes", "No");
            if (result == MessageBoxResult.OK)
            {
                if (File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)))
                    File.Delete(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type));
            }
            
            XmlInfoIsVisible = false;
            await RefreshAllXmls(false, true);
        }

        [RelayCommand]
        async Task ResyncAction() => await RefreshAllXmls(true);

        [ObservableProperty]
        private string xmlInfoTitle;

        [ObservableProperty]
        private string xmlInfoDescription;

        [ObservableProperty]
        private string xmlInfoAuthor;

        [ObservableProperty]
        private string xmlInfoDownloads;

        [ObservableProperty]
        private bool xmlIsSynced;

        [ObservableProperty]
        private bool xmlInfoIsVisible = false;

        private int XmlInfoID = -1;

        [RelayCommand]
        void CloseXmlInfo()
        {
            XmlInfoIsVisible = false;
        }

        [RelayCommand]
        async Task GetUpdates()
        {
            if (_settings.Sync.Synced == null) return;

            ProgressStatusText = "Getting updates...";
            ShowProgressView = true;

            await Task.Delay(500);

            foreach (var xml in _settings.Sync.Synced)
            {
                try
                {
                    string response = await _sync.Fetch_XMLAsync(xml.ID);
                    if (response.IsNullOrEmpty()) throw new Exception("response was blank");

                    // Band-aid fix for when a XML is no longer available (removed). Need to patch this on the servers side
                    if (response.Contains("\"xml\":[]")) throw new Exception($"{xml.Title} was removed from sync");

                    XML_VIEW_RESPONSE xml_data = JsonConvert.DeserializeObject<XMLS_RESPONSE>(response).xml;
                    if (!_sync.IsSynced(xml_data))
                    {
                        ProgressStatusText = $"Updating {xml.Name}";

                        if (!Directory.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString())))
                            Directory.CreateDirectory(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString()));

                        var result = await _httpClient.DownloadFileAsync(
                            string.Format("http://sync.bns.tools/xml_data/{0}/{1}.{2}", xml.Discord_id, xml.Name, xml.Type),
                            Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type),
                            false);

                        if (!result) continue;

                        // Update synced entry with data from server
                        var syncEntry = _settings.Sync.Synced.FirstOrDefault(x => x.ID == xml.ID);
                        if (syncEntry != null)
                        {
                            syncEntry.Version = xml_data.Version;
                            syncEntry.Name = xml_data.Name;
                            syncEntry.Description = xml_data.Description;
                            syncEntry.Title = xml_data.Title;
                            syncEntry.Hash = xml_data.Hash;
                            syncEntry.Date = xml_data.Date;
                            syncEntry.Username = xml_data.Username;
                        }

                        // Update view
                        var item = XmlCollection.FirstOrDefault(x => x.ID == xml.ID);
                        if (item != null)
                        {
                            item.SyncColor = Brushes.Green;
                            item.Synced = false; // logic is flipped, remember that
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating xml");
                }
            }

            ShowProgressView = false;

            // Invoke UI update of collection
            CollectionViewSource.GetDefaultView(XmlCollection).Refresh();
            await _settings.SaveAsync(Settings.CONFIG.Sync);
        }

        partial void OnXmlSelectedItemChanged(XML_View_List value)
        {
            if (value == null) return;

            XmlInfoTitle = XmlSelectedItem.Title;
            XmlInfoDescription = !XmlSelectedItem.Description.IsNullOrEmpty() ? _sync.Base64UrlDecodeAsync(XmlSelectedItem.Description).Result : string.Empty;
            XmlInfoAuthor = $"Uploaded by {XmlSelectedItem.User}";
            XmlInfoDownloads = $"Downloads: {XmlSelectedItem.Downloads:#,##0}";
            XmlInfoID = XmlSelectedItem.ID;
            XmlIsSynced = _settings.Sync.Synced.Any(x => x.ID == XmlInfoID);
            XmlInfoIsVisible = true;
        }

        // Triggered when category is changed
        partial void OnCategoryFilterIndexChanged(int value) =>
            Task.Run(async () => await RefreshAllXmls(true));

        // Triggered when sort by is changed
        partial void OnSortByIndexChanged(int value) =>
            Task.Run(async () => await OnSortChanged(value));

        private async Task OnSortChanged(int sortId)
        {
            if (sortId == 3)
                await RefreshAllXmls(true);

            var orderedSelection = SortXmlsBy();

            await Application.Current.Dispatcher.BeginInvoke(new Action(XmlCollection.Clear));
            foreach (var xml in orderedSelection)
            {
                Brush color;
                bool isSynced = _settings.Sync.Synced != null && _sync.IsSynced(xml);
                string syncButtonText = '\uE1DF'.ToString();
                Visibility owner = Visibility.Hidden;

                if (_sync.Discord != null && xml.Discord_id == _sync.Discord.id)
                {
                    isSynced = false;
                    syncButtonText = '\uE104'.ToString(); //Pencil Icon Segoe UI Symbol
                    color = Brushes.Orange;
                    owner = Visibility.Visible;
                }
                else if (isSynced && _settings.Sync.Synced != null)
                    color = Brushes.Green;
                else if (_settings.Sync.Synced != null && _settings.Sync.Synced.Any(x => x.ID == xml.ID) && !isSynced)
                    color = Brushes.Red;
                else
                    color = Brushes.White;

                await Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    XmlCollection.Add(new XML_View_List
                    {
                        ID = xml.ID,
                        Title = await _sync.Base64UrlDecodeAsync(xml.Title),
                        User = xml.Username,
                        DiscordID = xml.Discord_id,
                        Date = _sync.UnixToDateTime(long.Parse(xml.Date)).ToString("g"),
                        Type = xml.Type,
                        SyncColor = color,
                        CategoryText = xml.Category.GetDescription(),
                        Synced = !isSynced, // Remember the logic is flipped
                        Downloads = xml.Downloads,
                        Description = xml.Description,
                        FileName = xml.Name,
                        SyncButton = syncButtonText,
                        CanRemove = owner
                    });
                }));
            }
        }

        [ObservableProperty]
        private string eCharacterLimitText = "0/2000";

        [ObservableProperty]
        private string eFileLabel;

        [ObservableProperty]
        private string eDescription;

        [ObservableProperty]
        private string eTitle;

        [ObservableProperty]
        private string ePublishBtn = "Publish";

        [ObservableProperty]
        private CategoryType eCategoryItem;

        [ObservableProperty]
        private bool showPublishStatus = false;

        [ObservableProperty]
        private string publishStatusText = string.Empty;

        [RelayCommand]
        void OpenPublishView()
        {
            ETitle = string.Empty;
            EDescription = string.Empty;
            _sync.File = string.Empty;
            EFileLabel = string.Empty;
            EPublishBtn = "Publish";
            ECharacterLimitText = "0/2000";
            ShowSyncMain = false;
            ShowPublishView = true;
        }

        private void OpenEditView()
        {
            /*
             * EditingXMLId = xml.ID;
                EditingXML_Description = await _sync.Base64UrlDecodeAsync(xml.Description);
                EditingXML_Title = xml.Title;
                EditingXML_Ext = xml.Type;
                EditingXML_FileName = xml.FileName;
                EditingXML_Category = xml.Category;
            */

            string targetFile = $"{EditingXML_FileName}.{EditingXML_Ext}";
            var Target = Directory.EnumerateFiles(Path.GetFullPath(_settings.System.BNSPATCH_DIRECTORY), targetFile, SearchOption.AllDirectories).FirstOrDefault();
            if (Target != null && !Target.IsNullOrEmpty())
            {
                EFileLabel = $"Target file: {Target}";
                _sync.File = Target;
            }

            //EFileLabel = EditingXML_FileName;
            EDescription = EditingXML_Description;
            ETitle = EditingXML_Title;
            ECategoryItem = (CategoryType)EditingXML_Category;
            EPublishBtn = "Confirm";
            ShowSyncMain = false;
            ShowPublishView = true;
        }

        partial void OnEDescriptionChanged(string value)
        {
            ECharacterLimitText = $"{value.Trim().Length}/2000";
        }

        [RelayCommand]
        void BrowsePublish()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = EditingXML_Ext.IsNullOrEmpty() ? "XML Files (*.xml)|*.xml|Patch Files (*.patch)|*.patch" : string.Format("Patch Files(.*)|{0}.{1}", EditingXML_FileName, EditingXML_Ext),
                DefaultExt = EditingXML_Ext.IsNullOrEmpty() ? ".xml" : "." + EditingXML_Ext,
                FileName = EditingXML_FileName.IsNullOrEmpty() ? string.Empty : string.Format("{0}.{1}", EditingXML_FileName, EditingXML_Ext),
                InitialDirectory = Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager"),
                AddExtension = true
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                _sync.File = dialog.FileName;
                EFileLabel = string.Format("Target File: {0}", _sync.File);
                //Debug.WriteLine(_sync.File);
            }
        }

        [RelayCommand]
        async Task PublishToServer()
        {
            ShowPublishStatus = false;
            // Client-side checks, this is also checked with the server so no funny ideas.
            if (_sync.File.IsNullOrEmpty())
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "You need to select a file, click browse and select the file you want to upload", IsError = true });
                await Task.CompletedTask;
                return;
            }
            else if (ECategoryItem == null)
            {
                // This part is probably no longer needed
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "You need to select a category", IsError = true });
                await Task.CompletedTask;
                return;
            }
            else if (ETitle.IsNullOrEmpty())
            {

                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "You must enter a title description", IsError = true });
                await Task.CompletedTask;
                return;
            }
            else if (EDescription.Trim().Length > 2000)
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Maximum character length exceeded with description", IsError = true });
                await Task.CompletedTask;
                return;
            }
            else if (!EditingXML_FileName.IsNullOrEmpty() && Path.GetFileName(_sync.File) != string.Format("{0}.{1}", EditingXML_FileName, EditingXML_Ext))
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = string.Format("{0} does not match the targeted xml {1}", Path.GetFileName(_sync.File), string.Format("{0}.{1}", EditingXML_FileName, EditingXML_Ext)), IsError = true });
                await Task.CompletedTask;
                return;
            }

            /*
             * Client-side check if file exceeds 2MB
             * Don't get any funny ideas of recompiling this without this check
             * the server will still decline your request in a less graceful way
            */
            FileInfo fInfo = new FileInfo(_sync.File);
            if (fInfo.Length > (2 * 1024 * 1024))
            {
                _messageService.Enqueue(new MessageService.MessagePrompt { Message = "Your file exceeds the max limit of 2MB", IsError = true });
                await Task.CompletedTask;
                return;
            }

            /*
                                        Auto Tab / Auto Tumble roll check
                This is still checked by the server so don't get any ideas about removing this check.
                If the server flags this xml your discord_id will be banned from publishing anymore
                and all current xmls uploaded by your discord_id will be set to inactive.
            
            if (_sync.IllegalXMLCheck())
            {
                new ErrorPrompt("Your xml contains edits not authorized on the network.\rRemove auto tab / auto F roll").ShowDialog();
                goto Finished;
            }
            */

            ProgressStatusText = "Sending request...";
            ShowProgressView = true;

            string responseData = "";
            try
            {
                string description = EDescription.Trim();
                // Gzip compress the string then do a Base64 encode on the returned bytes and make it Urlsafe
                string compressedDescription = await _sync.Base64UrlEncode(description);
                string compressedTitle = await _sync.Base64UrlEncode(ETitle);
                string md5 = MD5_File(_sync.File);
                int category = (int)ECategoryItem;

                // POST data that we need to send, serialize it as json-format.
                var postData = JsonConvert.SerializeObject(new SyncData.PUBLISH_XML_POST
                {
                    Auth_Code = _settings.Sync.AUTH_KEY,
                    Title = compressedTitle,
                    Category = category,
                    Hash = md5,
                    Description = compressedDescription,
                    Type = Path.GetExtension(_sync.File).Replace(".", ""),
                    FileName = Path.GetFileNameWithoutExtension(_sync.File),
                    xml_id = EditingXMLId > 0 ? EditingXMLId : 0
                });

                // Submit a request for a security token to upload, Tokens last only 10 minutes
                responseData = await _sync.PostDataToServer(_sync.PublishXML_URL, postData);
                var response = JObject.Parse(responseData);
                //Debug.WriteLine(responseData);

                if ((HttpStatusCode)(int)response["response"] != HttpStatusCode.OK)
                    throw new WebException((string)response["response_msg"]);

                if ((string)response["token"] != "UPDATED")
                {
                    ProgressStatusText = "Uploading...";

                    NameValueCollection nvc = new NameValueCollection
                    {
                        { "token", (string)response["token"] }
                    };

                    // Send the file with the token to the server
                    responseData = await _sync.Upload(nvc);
                    response = JObject.Parse(responseData);
                    if ((HttpStatusCode)(int)response["response"] != HttpStatusCode.OK)
                        throw new WebException((string)response["response_msg"]);
                }
                ShowPublishStatus = true;
                PublishStatusText = (string)response["token"] == "UPDATED" ? "Updated" : "File Published";
            }
            catch (Exception ex)
            {
                ProgressStatusText = ex.GetType() == typeof(JsonReaderException) ? responseData : ex.Message;
                await Task.Delay(5000);
            }
            finally
            {
                await RefreshAllXmls(true);
                ShowProgressView = false;
            }
        }

        [RelayCommand]
        void ClosePublish()
        {
            // Reset variables
            EDescription = string.Empty;
            ETitle = string.Empty;
            ECategoryItem = CategoryType.Other;
            EPublishBtn = "Publish";
            EditingXMLId = -1;
            EditingXML_Category = (int)CategoryType.Other;
            EditingXML_Description = string.Empty;
            EditingXML_Ext = string.Empty;
            EditingXML_FileName = string.Empty;
            EditingXML_Title = string.Empty;
            _sync.File = string.Empty;
            EFileLabel = string.Empty;
            ShowPublishView = false;
            ShowSyncMain = true;
        }
    }
}
