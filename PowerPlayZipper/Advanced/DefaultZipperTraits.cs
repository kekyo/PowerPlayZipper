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
using System.Text.RegularExpressions;

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Advanced
{
    public class DefaultZipperTraits : IZipperTraits
    {
        public readonly string BasePath;
        public readonly string ZipFilePath;
        public readonly Regex? RegexPattern;

        public DefaultZipperTraits(
            string basePath, string zipFilePath, string? regexPattern = null)
        {
            this.BasePath = basePath;
            this.ZipFilePath = zipFilePath;
            this.RegexPattern = CompilePattern(regexPattern);
        }

        internal static Regex? CompilePattern(string? regexPattern) =>
#if NET20 || NET35
            string.IsNullOrEmpty(regexPattern) ? null : new Regex(regexPattern!, RegexOptions.Compiled);
#else
            string.IsNullOrWhiteSpace(regexPattern) ? null : new Regex(regexPattern!, RegexOptions.Compiled);
#endif

        public virtual void Started()
        {
        }

        public virtual IEnumerable<PathEntry> EnumerateTargetPaths() =>
            FileSystemAccessor.EnumeratePaths(this.BasePath);

        public virtual bool IsRequiredProcessing(PathEntry entry) =>
            this.RegexPattern?.IsMatch(entry.Path) ?? true;

        public virtual Stream OpenForReadFile(string path, int recommendedBufferSize) =>
            FileSystemAccessor.OpenForReadFile(path, recommendedBufferSize);

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            FileSystemAccessor.CombinePath(this.BasePath, entry.NormalizedFileName);

        public virtual string GetDirectoryName(string path) =>
            FileSystemAccessor.GetDirectoryName(path);

        public virtual void CreateDirectoryIfNotExist(string directoryPath) =>
            FileSystemAccessor.CreateDirectoryIfNotExist(directoryPath);

        public virtual Stream OpenForWriteZipFile(int recommendedBufferSize) =>
            FileSystemAccessor.OpenForOverwriteFile(this.ZipFilePath, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position)
        {
        }

        public virtual void Finished()
        {
        }
    }
}
