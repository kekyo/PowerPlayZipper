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
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzipperTraits : IUnzipperTraits
    {
        public readonly string ZipFilePath;
        public readonly string ExtractToBasePath;
        public readonly Regex? RegexPattern;

        private readonly DirectoryConstructor directoryConstructor;

        public DefaultUnzipperTraits(
            string zipFilePath, string extractToBasePath,
            string? regexPattern = null)
        {
            this.ZipFilePath = zipFilePath;
            this.ExtractToBasePath = extractToBasePath;
            this.RegexPattern = CompilePattern(regexPattern);

            this.directoryConstructor = new(this.CreateDirectoryIfNotExist);
        }

        internal static Regex? CompilePattern(string? regexPattern) =>
#if NET20 || NET35
            string.IsNullOrEmpty(regexPattern) ? null : new Regex(regexPattern, RegexOptions.Compiled);
#else
            string.IsNullOrWhiteSpace(regexPattern) ? null : new Regex(regexPattern, RegexOptions.Compiled);
#endif

        public void Started()
        {
            this.directoryConstructor.Clear();
            this.OnStarted();
        }

        protected virtual void OnStarted()
        {
        }

        public virtual Stream OpenForReadZipFile(int recommendedBufferSize) =>
            FileSystemAccessor.OpenForReadFile(this.ZipFilePath, recommendedBufferSize);

        public virtual bool IsRequiredProcessing(ZippedFileEntry entry) =>
            this.RegexPattern?.IsMatch(entry.NormalizedFileName) ?? true;

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            FileSystemAccessor.CombinePath(this.ExtractToBasePath, entry.NormalizedFileName);

        public virtual string GetDirectoryName(string path) =>
            FileSystemAccessor.GetDirectoryName(path);

        public virtual void CreateDirectoryIfNotExist(string directoryPath) =>
            FileSystemAccessor.CreateDirectoryIfNotExist(directoryPath);

        public virtual Stream? OpenForWriteFile(string path, int recommendedBufferSize) =>
            FileSystemAccessor.OpenForOverwriteFile(path, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position)
        {
        }

        public void Finished()
        {
            this.directoryConstructor.Clear();
            this.OnFinished();
        }

        protected virtual void OnFinished()
        {
        }
    }
}
