using System;
using System.IO;

#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Text;
#endif

using PowerPlayZipper.Internal;

namespace PowerPlayZipper.Compatibility
{
    public static class FileSystemAccessor
    {
#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
        private static readonly bool isOnWindows =
            Environment.OSVersion.Platform == PlatformID.Win32NT;

        private const int FACILITY_WIN32 = 7;
        private const int ERROR_ALREADY_EXISTS = 183;

        private static void ThrowWin32Error(int errorCode)
        {
            if (errorCode >= 1)
            {
                errorCode = (int)(((uint)errorCode & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000);
            }

            Marshal.ThrowExceptionForHR(errorCode);

            throw new InvalidOperationException();
        }

        private static void ThrowWin32Error() =>
            ThrowWin32Error(Marshal.GetLastWin32Error());

        private static string Win32GetLongFilePath(string path)
        {
            if (path.StartsWith(@"\\?\"))
            {
                return path;
            }

            var sanitizedPath = path.
                Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            var fullPath = new StringBuilder(NativeMethods.MaxLongPath);
            if (NativeMethods.Win32GetFullPathName(
                sanitizedPath, NativeMethods.MaxLongPath, fullPath, IntPtr.Zero) == 0)
            {
                ThrowWin32Error();
            }

            if ((fullPath.Length >= 3) &&
                (fullPath[0] == '\\') &&
                (fullPath[1] == '\\'))
            {
                fullPath.Insert(0, @"\\?\UNC\");
            }
            else
            {
                fullPath.Insert(0, @"\\?\");
            }

            return fullPath.ToString();
        }

        public static string CombinePath(string path1, string path2)
        {
            if (isOnWindows)
            {
                if ((path2.Length >= 1) &&
                    ((path2[0] == Path.DirectorySeparatorChar) ||
                     (path2[0] == Path.AltDirectorySeparatorChar) ||
                     ((path2.Length >= 2) && (path2[1] == Path.VolumeSeparatorChar))))
                {
                    return path2;
                }

                if (path1.Length >= 1)
                {
                    var lastCh = path1[path1.Length - 1];
                    if (!((lastCh == Path.DirectorySeparatorChar) ||
                         (lastCh == Path.VolumeSeparatorChar) ||
                         (lastCh == Path.AltDirectorySeparatorChar)))
                    {
                        path1 += Path.DirectorySeparatorChar;
                    }
                }

                return path1 + path2;
            }
            else
            {
                return Path.Combine(path1, path2);
            }
        }

        public static void CreateDirectoryIfNotExist(string directoryPath)
        {
            if (isOnWindows)
            {
                if (string.IsNullOrEmpty(directoryPath))
                {
                    throw new ArgumentException();
                }

                var index = 0;
                if (directoryPath.StartsWith(@"\\?\"))
                {
                    index = 4;
                }
                else if (directoryPath.StartsWith(@"\\?\UNC\"))
                {
                    index = 8;
                }

                while (true)
                {
                    var nextIndex = directoryPath.IndexOf(Path.DirectorySeparatorChar, index);
                    var path = (nextIndex == -1) ?
                        directoryPath :
                        directoryPath.Substring(0, nextIndex);

                    var longPath = Win32GetLongFilePath(path);
                    if (!NativeMethods.Win32CreateDirectory(longPath, IntPtr.Zero))
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        if ((errorCode != 0) &&
                            (errorCode != ERROR_ALREADY_EXISTS))
                        {
                            ThrowWin32Error(errorCode);
                        }
                    }

                    if (nextIndex == -1)
                    {
                        break;
                    }

                    index = nextIndex + 1;
                }
            }
            else
            {
                if (!Directory.Exists(directoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void Win32DeleteDirectoryRecursive(string directoryPath)
        {
            using (var handle = NativeMethods.Win32FindFirstFile(
                CombinePath(directoryPath, "*"), out var findData))
            {
                if (handle.IsInvalid)
                {
                    ThrowWin32Error();
                }

                while (true)
                {
                    if ((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if ((findData.cFileName != ".") && (findData.cFileName != ".."))
                        {
                            var childPath = CombinePath(directoryPath, findData.cFileName);
                            Win32DeleteDirectoryRecursive(childPath);
                        }
                    }
                    else
                    {
                        var childPath = CombinePath(directoryPath, findData.cFileName);
                        if (!NativeMethods.Win32DeleteFile(childPath))
                        {
                            ThrowWin32Error();
                        }
                    }

                    if (!NativeMethods.Win32FindNextFile(handle, ref findData))
                    {
                        break;
                    }
                }
            }

            if (!NativeMethods.Win32RemoveDirectory(directoryPath))
            {
                ThrowWin32Error();
            }
        }

        public static void DeleteDirectoryRecursive(string directoryPath)
        {
            if (isOnWindows)
            {
                var longPath = Win32GetLongFilePath(directoryPath);
                Win32DeleteDirectoryRecursive(longPath);
            }
            else
            {
                Directory.Delete(directoryPath, true);
            }
        }

        private static Stream Win32OpenForReadFile(string path, int recommendedBufferSize)
        {
            var longPath = Win32GetLongFilePath(path);
            
            var handle = NativeMethods.Win32CreateFile(
                longPath, NativeMethods.FileSystemRights.GenericRead,
                FileShare.Read, IntPtr.Zero, FileMode.Open, FileOptions.None, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                ThrowWin32Error();
            }

            return new FileStream(handle, FileAccess.Read, recommendedBufferSize);
        }

        public static Stream OpenForReadFile(string path, int recommendedBufferSize) =>
            isOnWindows ?
                Win32OpenForReadFile(path, recommendedBufferSize) :
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        private static Stream Win32OpenForWriteFile(string path, int recommendedBufferSize)
        {
            var longPath = Win32GetLongFilePath(path);

            var handle = NativeMethods.Win32CreateFile(
                longPath,
                NativeMethods.FileSystemRights.GenericRead | NativeMethods.FileSystemRights.GenericWrite,
                FileShare.None,
                IntPtr.Zero,
                FileMode.Create,
                FileOptions.SequentialScan,
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                ThrowWin32Error();
            }

            return new FileStream(handle, FileAccess.Write, recommendedBufferSize);
        }

        public static Stream OpenForWriteFile(string path, int recommendedBufferSize) =>
            isOnWindows ?
                Win32OpenForWriteFile(path, recommendedBufferSize) :
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, recommendedBufferSize);
#else
        public static string CombinePath(string path1, string path2) =>
            Path.Combine(path1, path2);

        public static void CreateDirectoryIfNotExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch
                {
                }
            }
        }

        public static void DeleteDirectoryRecursive(string directoryPath) =>
            Directory.Delete(directoryPath, true);

        public static Stream OpenForReadFile(string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        public static Stream OpenForWriteFile(string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, recommendedBufferSize);
#endif
    }
}
