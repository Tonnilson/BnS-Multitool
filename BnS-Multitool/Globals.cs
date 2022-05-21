using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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

        public static string onlineVersionNumber()
        {
            int version = 0;
            try
            {
                refreshServerVar();
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(BnS_ServerInfo.LoginServer, 27500).GetStream();

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
                version = br.ReadByte();
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

        public static void GameVersionCheck()
        {
            refreshServerVar();
            IniHandler VersionInfo_BnS = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR, string.Format("VersionInfo_{0}.ini", BnS_ServerInfo.Name)));
            localBnSVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            onlineBnSVersion = onlineVersionNumber();

            if (onlineBnSVersion == "")
                onlineBnSVersion = localBnSVersion;
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

                ms.Position = 9L;
                br.ReadBytes(br.ReadByte() + 1);
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
