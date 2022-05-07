using System;
using System.Windows;
using System.Net;
using System.Linq;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using BnS_Multitool.Extensions;
using System.Collections.ObjectModel;
using System.Diagnostics; //Remove Later
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Documents;
using System.Windows.Input;
using static BnS_Multitool.Functions.Crypto;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for Sync.xaml
    /// </summary>
    public partial class Sync : Page
    {
        private static SyncClient _sync;
        private static bool SkipAuth = false;
        private static bool ActionInProgress = false;
        private static bool SyncPublished = false;
        private int SelectedCategory = 0;
        private int SortedSelection = 0;
        private string Editing_Title = "";
        private string Editing_Description = "";
        private int Editing_ID = 0;
        private string Editing_FileName = "";
        private string Editing_FileExt = "";
        private static List<SyncData.XML_VIEW_RESPONSE> DOWNLOADED_XMLS = new List<SyncData.XML_VIEW_RESPONSE>();
        private TaskCompletionSource<bool> AuthorizationCompleted = new TaskCompletionSource<bool>();

        private ObservableCollection<XML_View_List> _XmlViewCollection = new ObservableCollection<XML_View_List>();
        public ObservableCollection<XML_View_List> Sync_Available_XMLS { get { return _XmlViewCollection; } }

        public class XML_View_List
        {
            public string Title { get; set; }
            public string User { get; set; }
            public string Date { get; set; }
            public string Type { get; set; }
            public int ID { get; set; }
            public Brush SyncColor { get; set; }
            public long DiscordID { get; set; }
            public int Category { get; set; }
            public string CategoryText { get; set; }
            public bool Synced { get; set; }
            public int Downloads { get; set; }
            public string Description { get; set; }
            public string SyncButton { get; set; }
            public string FileName { get; set; }
        }

        public Sync()
        {
            InitializeComponent();
        }

        private async Task SetupAuthKey()
        {
            SyncAuth.Visibility = Visibility.Visible;
            await AuthorizationCompleted.Task;
            if (!SkipAuth)
            {
                SyncConfig.AUTH_KEY = AuthTokenBox.Password;
                _sync.Auth_Token = SyncConfig.AUTH_KEY;
                await AuthorizeDiscord();
            }
            SyncAuth.Visibility = Visibility.Hidden;
            SyncView.Visibility = Visibility.Visible;
        }

        private async Task AuthorizeDiscord()
        {
            var _splash = new ProgressSplash();
            ProgressPanel.Children.Add(_splash);
            _splash.ProgressText = "Authorizing Discord...";
            ((Storyboard)FindResource("FadeIn")).Begin(ProgressGrid);

            try
            {
                if(_sync == null)
                    _sync = new SyncClient(SyncConfig.AUTH_KEY);

                if (!SyncConfig.AUTH_KEY.IsNullOrEmpty())
                {
                    await _sync.AuthDiscordAsync();
                    // User's discord identity was authorized, change display elements.
                    if (_sync.Authorized)
                    {
                        RestartSyncBtn.Visibility = Visibility.Hidden;
                        ViewSyncPublishBtn.Visibility = Visibility.Visible;
                        DiscordName.Content = _sync.Discord.username;
                        // Make sure user has an avatar to set otherwise skip
                        if(!_sync.Discord.avatar.IsNullOrEmpty())
                            DiscordPicture.ImageSource = new BitmapImage(new Uri(string.Format("{0}{1}/{2}.png?size=128", _sync.Avatar_url, _sync.Discord.id, _sync.Discord.avatar)));
                    }
                    else
                        throw new Exception("Failed to authorize with token, generate a new one.");
                }
            }
            catch (Exception ex)
            {
                _splash.ProgressText = ex.Message;
                await Task.Delay(5000);
            }
            ProgressPanel.Children.Clear();
            ((Storyboard)FindResource("FadeOut")).Begin(ProgressGrid);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            /*
              Code ref
            string compressedBase64 = Convert.ToBase64String(GzipContents("This is my compressed string\rwith new lines\rand all"));
            string compressed = await GunzipContentsAsync(Convert.FromBase64String(compressedBase64));
            */

            if (_sync == null) await AuthorizeDiscord();
            if (!_sync.Authorized)
                await SetupAuthKey();
            else
            {
                ((Storyboard)FindResource("FadeOut")).Begin(SyncPublish);
                ((Storyboard)FindResource("FadeIn")).Begin(SyncView);
            }

            if (categoryFilter.SelectedIndex != SelectedCategory)
                categoryFilter.SelectedIndex = 0;
            else
                await RefreshAllXmls();
        }

        /// <summary>
        /// Two-step publish process for submitting XML and patch files
        /// First step is to send the requested info about the file and get a token
        /// Second step requires you to send the file and token to the server, if token is fine
        /// the file will be uploaded and the xml/patch will show on the network
        /// </summary>
        /// This two-step process is setup for a reason, the main reason being performance and the next is
        /// security. The server will also doing its own set of checks on the initial publish process to
        /// disallow racial slurs, malicious xmls and other things.
        private async void SyncPublish_ConfirmPublish(object sender, RoutedEventArgs e)
        {
            if (ActionInProgress) return;
            ActionInProgress = true;

            // Client-side checks, this is also checked with the server so no funny ideas.
            if(_sync.File.IsNullOrEmpty())
            {
                new ErrorPrompt("You need to select a file, click browse and select the file you want to upload", false, true).ShowDialog();
                goto Finished;
            } else if (categoryBox.SelectedIndex == -1)
            {
                new ErrorPrompt("You need to select a category", false, true).ShowDialog();
                goto Finished;
            } else if (SyncPublish_TitleBox.Text.IsNullOrEmpty())
            {
                new ErrorPrompt("You must enter a title description", false, true).ShowDialog();
                goto Finished;
            } else if (new TextRange(SyncPublish_Description.Document.ContentStart, SyncPublish_Description.Document.ContentEnd).Text.Trim().Length > 2000)
            {
                new ErrorPrompt("Maximum character length exceeded with description").ShowDialog();
                goto Finished;
            } else if(!Editing_FileName.IsNullOrEmpty() && Path.GetFileName(_sync.File) != string.Format("{0}.{1}", Editing_FileName, Editing_FileExt))
            {
                new ErrorPrompt(string.Format("{0} does not match the targeted xml {1}", Path.GetFileName(_sync.File), string.Format("{0}.{1}", Editing_FileName, Editing_FileExt)), false, true).ShowDialog();
                goto Finished;
            }

            /*
             * Client-side check if file exceeds 2MB
             * Don't get any funny ideas of recompiling this without this check
             * the server will still decline your request in a less graceful way
            */
            FileInfo fInfo = new FileInfo(_sync.File);
            if (fInfo.Length > (2 * 1024 * 1024))
            {
                new ErrorPrompt("Your file exceeds the max limit of 2MB", false, true).ShowDialog();
                goto Finished;
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

            var _splash = new ProgressSplash();
            ProgressPanel.Children.Add(_splash);
            _splash.ProgressText = "Sending request...";
            ((Storyboard)FindResource("FadeIn")).Begin(ProgressGrid);

            string responseData = "";
            try
            {
                string description = new TextRange(SyncPublish_Description.Document.ContentStart, SyncPublish_Description.Document.ContentEnd).Text;
                // Gzip compress the string then do a Base64 encode on the returned bytes and make it Urlsafe
                string compressedDescription = await _sync.Base64UrlEncode(description);
                string compressedTitle = await _sync.Base64UrlEncode(SyncPublish_TitleBox.Text);
                string md5 = MD5_File(_sync.File);
                int category = categoryBox.SelectedIndex;

                // POST data that we need to send, serialize it as json-format.
                var postData = JsonConvert.SerializeObject(new SyncData.PUBLISH_XML_POST
                {
                    Auth_Code = SyncConfig.AUTH_KEY,
                    Title = compressedTitle,
                    Category = category,
                    Hash = md5,
                    Description = compressedDescription,
                    Type = Path.GetExtension(_sync.File).Replace(".",""),
                    FileName = Path.GetFileNameWithoutExtension(_sync.File),
                    xml_id = Editing_ID > 0 ? Editing_ID : 0
                });

                // Submit a request for a security token to upload, Tokens last only 10 minutes
                responseData = await _sync.PublishXMLRequest(postData);
                var response = JObject.Parse(responseData);
                //Debug.WriteLine(responseData);

                if ((HttpStatusCode)(int)response["response"] != HttpStatusCode.OK)
                    throw new WebException((string)response["response_msg"]);

                if ((string)response["token"] != "UPDATED")
                {
                    _splash.ProgressText = "Uploading...";

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
                ((Storyboard)FindResource("animate")).Begin(StatusGrid);
                StatusText.Text = (string)response["token"] == "UPDATED" ? "Updated" : "File Published";
            } catch (Exception ex)
            {
                _splash.ProgressText = ex.GetType() == typeof(JsonReaderException) ? responseData : ex.Message;
                await Task.Delay(5000);
            } finally
            {
                await RefreshAllXmls();
                ((Storyboard)FindResource("FadeOut")).Begin(ProgressGrid);
                ProgressPanel.Children.Clear();
            }

        Finished:
            ActionInProgress = false;
        }

        private void Browse_XML_File(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = Editing_FileExt.IsNullOrEmpty() ? "XML Files (*.xml)|*.xml|Patch Files (*.patch)|*.patch" : string.Format("Patch Files(.*)|{0}.{1}",Editing_FileName, Editing_FileExt),
                DefaultExt = Editing_FileExt.IsNullOrEmpty() ? ".xml" : "." + Editing_FileExt,
                FileName = Editing_FileName.IsNullOrEmpty() ? string.Empty : string.Format("{0}.{1}",Editing_FileName, Editing_FileExt),
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager"),
                AddExtension = true
            };
            bool? result = dialog.ShowDialog();
            if(result == true)
            {
                _sync.File = dialog.FileName;
                FILE_NAME_LABEL.Text = string.Format("Targeted File: {0}", Path.GetFileName(_sync.File));
            }
        }

        private void SkipAuthorization(object sender, RoutedEventArgs e)
        {
            SkipAuth = true;
            RestartSyncBtn.Visibility = Visibility.Visible;
            AuthorizationCompleted.SetResult(true);
        }

        private void UseAuthorizationToken(object sender, RoutedEventArgs e) => AuthorizationCompleted.SetResult(true);
        private void GetAuthToken(object sender, RoutedEventArgs e) => Process.Start("http://sync.bns.tools/discord/?login");

        private async void RestartSync(object sender, RoutedEventArgs e)
        {
            SyncView.Visibility = Visibility.Hidden;
            SkipAuth = false;
            AuthorizationCompleted = new TaskCompletionSource<bool>();
            await SetupAuthKey();
        }

        private async Task RefreshAllXmls()
        {
            var category = SelectedCategory - 1;

            try
            {
                NameValueCollection nvc = new NameValueCollection();

                if (_sync.Discord != null && SortedSelection == 3)
                    nvc.Add("uid", _sync.Discord.id.ToString());
                else
                    nvc.Add("q", category == -1 ? "all" : category.ToString());

                string responseData = await _sync.Fetch_XMLS(nvc);
                if (responseData.IsNullOrEmpty())
                {
                    new ErrorPrompt("There was an error reaching the host: sync.bns.tools", false, true).ShowDialog();
                    return;
                }

                var availableXMLs = JsonConvert.DeserializeObject<SyncData.XMLS_RESPONSE>(responseData);
                if (availableXMLs.response != HttpStatusCode.OK)
                {
                    new ErrorPrompt("Error reaching the database, try again later?", false, true).ShowDialog();
                    return;
                }
                var _splash = new ProgressSplash();
                ProgressPanel.Children.Add(_splash);
                _splash.ProgressText = "Retrieving from server..";
                ((Storyboard)FindResource("FadeIn")).Begin(ProgressGrid);
                _XmlViewCollection.Clear();

                DOWNLOADED_XMLS = availableXMLs.xmls;
                var sortedXmls = SortXmlsBy();
                foreach (var xml in sortedXmls)
                {
                    Brush color;
                    string syncButtonText = '\uE1DF'.ToString();
                    bool isSynced = SyncConfig.Synced != null && _sync.IsSynced(xml);

                    if (_sync.Discord != null && xml.Discord_id == _sync.Discord.id)
                    {
                        isSynced = false;
                        syncButtonText = '\uE104'.ToString(); // Pencil Icon Segoe UI Symbol
                        color = Brushes.Orange;
                    }
                    else if (isSynced && SyncConfig.Synced != null)
                        color = Brushes.Green;
                    else if (SyncConfig.Synced != null && SyncConfig.Synced.Any(x => x.ID == xml.ID) && !isSynced)
                        color = Brushes.Red;
                    else
                        color = Brushes.White;

                    _XmlViewCollection.Add(new XML_View_List
                    {
                        ID = xml.ID,
                        Title = await _sync.Base64UrlDecodeAsync(xml.Title),
                        User = xml.Username,
                        DiscordID = xml.Discord_id,
                        Date = _sync.UnixToDateTime(long.Parse(xml.Date)).ToString("g"),
                        Type = xml.Type,
                        SyncColor = color,
                        CategoryText = ((SyncData.CategoryType)xml.Category).GetDescription(),
                        Synced = !isSynced, // Remember the logic needs to be flipped
                        Downloads = xml.Downloads,
                        Description = xml.Description,
                        SyncButton = syncButtonText,
                        FileName = xml.Name
                    });
                }

                ProgressPanel.Children.Clear(); // Get rid of splash
                ((Storyboard)FindResource("FadeOut")).Begin(ProgressGrid);
            } catch (Exception ex)
            {
                //Log later
            }
        }

        private void ShowPublishWindow(object sender, RoutedEventArgs e)
        {
            SyncPublish_TitleBox.Text = string.Empty;
            SyncPublish_Description.Document.Blocks.Clear();
            _sync.File = string.Empty;
            categoryBox.SelectedIndex = -1;
            FILE_NAME_LABEL.Text = string.Empty;
            ((Storyboard)FindResource("FadeOut")).Begin(SyncView);
            ((Storyboard)FindResource("FadeIn")).Begin(SyncPublish);
        }

        private void ShowPublishWindowEdit()
        {
            SyncPublish_TitleBox.Text = Editing_Title;
            SyncPublish_Description.Document.Blocks.Clear();
            SyncPublish_EditSubmit.Content = "Confirm";
            SyncPublish_Description.AppendText(Editing_Description);
            ((Storyboard)FindResource("FadeOut")).Begin(SyncView);
            ((Storyboard)FindResource("FadeIn")).Begin(SyncPublish);
        }

        private async void ClosePublishWindow(object sender, RoutedEventArgs e)
        {
            // Only commit a call to the server 
            if (SyncPublished)
            {
                SyncPublished = false;
                await RefreshAllXmls();
            }

            SyncPublish_EditSubmit.Content = "Publish";
            Editing_FileName = string.Empty;
            Editing_Description = string.Empty;
            Editing_FileExt = string.Empty;
            Editing_Title = string.Empty;
            Editing_ID = -1;
            categoryBox.SelectedIndex = -1;
            FILE_NAME_LABEL.Text = string.Empty;
            SyncPublish_TitleBox.Text = string.Empty;
            _sync.File = string.Empty;

            ((Storyboard)FindResource("FadeOut")).Begin(SyncPublish);
            ((Storyboard)FindResource("FadeIn")).Begin(SyncView);
            //SyncView.Visibility = Visibility.Visible;
            //SyncPublish.Visibility = Visibility.Hidden;
        }

        private async void SyncXMLBtn(object sender, RoutedEventArgs e)
        {
            if (ActionInProgress) return;
            ActionInProgress = true;

            XML_View_List entry = ((ListViewItem)AvailablexmlsView.ContainerFromElement((Button)sender)).Content as XML_View_List;

            // Check if we're editing
            if(_sync.Discord != null && entry.DiscordID == _sync.Discord.id)
            {
                Editing_ID = entry.ID;
                Editing_Description = await _sync.Base64UrlDecodeAsync(entry.Description);
                Editing_Title = entry.Title;
                Editing_FileExt = entry.Type;
                Editing_FileName = entry.FileName;
                categoryBox.SelectedIndex = entry.Category;

                ShowPublishWindowEdit();
                ActionInProgress = false;
                return;
            }
            var _splash = new ProgressSplash();
            ProgressPanel.Children.Add(_splash);
            _splash.ProgressText = String.Format("Syncing {0}...", entry.Title);
            ((Storyboard)FindResource("FadeIn")).Begin(ProgressGrid);
            try
            {
                string response = await _sync.Fetch_XMLAsync(entry.ID, true);
                Debug.WriteLine(response);
                if (response.IsNullOrEmpty()) throw new Exception("Error retrieving data from server");
                SyncData.XML_VIEW_RESPONSE xml = JsonConvert.DeserializeObject<SyncData.XMLS_RESPONSE>(response).xml;

                
                if (xml == null) throw new Exception("Error retrieving data from server");
                if (xml.Title.IsNullOrEmpty()) throw new Exception("Error retrieving data from server");

                var xmlStruct = new SyncConfig.Synced_Xmls
                {
                    ID = xml.ID,
                    Username = xml.Username,
                    Discord_id = xml.Discord_id,
                    Date = xml.Date,
                    Title = xml.Title,
                    Type = xml.Type,
                    Category = xml.Category,
                    Hash = xml.Hash,
                    Description = xml.Description,
                    Name = xml.Name,
                    Version = xml.Version
                };

                if (SyncConfig.Synced != null && !SyncConfig.Synced.Any(x => x.ID == xml.ID))
                    SyncConfig.Synced.Add(xmlStruct);
                else if (SyncConfig.Synced == null)
                    SyncConfig.Synced = new List<SyncConfig.Synced_Xmls> { xmlStruct };
                else
                {
                    var synced_xml = SyncConfig.Synced.FirstOrDefault(x => x.ID == xml.ID);
                    synced_xml = xmlStruct;
                }

                SyncConfig.Save();

                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString())))
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString()));

                using (GZipWebClient client = new GZipWebClient())
                    await client.DownloadFileTaskAsync(
                        string.Format("http://sync.bns.tools/xml_data/{0}/{1}.{2}", xml.Discord_id, xml.Name, xml.Type),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)
                        );

                if (_sync.Discord == null)
                    entry.SyncColor = Brushes.Green;
                else if (xml.Discord_id != _sync.Discord.id)
                    entry.SyncColor = Brushes.Green;

                entry.Synced = false;
                AvailablexmlsView.Items.Refresh();
            } catch (Exception ex)
            {
                _splash.ProgressText = ex.Message;
                await Task.Delay(3000);
            }
            ProgressPanel.Children.Clear(); // Get rid of splash
            ((Storyboard)FindResource("FadeOut")).Begin(ProgressGrid);
            ActionInProgress = false;
        }

        private int INFO_XML_ID;
        private async void AvailablexmlsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                INFO_DESCRIPTION.Document.Blocks.Clear();
                var control = sender as ListView;
                // This is a cheesey way to get around having a clicked event but without having a selected focus
                if (control != null && control.SelectedIndex == -1) return;
                if (control == null) return;
                var selectedItem = control.SelectedItem as XML_View_List;
                control.SelectedIndex = -1;
                if (ActionInProgress) return;
                ActionInProgress = true;

                INFO_TITLE.Text = selectedItem.Title;
                INFO_DOWNLOADS.Content = string.Format("Downloads: {0:#,##0}", selectedItem.Downloads);
                INFO_USER.Content = string.Format("Uploaded by {0}", selectedItem.User);
                INFO_XML_ID = selectedItem.ID;
                if (SyncConfig.Synced != null && SyncConfig.Synced.Any(x => x.ID == INFO_XML_ID))
                    SyncInfo_Unsync.Visibility = Visibility.Visible;
                else
                    SyncInfo_Unsync.Visibility = Visibility.Hidden;

                ((Storyboard)FindResource("FadeIn")).Begin(XML_DESCRIPTION_GRID);

                if (!selectedItem.Description.IsNullOrEmpty())
                    INFO_DESCRIPTION.AppendText(await _sync.Base64UrlDecodeAsync(selectedItem.Description));
            }
            catch (Exception ex)
            {
                INFO_DESCRIPTION.AppendText(ex.Message);
            }
            ActionInProgress = false;
        }

        private async void CategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedCategory = categoryFilter.SelectedIndex;
            await RefreshAllXmls();
        }

        private void CloseDescriptionBox(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeOut")).Begin(XML_DESCRIPTION_GRID);
            INFO_DESCRIPTION.Document.Blocks.Clear();
            INFO_TITLE.Text = "";
            INFO_USER.Content = "";
        }

        private async void SyncGetUpdates(object sender, RoutedEventArgs e)
        {
            if (ActionInProgress) return;
            ActionInProgress = true;
            if (SyncConfig.Synced == null) goto Completed;

            var _splash = new ProgressSplash();
            ProgressPanel.Children.Add(_splash);
            _splash.ProgressText = "Getting updates...";
            ((Storyboard)FindResource("FadeIn")).Begin(ProgressGrid);
            HttpClient client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            await Task.Delay(500);

            foreach (var xml in SyncConfig.Synced)
            {
                try
                {
                    string response = await _sync.Fetch_XMLAsync(xml.ID);
                    if (response.IsNullOrEmpty()) throw new Exception("response was blank");

                    SyncData.XML_VIEW_RESPONSE xml_data = JsonConvert.DeserializeObject<SyncData.XMLS_RESPONSE>(response).xml;
                    if (!_sync.IsSynced(xml_data))
                    {
                        _splash.ProgressText = string.Format("Updating {0}", xml.Name);
                        if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString())))
                            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString()));

                        var dataResult = await client.GetAsync(string.Format("http://sync.bns.tools/xml_data/{0}/{1}.{2}", xml.Discord_id, xml.Name, xml.Type));
                        using (var stream = await dataResult.Content.ReadAsStreamAsync())
                        using (var filestream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type), FileMode.Create))
                            await stream.CopyToAsync(filestream);

                        // Update synced entry with data from server
                        var syncEntry = SyncConfig.Synced.FirstOrDefault(x => x.ID == xml.ID);
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
                        var item = Sync_Available_XMLS.FirstOrDefault(x => x.ID == xml.ID);
                        if (item != null)
                        {
                            item.SyncColor = Brushes.Green;
                            item.Synced = false; // logic is flipped, remember that
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    // Some error logging later
                }
            }

            client.Dispose();
            SyncConfig.Save();
            AvailablexmlsView.Items.Refresh(); //Incase there was changes refresh the data bindings
            ProgressPanel.Children.Clear(); //Get rid of splash
            ((Storyboard)FindResource("FadeOut")).Begin(ProgressGrid);

        Completed:
            ActionInProgress = false;
        }

        private void SyncPublish_Desc_Updated(object sender, TextChangedEventArgs e)
        {
            try
            {
                var rtb = sender as RichTextBox;
                TextRange textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                var text = textRange.Text.Trim();
                if (text.Length > 2000)
                {
                    while (rtb.CaretPosition.DeleteTextInRun(-1) == 0)
                        rtb.CaretPosition = rtb.CaretPosition.GetPositionAtOffset(0);
                }

                SyncPublish_CharacterLimit.Content = string.Format("{0}/2000", text.Length);
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SyncPublish_Desc_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Delete && e.Key != Key.Back)
                {
                    var rtb = sender as RichTextBox;
                    var textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                    if (textRange.Text.Trim().Length > 2000)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Sorts through the downloaded data and returns it.
        /// </summary>
        /// <returns></returns>
        private List<SyncData.XML_VIEW_RESPONSE> SortXmlsBy()
        {
            List<SyncData.XML_VIEW_RESPONSE> orderedSelection;

            switch (SortedSelection)
            {
                case 1:
                    orderedSelection = DOWNLOADED_XMLS.OrderBy(x => x.Date).ToList();
                    break;
                case 2:
                    orderedSelection = DOWNLOADED_XMLS.OrderByDescending(x => x.Downloads).ToList();
                    break;
                case 3:
                    if (_sync.Discord != null)
                        orderedSelection = DOWNLOADED_XMLS.Where(x => x.Discord_id == _sync.Discord.id).ToList();
                    else
                        orderedSelection = DOWNLOADED_XMLS;
                    break;
                default:
                    orderedSelection = DOWNLOADED_XMLS.OrderByDescending(x => x.Date).ToList();
                    break;
            }
            return orderedSelection;
        }

        private async void SyncView_SortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var SelectionObj = sender as ComboBox;
            //Redownload the list if last selection was self published
            if(SortedSelection == 3)
            {
                SortedSelection = SelectionObj.SelectedIndex;
                await RefreshAllXmls();
            } else
                SortedSelection = SelectionObj.SelectedIndex;

            var orderedSelection = SortXmlsBy();

            _XmlViewCollection.Clear();
            foreach (var xml in orderedSelection)
            {
                Brush color;
                bool isSynced = SyncConfig.Synced != null && _sync.IsSynced(xml);
                string syncButtonText = '\uE1DF'.ToString();

                if (_sync.Discord != null && xml.Discord_id == _sync.Discord.id)
                {
                    isSynced = false;
                    syncButtonText = '\uE104'.ToString(); //Pencil Icon Segoe UI Symbol
                    color = Brushes.Orange;
                }
                else if (isSynced && SyncConfig.Synced != null)
                    color = Brushes.Green;
                else if (SyncConfig.Synced != null && SyncConfig.Synced.Any(x => x.ID == xml.ID) && !isSynced)
                    color = Brushes.Red;
                else
                    color = Brushes.White;

                _XmlViewCollection.Add(new XML_View_List
                {
                    ID = xml.ID,
                    Title = await _sync.Base64UrlDecodeAsync(xml.Title),
                    User = xml.Username,
                    DiscordID = xml.Discord_id,
                    Date = _sync.UnixToDateTime(long.Parse(xml.Date)).ToString("g"),
                    Type = xml.Type,
                    SyncColor = color,
                    CategoryText = ((SyncData.CategoryType)xml.Category).GetDescription(),
                    Synced = !isSynced, //Remember the logic needs to be flipped
                    Downloads = xml.Downloads,
                    Description = xml.Description,
                    FileName = xml.Name,
                    SyncButton = syncButtonText
                });
            }
        }

        private async void SyncView_SyncRefresh(object sender, RoutedEventArgs e) => await RefreshAllXmls();
        private async void SyncInfo_UnsyncAction(object sender, RoutedEventArgs e)
        {
            if (SyncConfig.Synced == null) return;
            var xml = SyncConfig.Synced.FirstOrDefault(x => x.ID == INFO_XML_ID);
            if(xml == null) return;
            SyncConfig.Synced.Remove(xml);

            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), string.Format("{0}.{1}", xml.Name, xml.Type))))
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), string.Format("{0}.{1}", xml.Name, xml.Type)));

            SyncConfig.Save();

            ((Storyboard)FindResource("FadeOut")).Begin(XML_DESCRIPTION_GRID);
            await RefreshAllXmls();
        }
    }
}