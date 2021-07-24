///////////////////////////////////////////////////////////////////////////
//
// PowerPlayZipper - An implementation of Lightning-Fast Zip file
// compression/decompression library on .NET.
// Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using PowerPlayZipper.Internal;

namespace PowerPlayZipper.Utilities
{
    public static class FileSystemAccessor
    {
        private const int ERROR_ALREADY_EXISTS = 183;

        public static void CreateDirectoryIfNotExist(object basePath)
        {
            throw new NotImplementedException();
        }

#if NETFRAMEWORK
        // On Windows or mono
        private static readonly bool isOnWindowsNetFx =
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#elif NETSTANDARD
        // On Windows or mono or .NET Core
        private static readonly bool isOnWindowsNetFx =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
#endif

#if NETFRAMEWORK || NETSTANDARD
        private const int NO_ERROR = 0;
        private const int ERROR_ACCESS_DENIED = 5;
        private const int FACILITY_WIN32 = 7;

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

        public static string CombinePath(
            string path1, string path2)
        {
            if (isOnWindowsNetFx)
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

        public static string GetDirectoryName(string path)
        {
            if (isOnWindowsNetFx)
            {
                var index = path.LastIndexOfAny(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                if (index >= 0)
                {
                    return path.Substring(0, index);
                }

                var indexd = path.IndexOf(Path.VolumeSeparatorChar);
                if (indexd >= 0)
                {
                    return path.Substring(0, indexd);
                }

                return string.Empty;
            }
            else
            {
                return Path.GetDirectoryName(path)!;
            }
        }

        public static void CreateDirectoryIfNotExist(
            string directoryPath)
        {
            if (isOnWindowsNetFx)
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
                        switch (errorCode)
                        {
                            case NO_ERROR:
                            case ERROR_ALREADY_EXISTS:
                                break;
                            case ERROR_ACCESS_DENIED when nextIndex != -1:
                                break;
                            default:
                                ThrowWin32Error(errorCode);
                                break;
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

        private static void Win32DeleteDirectoryRecursive(
            string directoryPath)
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

        public static void DeleteDirectoryRecursive(
            string directoryPath)
        {
            if (isOnWindowsNetFx)
            {
                var longPath = Win32GetLongFilePath(directoryPath);
                Win32DeleteDirectoryRecursive(longPath);
            }
            else
            {
                Directory.Delete(directoryPath, true);
            }
        }

        private static Stream Win32OpenForReadFile(
            string path, int recommendedBufferSize)
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

        public static Stream OpenForReadFile(
            string path, int recommendedBufferSize) =>
            isOnWindowsNetFx ?
                Win32OpenForReadFile(path, recommendedBufferSize) :
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        private static Stream? Win32OpenForWriteFile(
            string path, bool overwrite, int recommendedBufferSize)
        {
            var longPath = Win32GetLongFilePath(path);

            var handle = NativeMethods.Win32CreateFile(
                longPath,
                NativeMethods.FileSystemRights.GenericRead | NativeMethods.FileSystemRights.GenericWrite,
                FileShare.None,
                IntPtr.Zero,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileOptions.SequentialScan,
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode == ERROR_ALREADY_EXISTS)
                {
                    return null;
                }
                else
                {
                    ThrowWin32Error(errorCode);
                }
            }

            return new FileStream(handle, FileAccess.Write, recommendedBufferSize);
        }

        public static Stream OpenForOverwriteFile(
            string path, int recommendedBufferSize) =>
            isOnWindowsNetFx ?
                Win32OpenForWriteFile(path, true, recommendedBufferSize)! :
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, recommendedBufferSize);

        public static Stream? OpenForWriteFile(
            string path, int recommendedBufferSize)
        {
            if (isOnWindowsNetFx)
            {
                return Win32OpenForWriteFile(path, false, recommendedBufferSize);
            }
            else
            {
                try
                {
                    return new FileStream(
                        path, FileMode.CreateNew, FileAccess.Write, FileShare.None, recommendedBufferSize);
                }
                catch (IOException)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == ERROR_ALREADY_EXISTS)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static IEnumerable<PathEntry> Win32EnumeratePathsRecursive(string basePath)
        {
            using (var handle = NativeMethods.Win32FindFirstFile(
                CombinePath(basePath, "*"), out var findData))
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
                            var childPath = CombinePath(basePath, findData.cFileName);
                            yield return new PathEntry(childPath, true);
                            foreach (var result in Win32EnumeratePathsRecursive(childPath))
                            {
                                yield return result;
                            }
                        }
                    }
                    else
                    {
                        var childPath = CombinePath(basePath, findData.cFileName);
                        yield return new PathEntry(childPath, false);
                    }

                    if (!NativeMethods.Win32FindNextFile(handle, ref findData))
                    {
                        break;
                    }
                }
            }
        }

        private static IEnumerable<PathEntry> EnumeratePathsRecursive(string basePath)
        {
#if NET20 || NET35
            foreach (var path in Directory.GetDirectories(basePath))
#else
            foreach (var path in Directory.EnumerateDirectories(basePath))
#endif
            {
                yield return new PathEntry(path, true);
                foreach (var result in EnumeratePathsRecursive(path))
                {
                    yield return result;
                }
            }

#if NET20 || NET35
            foreach (var path in Directory.GetFiles(basePath))
#else
            foreach (var path in Directory.EnumerateFiles(basePath))
#endif
            {
                yield return new PathEntry(path, false);
            }
        }

        public static IEnumerable<PathEntry> EnumeratePaths(string basePath) =>
            isOnWindowsNetFx ?
                Win32EnumeratePathsRecursive(basePath) :
                EnumeratePathsRecursive(basePath);
#else
        public static string CombinePath(
            string path1, string path2) =>
            Path.Combine(path1, path2);

        public static string GetDirectoryName(string path) =>
            Path.GetDirectoryName(path)!;

        public static void CreateDirectoryIfNotExist(
            string directoryPath)
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

        public static void DeleteDirectoryRecursive(
            string directoryPath) =>
            Directory.Delete(directoryPath, true);

        public static Stream OpenForReadFile(
            string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        public static Stream OpenForOverwriteFile(
            string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, recommendedBufferSize);

        public static Stream? OpenForWriteFile(
            string path, int recommendedBufferSize)
        {
            try
            {
                return new FileStream(
                    path, FileMode.CreateNew, FileAccess.Write, FileShare.None, recommendedBufferSize);
            }
            catch (IOException)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode == ERROR_ALREADY_EXISTS)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        private static IEnumerable<PathEntry> EnumeratePathsRecursive(string basePath)
        {
            foreach (var path in Directory.EnumerateDirectories(basePath))
            {
                yield return new PathEntry(path, true);
                foreach (var result in EnumeratePathsRecursive(path))
                {
                    yield return result;
                }
            }

            foreach (var path in Directory.EnumerateFiles(basePath))
            {
                yield return new PathEntry(path, false);
            }
        }

        public static IEnumerable<PathEntry> EnumeratePaths(string basePath) =>
            EnumeratePathsRecursive(basePath);
#endif
    }
}
