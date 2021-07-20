using System;
using System.IO;

#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
#endif

namespace PowerPlayZipper.Internal
{
    internal static class NativeMethods
    {
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
        public const int MaxLongPath = 32700;
        
        [Flags]
        public enum FileSystemRights
        {
            GenericRead = 0x00020089,
            GenericWrite = 0x00000116
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern SafeFileHandle Win32CreateFile(
            string path,
            FileSystemRights dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            FileOptions dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "GetFullPathNameW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern uint Win32GetFullPathName(
            string path,
            uint nBufferLength,
            StringBuilder lpBuffer,
            IntPtr lpFilePart);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateDirectoryW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool Win32CreateDirectory(
            string path,
            IntPtr lpSecurityAttributes);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FindData
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public sealed class SafeFindFileHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindClose", SetLastError = true)]
            [SuppressUnmanagedCodeSecurity]
            private static extern bool Win32FindClose(IntPtr handle);

            public SafeFindFileHandle() :
                base(true)
            { }

            protected override bool ReleaseHandle() =>
                Win32FindClose(handle);
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindFirstFileW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern SafeFindFileHandle Win32FindFirstFile(string path, out FindData findFileData);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindNextFileW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool Win32FindNextFile(SafeFindFileHandle handle, ref FindData findFileData);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "DeleteFileW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool Win32DeleteFile(string path);

        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "RemoveDirectoryW", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool Win32RemoveDirectory(string path);
#endif
    }
}
