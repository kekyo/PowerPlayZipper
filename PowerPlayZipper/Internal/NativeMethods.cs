using System;
using System.IO;

#if NETFRAMEWORK || NETSTANDARD
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;
#endif

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
using System.Security;
#endif

namespace PowerPlayZipper.Internal
{
#if NETSTANDARD1_3 || NETSTANDARD1_6
    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        private static readonly IntPtr minusOne =
            (IntPtr.Size == 8) ? new IntPtr(-1L) : new IntPtr(-1);

        public SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) :
            base(default, ownsHandle)
        { }

        public override bool IsInvalid =>
            (base.handle == IntPtr.Zero) || (base.handle == minusOne);
    }
#endif

#if NETFRAMEWORK || NETSTANDARD
    internal static class NativeMethods
    {
        public const int MaxLongPath = 32700;

        [Flags]
        public enum FileSystemRights
        {
            GenericRead = 0x00020089,
            GenericWrite = 0x00000116
        }

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern SafeFileHandle Win32CreateFile(
            string path,
            FileSystemRights dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            FileOptions dwFlagsAndAttributes,
            IntPtr hTemplateFile);

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "GetFullPathNameW", SetLastError = true)]
        public static extern uint Win32GetFullPathName(
            string path,
            uint nBufferLength,
            StringBuilder lpBuffer,
            IntPtr lpFilePart);

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateDirectoryW", SetLastError = true)]
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
#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
            [SuppressUnmanagedCodeSecurity]
#endif
            [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindClose", SetLastError = true)]
            private static extern bool Win32FindClose(IntPtr handle);

            public SafeFindFileHandle() :
                base(true)
            { }

            protected override bool ReleaseHandle() =>
                Win32FindClose(handle);
        }

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindFirstFileW", SetLastError = true)]
        public static extern SafeFindFileHandle Win32FindFirstFile(string path, out FindData findFileData);

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "FindNextFileW", SetLastError = true)]
        public static extern bool Win32FindNextFile(SafeFindFileHandle handle, ref FindData findFileData);

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "DeleteFileW", SetLastError = true)]
        public static extern bool Win32DeleteFile(string path);

#if NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1
        [SuppressUnmanagedCodeSecurity]
#endif
        [DllImport("kernel32", CharSet = CharSet.Unicode, EntryPoint = "RemoveDirectoryW", SetLastError = true)]
        public static extern bool Win32RemoveDirectory(string path);
    }
#endif
}
