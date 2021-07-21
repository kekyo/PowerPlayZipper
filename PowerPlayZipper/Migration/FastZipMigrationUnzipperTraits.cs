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

using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Migration
{
    internal sealed class FastZipMigrationUnzipperTraits : DefaultUnzipperTraits
    {
        private readonly Regex? directoryPattern;
        private readonly FastZip.Overwrite overwrite;
        private readonly FastZip.ConfirmOverwriteDelegate? comfirm;

        public FastZipMigrationUnzipperTraits(
            string zipFilePath, string extractToBasePath, string? filePattern, string? directoryPattern,
            FastZip.Overwrite overwrite, FastZip.ConfirmOverwriteDelegate? comfirm) :
            base(zipFilePath, extractToBasePath, filePattern)
        {
            this.directoryPattern = CompilePattern(directoryPattern);
            this.overwrite = overwrite;
            this.comfirm = comfirm;
        }

        public override bool IsRequiredProcessing(ZippedFileEntry entry) =>
            (entry.CompressionMethod == CompressionMethods.Directory) ?
                (this.directoryPattern?.IsMatch(entry.NormalizedFileName) ?? true) :
                (this.RegexPattern?.IsMatch(entry.NormalizedFileName) ?? true);

        public override Stream? OpenForWriteFile(string path, int recommendedBufferSize)
        {
            switch (this.overwrite)
            {
                case FastZip.Overwrite.Never:
                    return FileSystemAccessor.OpenForWriteFile(
                        path, recommendedBufferSize);
                case FastZip.Overwrite.Prompt:
                    var stream = FileSystemAccessor.OpenForWriteFile(
                        path, recommendedBufferSize);
                    if (stream == null)
                    {
                        if (this.comfirm?.Invoke(path) ?? false)
                        {
                            stream = FileSystemAccessor.OpenForOverwriteFile(
                                path, recommendedBufferSize);
                        }
                    }
                    return stream;
                default:
                    return FileSystemAccessor.OpenForOverwriteFile(
                        path, recommendedBufferSize);
            }
        }
    }
}
