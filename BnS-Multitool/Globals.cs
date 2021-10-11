using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using CG.Web.MegaApiClient;

namespace BnS_Multitool
{
    class Globals
    {
        public static string localBnSVersion;
        public static string onlineBnSVersion;
        public static bool loginAvailable;
        private static string loginServer;
        private static string loginServerVar;
        public static string MAIN_SERVER_ADDR = @"http://multitool.tonic.pw/";

        public class BnS_Servers
        {
            public string region { get; set; }
            public List<BnS_Server_Details> info { get; set; }
        }

        public class BnS_Server_Details
        {
            public string loginServer { get; set; }
            public string loginVar { get; set; }
            public string CDN { get; set; }
        }

        public enum BnS_Region
        {
            NA,
            EU,
            TW,
            KR,
            TEST,
            NAEU
        }

        public static MegaApiClient MEGA_API_CLIENT = new MegaApiClient();

        private static void refreshServerVar()
        {
            switch((BnS_Region)ACCOUNT_CONFIG.ACCOUNTS.REGION)
            {
                case BnS_Region.KR:
                    loginServer = "up4svr.ncupdate.com";
                    loginServerVar = "BNS_LIVE";
                    break;
                case BnS_Region.TW:
                    loginServer = "up4svr.plaync.com.tw";
                    loginServerVar = "TWBNSUE4";
                    break;
                case BnS_Region.TEST:
                    loginServer = "up4svr.plaync.com";
                    loginServerVar = "BNS_KOR_TEST";
                    break;
                default:
                    loginServer = "updater.nclauncher.ncsoft.com";
                    loginServerVar = "BnS_UE4";
                    break;
            }
        }

        public static string onlineVersionNumber()
        {
            int version = 0;
            try
            {
                refreshServerVar();
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)6);
                bw.Write((byte)10);
                bw.Write((byte)loginServerVar.Length);
                bw.Write(Encoding.ASCII.GetBytes(loginServerVar));
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
            IniHandler VersionInfo_BnS = new IniHandler(Directory.GetFiles(SystemConfig.SYS.BNS_DIR, "VersionInfo_*.ini").FirstOrDefault());
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
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)4);
                bw.Write((byte)10);
                bw.Write((byte)loginServerVar.Length);
                bw.Write(Encoding.ASCII.GetBytes(loginServerVar));
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
