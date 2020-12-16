using System.Runtime.InteropServices;
using System.Text;

namespace BnS_Multitool
{
    public class IniHandler
    {
        public string path;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);

        public IniHandler(string INIPath)
        {
            path = INIPath;
        }

        public void Write(string Section, string Key, string Value) => WritePrivateProfileString(Section, Key, Value, this.path);

        public string Read(string Section, string Key)
        {
            StringBuilder builder = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", builder, 255, this.path);
            return builder.ToString();
        }
    }
}
