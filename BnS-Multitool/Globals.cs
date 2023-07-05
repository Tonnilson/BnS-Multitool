using BnS_Multitool.Extensions;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace BnS_Multitool
{
    class Globals
    {
        public class Region_Info
        {
            public string Name;
            public string LoginServer;
            public string CDN;
        }

        public static string localBnSVersion;
        public static string onlineBnSVersion;
        public static bool loginAvailable;
        public static string MAIN_SERVER_ADDR = @"http://multitool.tonic.pw/";
        public static Region_Info BnS_ServerInfo = new Region_Info();

        public enum BnS_Region
        {
            [Description("NA")]
            NA,
            [Description("EU")]
            EU,
            [Description("TW")]
            TW,
            [Description("KR")]
            KR,
            [Description("KR Test")]
            TEST,
            [Description("NA/EU")] // Why?
            NAEU
        }

        public static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }

        private static void refreshServerVar()
        {
            switch((BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case BnS_Region.KR:
                    BnS_ServerInfo.LoginServer = "up4svr.ncupdate.com";
                    BnS_ServerInfo.Name = "BNS_LIVE";
                    BnS_ServerInfo.CDN = @"http://bnskor.ncupdate.com/BNS_LIVE/";
                    break;
                case BnS_Region.TW:
                    BnS_ServerInfo.LoginServer = "up4svr.plaync.com.tw";
                    BnS_ServerInfo.Name = "TWBNSUE4";
                    BnS_ServerInfo.CDN = @"http://mmorepo.cdn.plaync.com.tw/TWBNSUE4/";
                    break;
                case BnS_Region.TEST:
                    BnS_ServerInfo.LoginServer = "up4svr.plaync.com";
                    BnS_ServerInfo.Name = "BNS_KOR_TEST";
                    BnS_ServerInfo.CDN = @"http://bnskor.ncupdate.com/BNS_LIVE/";
                    break;
                default:
                    BnS_ServerInfo.LoginServer = "updater.nclauncher.ncsoft.com";
                    BnS_ServerInfo.Name = "BnS_UE4";
                    BnS_ServerInfo.CDN = @"http://d37ob46rk09il3.cloudfront.net/BnS_UE4/";
                    break;
            }

            // This is an automated way to get the game update URL incase these above ever change.
            GetGameUpdateUrl();
        }

        // Potential solution to Accounts & settings.json being nulled out when system unexpectedly crashes or restarts
        // Apparently all contents through System.IO.File.WriteAllText get flushed to the filesystem cache
        // tl;dr the file is written over with 0x00 and then the bytes are replaced
        // with the bytes from the filesystem cache but that cache is obviously lost on system restart / crash
        public static void WriteAllText(string path, string contents)
        {
            // generate a temp filename
            var tempPath = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString());
            var backup = path + ".backup";

            if (File.Exists(backup))
                File.Delete(backup);

            var data = Encoding.UTF8.GetBytes(contents);

            using (var tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            // We need to make sure the file exists otherwise it'll throw an error if we try to use File.Replace
            if(File.Exists(path))
                File.Replace(tempPath, path, backup);
            else
                File.Move(tempPath, path);
        }

        public static string languageFromSelection(int index)
        {
            string lang;
            switch (index)
            {
                case 1:
                    lang = "BPORTUGUESE";
                    break;
                case 2:
                    lang = "GERMAN";
                    break;
                case 3:
                    lang = "FRENCH";
                    break;
                case 4:
                    lang = "CHINESET";
                    break;
                default:
                    lang = "English";
                    break;
            }
            return lang;
        }

        // Write the desired audio localization, why is this controlled through the local.ini file ??
        public static void UpdateLocalization(int index)
        {
            string language = languageFromSelection(index).ToLower();

            if (File.Exists(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "local.ini")))
            {
                IniHandler localFile = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR, "BNSR", "Binaries", "Win64", "local.ini"));
                if(localFile.Read("Locale", "Language") != language)
                    localFile.Write("Locale", "Language", language);
            }
        }

        public class BUILD_RELAY_RESPONSE
        {
            public string BUILD_NUMBER { get; set; }
        }

        public static string onlineVersionNumber()
        {
            if (SystemConfig.SYS.BUILD_RELAY && (BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION != BnS_Region.TW)
            {
                WebClient client = new GZipWebClient();
                string build;
                try
                {
                    var response = client.DownloadString(MAIN_SERVER_ADDR + "build_relay.json");
                    build = JsonConvert.DeserializeObject<BUILD_RELAY_RESPONSE>(response).BUILD_NUMBER;
                }
                catch (Exception ex)
                {
                    Logger.log.Error("{0}\n{1}", ex.Message, ex.StackTrace);
                    new ErrorPrompt("There was an error retrieving the relay response, DNS Problem?", false).ShowDialog();
                    build = "";
                } finally { client.Dispose(); }
                return build;
            }
            else
            {
                int version = 0;
                try
                {
                    refreshServerVar();
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    NetworkStream ns = new TcpClient(BnS_ServerInfo.LoginServer, 27500).GetStream();

                    /*
                        For those that want to port this to a different language and don't know C#, this is the basic packet structure
                        size of packet, null, query type, null, 10 (0xA), length of game_name, game_name
                        (Byte array: 0x0D, 0x00, 0x06, 0x00, 0x0A, 0x07, 0x42, 0x6E, 0x53, 0x5F, 0x55, 0x45, 0x34)

                        query types:
                            1 = game list
                            2 = Launch params?
                            3 = BASE_UPDATE_URL
                            4 = in-game login service status
                            5 = info servicec
                            6 = Current Region info (Game build # etc)
                            7 = Future region info (same as above but prepatch?)
                            8 = info language
                            9 = info level update (deprecated)

                        game_name is just a region build identifier i.e BnS_UE4 for NCW live server
                    */
                    bw.Write((short)0);
                    bw.Write((short)6);
                    bw.Write((byte)10);
                    bw.Write((byte)BnS_ServerInfo.Name.Length);
                    bw.Write(Encoding.ASCII.GetBytes(BnS_ServerInfo.Name));
                    bw.BaseStream.Position = 0L;
                    bw.Write((short)ms.Length);

                    ns.Write(ms.ToArray(), 0, (int)ms.Length);
                    bw.Dispose();
                    ms.Dispose();

                    ms = new MemoryStream();
                    BinaryReader br = new BinaryReader(ms);

                    byte[] byte_array = new byte[1024];
                    int num = 0;

                    do
                    {
                        num = ns.Read(byte_array, 0, byte_array.Length);
                        if (num > 0)
                            ms.Write(byte_array, 0, num);
                    } while (num == byte_array.Length);

                    ms.Position = 9L;
                    br.ReadBytes(br.ReadByte() + 5);
                    version = br.ReadByte(); // This should be offset 0x22
                    // If build # exceeds 255 some extra stuff needs to be done
                    if (br.ReadInt16() != 40)
                    {
                        ms.Position -= 2;
                        version += 128 * (br.ReadByte() - 1);
                    }
                }
                catch (Exception ex)
                {
                    return "";
                }
                return version.ToString();
            }
        }

        public static void GameVersionCheck()
        {
            refreshServerVar();
            IniHandler VersionInfo_BnS = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR, string.Format("VersionInfo_{0}.ini", BnS_ServerInfo.Name)));
            localBnSVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion").Trim();
            onlineBnSVersion = onlineVersionNumber();

            if (onlineBnSVersion == "")
                onlineBnSVersion = localBnSVersion;
        }

        public static void GetGameUpdateUrl()
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(BnS_ServerInfo.LoginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)4);
                bw.Write((byte)10);
                bw.Write((byte)BnS_ServerInfo.Name.Length);
                bw.Write(Encoding.ASCII.GetBytes(BnS_ServerInfo.Name));
                bw.BaseStream.Position = 0L;
                bw.Write((short)ms.Length);

                ns.Write(ms.ToArray(), 0, (int)ms.Length);
                bw.Dispose();
                ms.Dispose();

                ms = new MemoryStream();
                BinaryReader br = new BinaryReader(ms);

                byte[] byte_array = new byte[1024];
                int num = 0;

                do
                {
                    num = ns.Read(byte_array, 0, byte_array.Length);
                    if (num > 0)
                        ms.Write(byte_array, 0, num);
                }
                while (num == byte_array.Length);

                br.ReadBytes(br.ReadByte() + 1);
                var url = br.ReadString();
                if (!url.IsNullOrEmpty())
                    BnS_ServerInfo.CDN = string.Format("http://{0}/{1}/", url, BnS_ServerInfo.Name);
            }
            catch (Exception)
            {
                
            }
        }

        public static bool isLoginAvailable()
        {
            try
            {
                refreshServerVar();
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(BnS_ServerInfo.LoginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)4);
                bw.Write((byte)10);
                bw.Write((byte)BnS_ServerInfo.Name.Length);
                bw.Write(Encoding.ASCII.GetBytes(BnS_ServerInfo.Name));
                bw.BaseStream.Position = 0L;
                bw.Write((short)ms.Length);

                ns.Write(ms.ToArray(), 0, (int)ms.Length);
                bw.Dispose();
                ms.Dispose();

                ms = new MemoryStream();
                BinaryReader br = new BinaryReader(ms);

                byte[] byte_array = new byte[1024];
                int num = 0;

                do
                {
                    num = ns.Read(byte_array, 0, byte_array.Length);
                    if (num > 0)
                        ms.Write(byte_array, 0, num);
                }
                while (num == byte_array.Length);

                ms.Position = ms.Length - 1;
                loginAvailable = br.ReadBoolean();
                return loginAvailable;
            }
            catch (Exception)
            {
                loginAvailable = false;
                return false;
            }
        }
    }
}
