using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BnS_Multitool.Functions
{
    static class Crypto
    {
        public static string SHA1_File(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (SHA1Managed sha1 = new SHA1Managed())
                    {
                        byte[] hash = sha1.ComputeHash(bs);
                        StringBuilder formatted = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                        {
                            formatted.AppendFormat("{0:x2}", b);
                        }
                        return formatted.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("Crypto::SHA1_File::Type {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                return string.Empty;
            }
        }

        public static string MD5_File(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
