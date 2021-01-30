using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Security
{
    /*
     * Original: https://www.codeproject.com/Articles/28678/Generating-Unique-Key-Finger-Print-for-a-Computer
     * Modified for our use case
     */

    public class FingerPrint
    {
        private static string fingerPrint = string.Empty;

        public static string Value()
        {
            System.Diagnostics.Debug.WriteLine("Getting fingerprint");
            fingerPrint = string.Empty;
            if (string.IsNullOrEmpty(fingerPrint))
            {
                try
                {
                    fingerPrint = GetHash("CPU >> " + cpuId() + "\nBIOS >> " +
                        biosId() + "\nBASE >> " + baseId());
                }
                catch (Exception) { }
            }
            return GetHash(fingerPrint);
        }

        private static string GetHash(string s)
        {
            MD5 sec = new MD5CryptoServiceProvider();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bt = enc.GetBytes(s);
            return GetHexString(sec.ComputeHash(bt));
        }
        private static string GetHexString(byte[] bt)
        {
            string s = string.Empty;
            for (int i = 0; i < bt.Length; i++)
            {
                byte b = bt[i];
                int n, n1, n2;
                n = (int)b;
                n1 = n & 15;
                n2 = (n >> 4) & 15;
                if (n2 > 9)
                    s += ((char)(n2 - 10 + (int)'A')).ToString();
                else
                    s += n2.ToString();
                if (n1 > 9)
                    s += ((char)(n1 - 10 + (int)'A')).ToString();
                else
                    s += n1.ToString();
                //if ((i + 1) != bt.Length && (i + 1) % 2 == 0) s += "-";
            }
            return s;
        }

        private static string identifier(string wmiClass, string wmiProperty)
        {
            string text = "";
            foreach (ManagementObject instance in new ManagementClass(wmiClass).GetInstances())
            {
                if (text == "")
                {
                    try
                    {
                        if (instance[wmiProperty] == null)
                        {
                            return text;
                        }
                        text = instance[wmiProperty].ToString();
                        return text;
                    }
                    catch
                    {
                    }
                }
            }
            return text;
        }

        private static string cpuId()
        {
            string text = identifier("Win32_Processor", "UniqueId");
            if (text == "")
            {
                text = identifier("Win32_Processor", "ProcessorId");
                if (text == "")
                {
                    text = identifier("Win32_Processor", "Name");
                    if (text == "")
                    {
                        text = identifier("Win32_Processor", "Manufacturer");
                    }
                }
            }
            return text;
        }

        private static string biosId() => identifier("Win32_BIOS", "Manufacturer") + identifier("Win32_BIOS", "SMBIOSBIOSVersion") + identifier("Win32_BIOS", "IdentificationCode") + identifier("Win32_BIOS", "SerialNumber") + identifier("Win32_BIOS", "ReleaseDate") + identifier("Win32_BIOS", "Version");
        private static string baseId() => identifier("Win32_BaseBoard", "Model") + identifier("Win32_BaseBoard", "Manufacturer") + identifier("Win32_BaseBoard", "Name") + identifier("Win32_BaseBoard", "SerialNumber");
    }
}