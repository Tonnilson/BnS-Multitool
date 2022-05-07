using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BnS_Multitool.Extensions;
using System.IO.Compression;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using static BnS_Multitool.Functions.Crypto;

namespace BnS_Multitool
{
    public class SyncClient
    {
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

        public SyncClient(string authkey)
        {
            this.Auth_Token = authkey;
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

            string WEB_ADDR = string.Format("http://sync.bns.tools/xmls/?{0}", postData);
            try
            {
                using (GZipWebClient client = new GZipWebClient())
                    return await client.DownloadStringTaskAsync(new Uri(WEB_ADDR));
            } catch (WebException ex)
            {
                Logger.log.Error("Sync::Fetch_XMLS::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                return "";
            }
        }

        /// <summary>
        /// Convert unix timestamp to systems local UTC time.
        /// </summary>
        /// <param name="unixstamp"></param>
        /// <returns>Local UTC Time</returns>
        public DateTime UnixToDateTime(long unixstamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixstamp).ToLocalTime();

        public async Task<string> PublishXMLRequest(string postData)
        {
            HttpWebRequest client = (HttpWebRequest)WebRequest.Create("http://sync.bns.tools/publish/?action=publish");
            client.Method = "POST";
            client.ContentType = "application/json";
            client.Accept = client.ContentType;
            string response = "";

            try
            {
                using (StreamWriter sw = new StreamWriter(client.GetRequestStream()))
                {
                    await sw.WriteLineAsync(postData);
                    sw.Close();

                    var httpResponse = (HttpWebResponse)client.GetResponse();
                    using (StreamReader sr = new StreamReader(httpResponse.GetResponseStream()))
                        response = await sr.ReadToEndAsync();
                }
            } catch (WebException we)
            {
                Logger.log.Error("Synce::PublishXMLRequest::{0}\n{1}", we.ToString(), we.StackTrace);
                response = ((HttpWebResponse)we.Response).StatusDescription;
            }

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
            if(this.Discord == null && SyncConfig.Synced != null)
            {
                if (!SyncConfig.Synced.Any(x => x.ID == xml.ID)) return false;
                if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync",xml.Discord_id.ToString(), xml.Name + "." + xml.Type))) return false;
                if (!SyncConfig.Synced.Any(x => x.ID == xml.ID && x.Hash == xml.Hash)) return false;
                if (MD5_File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync",xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
            } else if (SyncConfig.Synced != null && xml.Discord_id != this.Discord.id)
            {
                if (!SyncConfig.Synced.Any(x => x.ID == xml.ID)) return false;
                if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync",xml.Discord_id.ToString(), xml.Name + "." + xml.Type))) return false;
                if (!SyncConfig.Synced.Any(x => x.ID == xml.ID && x.Hash == xml.Hash)) return false;
                if (MD5_File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync",xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
            } else if (this.Discord != null && xml.Discord_id == this.Discord.id)
            {
                bool exists = System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", xml.Name + "." + xml.Type)) ||
                    System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type));
                if (!exists) return false;
                else
                {
                    if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", xml.Name + "." + xml.Type)))
                        if (MD5_File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", xml.Name + "." + xml.Type)) != xml.Hash) return false;
                    if(System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)))
                        if (MD5_File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "manager", "sync", xml.Discord_id.ToString(), xml.Name + "." + xml.Type)) != xml.Hash) return false;
                }
            } else if (SyncConfig.Synced == null && this.Discord != null && xml.Discord_id != this.Discord.id) return false;
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
            string WEB_ADDR = string.Format("http://sync.bns.tools/xmls/?id={0}&full{1}", id, download ? "&download" : string.Empty);
            string response = "";
            try
            {
                using (GZipWebClient client = new GZipWebClient())
                    response = await client.DownloadStringTaskAsync(WEB_ADDR);
            } catch (Exception ex)
            {
                Logger.log.Error("Sync::Fetch_XMLAsync::Type: {0}\n{1}{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
            }
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
            Regex regexAppendBuffer = new Regex(string.Format("<decision><condition skill=\"({0})\"\\/><(.*?)key-status=\"unpress\"(.*?)<\\/decision>", string.Join("|",Illegal_Ids)), RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
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

            string WEB_ADDR = string.Format("http://sync.bns.tools/publish/{0}", postData);
            string response = "";
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "binary/octet-stream");
                try
                {
                    response = Encoding.UTF8.GetString(await client.UploadFileTaskAsync(WEB_ADDR, "POST", this.File));
                } catch (WebException we)
                {
                    Logger.log.Error("Sync::Upload::{0}\n{1}", we.ToString(), we.StackTrace);
                    response = ((HttpWebResponse)we.Response).StatusDescription;
                }
            }

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
    }
}
