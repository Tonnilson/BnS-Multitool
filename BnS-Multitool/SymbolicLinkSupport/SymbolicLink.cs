﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Wrapper by Michael M
/// https://github.com/michaelmelancon/symboliclinksupport
/// </summary>
namespace SymbolicLinkSupport
{
    internal static class SymbolicLink
    {
        private const uint genericReadAccess = 0x80000000;

        private const uint fileFlagsForOpenReparsePointAndBackupSemantics = 0x02200000;
        /// <summary>
        /// Flag to indicate that the reparse point is relative
        /// </summary>
        /// <remarks>
        /// This is SYMLINK_FLAG_RELATIVE from from ntifs.h
        /// See https://msdn.microsoft.com/en-us/library/cc232006.aspx
        /// </remarks>
        private const uint symlinkReparsePointFlagRelative = 0x00000001;

        private const int ioctlCommandGetReparsePoint = 0x000900A8;

        private const uint openExisting = 0x3;

        private const uint pathNotAReparsePointError = 0x80071126;

        private const uint shareModeAll = 0x7; // Read, Write, Delete

        private const uint symLinkTag = 0xA000000C;

        private const int targetIsAFile = 0;

        private const int targetIsADirectory = 1;

        /// <summary>
        /// The maximum number of characters for a relative path, using Unicode 2-byte characters.
        /// </summary>
        /// <remarks>
        /// <para>This is the same as the old MAX_PATH value, because:</para>
        /// <para>
        /// "you cannot use the "\\?\" prefix with a relative path, relative paths are always limited to a total of MAX_PATH characters"
        /// </para>
        /// (https://docs.microsoft.com/en-us/windows/desktop/fileio/naming-a-file#maximum-path-length-limitation)
        /// 
        /// This value includes allowing for a terminating null character.
        /// </remarks>
        private const int maxRelativePathLengthUnicodeChars = 260;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool PathRelativePathToW(
            StringBuilder pszPath,
            string pszFrom,
            FileAttributes dwAttrFrom,
            string pszTo,
            FileAttributes dwAttrTo);

        public static void CreateDirectoryLink(string linkPath, string targetPath)
        {
            CreateDirectoryLink(linkPath, targetPath, false);
        }

        public static void CreateDirectoryLink(string linkPath, string targetPath, bool makeTargetPathRelative)
        {
            if (makeTargetPathRelative)
            {
                targetPath = GetTargetPathRelativeToLink(linkPath, targetPath, true);
            }

            if (!CreateSymbolicLink(linkPath, targetPath, targetIsADirectory) || Marshal.GetLastWin32Error() != 0)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }
        }

        public static void CreateFileLink(string linkPath, string targetPath)
        {
            CreateFileLink(linkPath, targetPath, false);
        }

        public static void CreateFileLink(string linkPath, string targetPath, bool makeTargetPathRelative)
        {
            if (makeTargetPathRelative)
            {
                targetPath = GetTargetPathRelativeToLink(linkPath, targetPath);
            }

            if (!CreateSymbolicLink(linkPath, targetPath, targetIsAFile))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        private static string GetTargetPathRelativeToLink(string linkPath, string targetPath, bool linkAndTargetAreDirectories = false)
        {
            string returnPath;

            FileAttributes relativePathAttribute = 0;
            if (linkAndTargetAreDirectories)
            {
                relativePathAttribute = FileAttributes.Directory;

                // set the link path to the parent directory, so that PathRelativePathToW returns a path that works
                // for directory symlink traversal
                linkPath = Path.GetDirectoryName(linkPath.TrimEnd(Path.DirectorySeparatorChar));
            }

            StringBuilder relativePath = new StringBuilder(maxRelativePathLengthUnicodeChars);
            if (!PathRelativePathToW(relativePath, linkPath, relativePathAttribute, targetPath, relativePathAttribute))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                returnPath = targetPath;
            }
            else
            {
                returnPath = relativePath.ToString();
            }

            return returnPath;

        }

        public static bool Exists(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                return false;
            }
            string target = GetTarget(path);
            return target != null;
        }

        private static SafeFileHandle GetFileHandle(string path)
        {
            return CreateFile(path, genericReadAccess, shareModeAll, IntPtr.Zero, openExisting,
                fileFlagsForOpenReparsePointAndBackupSemantics, IntPtr.Zero);
        }

        public static string GetTarget(string path)
        {
            SymbolicLinkReparseData reparseDataBuffer;

            using (SafeFileHandle fileHandle = GetFileHandle(path))
            {
                if (fileHandle.IsInvalid)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
#if NET35
                int outBufferSize = Marshal.SizeOf(typeof(SymbolicLinkReparseData));
#else
                int outBufferSize = Marshal.SizeOf<SymbolicLinkReparseData>();
#endif
                IntPtr outBuffer = IntPtr.Zero;
                try
                {
                    outBuffer = Marshal.AllocHGlobal(outBufferSize);
                    bool success = DeviceIoControl(
                        fileHandle.DangerousGetHandle(), ioctlCommandGetReparsePoint, IntPtr.Zero, 0,
                        outBuffer, outBufferSize, out int bytesReturned, IntPtr.Zero);

                    fileHandle.Dispose();

                    if (!success)
                    {
                        if (((uint)Marshal.GetHRForLastWin32Error()) == pathNotAReparsePointError)
                        {
                            return null;
                        }
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

#if NET35
                    reparseDataBuffer = (SymbolicLinkReparseData)Marshal.PtrToStructure(
                        outBuffer, typeof(SymbolicLinkReparseData));
#else
                    reparseDataBuffer = Marshal.PtrToStructure<SymbolicLinkReparseData>(outBuffer);
#endif
                }
                finally
                {
                    Marshal.FreeHGlobal(outBuffer);
                }
            }
            if (reparseDataBuffer.ReparseTag != symLinkTag)
            {
                return null;
            }

            string target = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                reparseDataBuffer.PrintNameOffset, reparseDataBuffer.PrintNameLength);

            if ((reparseDataBuffer.Flags & symlinkReparsePointFlagRelative) == symlinkReparsePointFlagRelative)
            {
                string basePath = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar));
                string combinedPath = Path.Combine(basePath, target);
                target = Path.GetFullPath(combinedPath);
            }

            return target;
        }
    }
}