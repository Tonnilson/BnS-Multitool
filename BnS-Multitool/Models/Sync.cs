using BnS_Multitool.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using static BnS_Multitool.Functions.Crypto;
using BnS_Multitool.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.ComponentModel;

namespace BnS_Multitool.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CategoryType
    {
        [Description("Other")]
        Other = 0,
        [Description("Blademaster")]
        BM = 1,
        [Description("Kung Fu Master")]
        KFM = 2,
        [Description("Forcemaster")]
        FM = 3,
        [Description("Destroyer")]
        DES = 4,
        [Description("Assassin")]
        SIN = 5,
        [Description("Summoner")]
        SUM = 6,
        [Description("Blade dancer")]
        BD = 7,
        [Description("Warlock")]
        WL = 8,
        [Description("Soul Fighter")]
        SF = 9,
        [Description("Gunslinger")]
        GUN = 10,
        [Description("Warden")]
        WAR = 11,
        [Description("Archer")]
        ARC = 12,
        [Description("Astromancer")]
        AST = 13,
        [Description("Dualblade")]
        DB = 14,
        [Description("Bard")]
        BRD = 15
    }

    public class SyncClient
    {
        public string PublishXML_URL { get { return "https://sync.bns.tools/publish/?action=publish"; } }
        public string RemoveXML_URL { get { return "https://sync.bns.tools/removeXML"; } }
        public string Auth_Token { get; set; }
        private bool authorized { get; set; }

        public string File { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DISCORD_JSON_RESPONSE Discord { get; set; }

        public string Avatar_url => "https://cdn.discordapp.com/avatars/";

        public bool Authorized
        {
            get { return authorized; }
        }

        public class DISCORD_JSON_RESPONSE
        {
            public long id { get; set; }
            public string username { get; set; }
            public string avatar { get; set; }
        }

        private readonly Settings _settings;
        private readonly ILogger<SyncClient> _logger;
        private readonly httpClient _httpClient;

        public SyncClient(Settings settings, ILogger<SyncClient> logger, httpClient httpClient)
        {
            _settings = settings;
            _logger = logger;
            _httpClient = httpClient;

            this.Auth_Token = _settings.Sync.AUTH_KEY; // Why the fuck am I using this?
        }

        /// <summary>
        /// Base64 encode a stream and make it url-safe
        /// </summary>
        /// <param name="text"></param>
        public async Task<string> Base64UrlEncode(string text) => Convert.ToBase64String(await GzEncodeAsync(text)).TrimEnd(new char[] { '=' }).Replace('+', '-').Replace('/', '_');

        /// <summary>
        /// Converts a base64 url-safe string to base64 and decodes it
        /// </summary>
        /// <param name="text"></param>
        public async Task<string> Base64UrlDecodeAsync(string text)
        {
            string base64str = text.Replace('_', '/').Replace('-', '+');
            switch (text.Length % 4)
            {
                case 2: base64str += "=="; break;
                case 3: base64str += "="; break;
            }
            return await GzDecodeAsync(Convert.FromBase64String(base64str));
        }

        /// <summary>
        /// Applies Gzip compression on a string
        /// </summary>
        /// <param name="uncompressed_data"></param>
        /// <returns>GZip byte array</returns>
        public static async Task<byte[]> GzEncodeAsync(string uncompressed_data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzs = new GZipStream(ms, CompressionLevel.Optimal))
                using (StreamWriter sw = new StreamWriter(gzs, Encoding.UTF8))
                    await sw.WriteAsync(uncompressed_data);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a GZip compressed byte array
        /// </summary>
        /// <param name="compressed"></param>
        /// <returns>Decompressed string</returns>
        public static async Task<string> GzDecodeAsync(byte[] compressed)
        {
            using (MemoryStream ms = new MemoryStream(compressed))
            {
                using (GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress))
                using (StreamReader sr = new StreamReader(gzs, Encoding.UTF8))
                    return await sr.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Retrieves a json formatted string of available xmls from database
        /// Uses GZipWebclient to use GZip compression on the content stream
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<string> Fetch_XMLS(NameValueCollection queryString)
        {
            string postData = "";
            if (queryString == null) return "";
            foreach (string key in queryString.Keys)
                postData += string.Format("{0}={1}&", key, queryString.Get(key));

            string WEB_ADDR = string.Format("https://sync.bns.tools/xmls/?{0}", postData);
            try
            {
                var response = await _httpClient.DownloadString(WEB_ADDR);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch xmls");
                return "";
            }
        }

        /// <summary>
        /// Convert unix timestamp to systems local UTC time.
        /// </summary>
        /// <param name="unixstamp"></param>
        /// <returns>Local UTC Time</returns>
        public DateTime UnixToDateTime(long unixstamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixstamp).ToLocalTime();

        public async Task<string> PostDataToServer(string url, string postData)
        {
            HttpWebRequest client = (HttpWebRequest)WebRequest.Create(url);
            client.Method = "POST";
            client.ContentType = "application/json";
            client.Accept = client.ContentType;

            var content = new StringContent(postData, Encoding.UTF8, "application/json");
            var response = await _httpClient.UploadString(url, content);
            return response;
        }

        /*
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⣀⠠⠒⠄⣰⣾⣿⣿⣿⠉⠐⠠⠤⢄⠄⠄⠄⠄⠄⠄
        ⠄⠄⠄⠄⠄⠄⣠⠔⠊⡽⠄⠄⠄⠄⢸⣿⣿⣿⣿⠃⠄⠄⠄⠄⣷⣦⡄⠄⠄⠄
        ⠄⠄⠄⠄⡤⠊⠄⠄⠄⡇⠄⠄⠄⠄⠻⣿⣿⣿⣿⡇⠄⠄⠄⠄⣹⣿⣿⡀⠄⠄
        ⠄⠄⣠⣾⡦⠄⠄⠄⠄⢳⠄⠄⠄⠄⣰⣿⣿⣿⡋⠄⠄⠄⠄⠄⠹⣿⣿⡃⠄⠄
        ⠄⢀⣹⣿⣷⠄⠄⠄⠄⠘⢆⣀⠄⠄⢋⣭⣭⠭⣶⣬⡛⢋⣴⠟⣣⣬⣍⠃⠄⠄
        ⠄⣿⣿⣿⣿⣆⡀⠄⠄⠄⣀⣨⣭⣶⢋⣩⣶⣿⣶⠶⣦⡈⠁⣾⣿⠄⠄⠱⡄⠄
        ⠠⣾⣿⣿⣿⡿⢟⣠⣼⣿⣿⣿⣿⣿⣮⡹⣿⣿⠁⢀⢸⣿⡄⢻⣿⣇⠄⢃⡿⠄
        ⠄⠈⢋⣉⣵⣾⣿⣿⣿⣿⣿⣿⣿⣿⢸⡇⣿⡇⠄⠋⣸⠋⠄⠄⠈⠉⢉⠥⠖⠁
        ⣠⣾⣿⣿⠟⣫⣔⣒⠻⣿⣿⣿⠹⣿⡘⠷⣭⣭⣭⡭⣥⠄⠄⠄⠄⠄⢰⣵⣾⡧
        ⣿⣿⣿⣿⢸⡟⣍⠻⣷⣜⡻⠿⣿⣶⣤⣤⣤⣤⣴⣾⣿⣦⣀⠄⢀⣠⡿⢟⣋⠄
        ⣿⣿⣿⣿⠸⣧⡹⢿⣮⣙⡻⠷⢦⣭⣛⣛⣛⣛⣛⣛⣛⣛⣛⣫⠭⢖⣚⡍⠁⠄
        ⢿⣿⣿⣿⣧⡻⢷⣤⣀⠄⠙⠛⠷⣦⣬⣭⣭⣭⣭⡍⠉⠁⠄⠄⢀⠰⠛⠄⠄⠄
        ⣦⡙⢿⣿⣿⣿⣷⣮⣝⡻⠷⠶⢤⣤⣬⣭⣭⡭⠥⢤⣤⡤⠶⣊⠁⠄⠄⠄⠄⠄
        ⣿⣿⣦⣭⣛⠻⠿⠿⢟⡻⠐⢃⠒⢥⣤⣄⢤⠠⢈⠂⣵⣶⣾⣿⣆⠄⠄⠄⠄⠄
        ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠐⠂⠴⢞⠒⠒⡸⠐⢬⢀⣿⣿⣿⣿⣿⣷⡄⠄⠄⠄
                There's totally a better way
                     but I am a clown
        */
        public bool IsSynced(SyncData.XML_VIEW_RESPONSE xml)
        {
            if (this.Discord == null && _settings.Sync.Synced != null)
            {
                if (!_settings.Sync.Synced.Any(x => x.ID == xml.ID)) return false;
                if (!System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type))) return false;
                if (!_settings.Sync.Synced.Any(x => x.ID == xml.ID && x.Hash == xml.Hash)) return false;
                if (MD5_File(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
            }
            else if (_settings.Sync.Synced != null && xml.Discord_id != this.Discord.id)
            {
                if (!_settings.Sync.Synced.Any(x => x.ID == xml.ID)) return false;
                if (!System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type))) return false;
                if (!_settings.Sync.Synced.Any(x => x.ID == xml.ID && x.Hash == xml.Hash)) return false;
                if (MD5_File(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
            }
            else if (this.Discord != null && xml.Discord_id == this.Discord.id)
            {
                bool exists = System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", xml.Name + "." + xml.Type)) ||
                    System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type));
                if (!exists) return false;
                else
                {
                    if (System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", xml.Name + "." + xml.Type)))
                        if (MD5_File(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", xml.Name + "." + xml.Type)) != xml.Hash) return false;
                    if (System.IO.File.Exists(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)))
                        if (MD5_File(Path.Combine(_settings.System.BNSPATCH_DIRECTORY, "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
                }
            }
            else if (_settings.Sync.Synced == null && this.Discord != null && xml.Discord_id != this.Discord.id) return false;
            return true;
        }

        /// <summary>
        /// Retrieves json formatted string response about specific xml
        /// </summary>
        /// <param name="id">Database ID of xml</param>
        /// <param name="download">Set flag for file xml being downloaded</param>
        /// <returns></returns>
        public async Task<string> Fetch_XMLAsync(int id, bool download = false)
        {
            string WEB_ADDR = string.Format("https://sync.bns.tools/xmls/?id={0}&full{1}", id, download ? "&download" : string.Empty);
            string response = await _httpClient.DownloadString(WEB_ADDR);
            return response;
        }

        /// <summary>
        /// Work in-progress function never finished or in use (yet)
        /// Will filter XML's and check for specific skills having key-state unpress set to it
        /// Meant to target Auto [TAB] escape, Auto [F] roll and possibly even aerial abilities.
        /// </summary>
        private string[] Illegal_Ids = { "101440", "111090", "131260", "133420", "121160", "141260", "143132" };
        public bool IllegalXMLCheck()
        {
            //Load the data into a string and trim it of white spaces
            string fileData = System.IO.File.ReadAllText(this.File);
            XDocument xml = XDocument.Parse(fileData);
            var nodes = xml.XPathSelectElements("/patches/patch/select-nodes");
            foreach (var node in nodes)
            {
                string attr = node.Attribute("query").Value;
                if (node.Attribute("query").Value.Contains("condition[@skill") && Array.Exists(Illegal_Ids, e => attr.Contains(e)))
                {
                    var children = node.Descendants("append-attribute").Descendants();
                    foreach (var child in children)
                        if (child.Attribute("value").Value.ToLower() == "unpress")
                            return true;
                }
                else if (node.Descendants("select-node").Descendants("insert-sibling-before").Descendants("append-child").Descendants("append-attribute").Attributes().Count() > 0)
                {
                    var children = node.Descendants("select-node").Descendants("insert-sibling-before").Descendants("append-child").Descendants("append-attribute").Attributes().ToList();
                    if (children.Count == 0) continue;
                    if (children.Any(x => x.Value == "unpress") && children.Any(x => Illegal_Ids.Contains(x.Value)))
                        return true;
                }
            }

            fileData = Regex.Replace(fileData, @"\s+", string.Empty);

            //bnspatch append-buffer check
            //TODO :: Handle BnS buddy style but I honestly can't be bothered, the majority of people use bnspatch style for these things. Technically this should detect it..? Since bns-buddy is literal find-replace.
            //Append-Buffers do a literal replace of the entire contents, to read it I would need to parse it as xml while already reading a parsed xml and i'm too lazy to do that so regex comes in.
            Regex regexAppendBuffer = new Regex(string.Format("<decision><condition skill=\"({0})\"\\/><(.*?)key-status=\"unpress\"(.*?)<\\/decision>", string.Join("|", Illegal_Ids)), RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            var bufferMatches = regexAppendBuffer.Matches(fileData);

            return bufferMatches.Count > 0;
        }

        /// <summary>
        /// Uploads file to server
        /// </summary>
        /// <param name="queryString">Collection containing access token</param>
        /// <returns></returns>
        public async Task<string> Upload(NameValueCollection queryString)
        {
            string postData = "?action=upload";
            if (queryString != null)
            {
                foreach (string key in queryString.Keys)
                    postData += string.Format("&{0}={1}", key, queryString.Get(key));
            }

            string WEB_ADDR = string.Format("https://sync.bns.tools/publish/{0}", postData);
            var response = await _httpClient.UploadFile(WEB_ADDR, this.File);

            return response;
        }

        /// <summary>
        /// Commits Get request for a discord users name, id and avatar.
        /// </summary>
        /// <returns></returns>
        public async Task<Task> AuthDiscordAsync()
        {
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create("https://discordapp.com/api/users/@me");
            webrequest.Method = "Get";
            webrequest.ContentLength = 0;
            webrequest.Headers.Add("Authorization", string.Format("Bearer {0}", this.Auth_Token));
            webrequest.ContentType = "application/x-www-form-urlencoded";

            string responseString;
            using (HttpWebResponse response = webrequest.GetResponse() as HttpWebResponse)
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                responseString = await sr.ReadToEndAsync();

            this.Discord = JsonConvert.DeserializeObject<DISCORD_JSON_RESPONSE>(responseString);
            this.authorized = !this.Discord.username.IsNullOrEmpty();
            return Task.CompletedTask;
        }

        public async Task<SyncData.Discord_Refresh> DiscordRefreshToken()
        {
            if (_settings.Sync.AUTH_REFRESH == null)
                return null;

            NameValueCollection nvc = new NameValueCollection
            {
                { "client_id", Properties.Resources.discord_clientId },
                { "client_secret", Properties.Resources.discord_secret },
                { "refresh_token", _settings.Sync.AUTH_REFRESH },
                { "grant_type", "refresh_token" }
            };

            string postData = http_build_query(nvc);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/oauth2/token");
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.Accept = "application/json";
            req.Headers.Add("authorization", "Bearer ");
            string responseMessage = "";

            try
            {
                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    await sw.WriteAsync(postData);
                    sw.Close();
                    var httpResponse = (HttpWebResponse)req.GetResponse();
                    using (StreamReader sr = new StreamReader(httpResponse.GetResponseStream()))
                        responseMessage = await sr.ReadToEndAsync();
                }

                return JsonConvert.DeserializeObject<SyncData.Discord_Refresh>(responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, postData, HttpStatusCode.InternalServerError);
                return null;
            }
        }

        public string http_build_query(NameValueCollection nvc)
        {
            string postData = "";
            foreach (string key in nvc)
            {
                if (postData == string.Empty)
                    postData += string.Format("{0}={1}", key, nvc.Get(key));
                else
                    postData += string.Format("&{0}={1}", key, nvc.Get(key));
            }
            return postData;
        }
    }
}
