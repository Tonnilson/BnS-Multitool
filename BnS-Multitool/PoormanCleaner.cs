using System;
using System.Runtime.InteropServices;

namespace BnS_Multitool
{
    class PoormanCleaner
    {
        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);
    }
}
