using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BnS_Multitool
{
    class Globals
    {
        public static string localBnSVersion;
        public static string onlineBnSVersion;
        public static bool loginAvailable;
        private static string loginServer = "updater.nclauncher.ncsoft.com";

        public static string onlineVersionNumber()
        {
            int version = 0;
            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)6);
                bw.Write((byte)10);
                bw.Write((byte)"BnS".Length);
                bw.Write(Encoding.ASCII.GetBytes("BnS"));
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
            IniHandler VersionInfo_BnS = new IniHandler(Path.Combine(SystemConfig.SYS.BNS_DIR, "VersionInfo_BnS.ini"));
            localBnSVersion = VersionInfo_BnS.Read("VersionInfo", "GlobalVersion");
            onlineBnSVersion = onlineVersionNumber();

            if (onlineBnSVersion == "")
                onlineBnSVersion = localBnSVersion;
        }

        public static bool isLoginAvailable()
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                NetworkStream ns = new TcpClient(loginServer, 27500).GetStream();

                bw.Write((short)0);
                bw.Write((short)4);
                bw.Write((byte)10);
                bw.Write((byte)"BnS".Length);
                bw.Write(Encoding.ASCII.GetBytes("BnS"));
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
            catch (Exception ex)
            {
                loginAvailable = false;
                return false;
            }
        }
    }
}
