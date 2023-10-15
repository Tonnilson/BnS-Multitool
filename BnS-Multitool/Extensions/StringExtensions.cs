using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnS_Multitool.Extensions
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static bool DatFilePathMatches(this string input, string compare)
        {
            return input.Substring(input.IndexOf('\\') + 1).Split(new char[] { ':' })[0].StartsWith(compare);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }
    }
}
