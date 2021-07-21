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

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Migration
{
    internal sealed class ZipFileMigrationUnzipperTraits : DefaultUnzipperTraits
    {
        private readonly bool overwriteFiles;

        public ZipFileMigrationUnzipperTraits(
            string zipFilePath, string extractToBasePath, bool overwriteFiles) :
            base(zipFilePath, extractToBasePath) =>
            this.overwriteFiles = overwriteFiles;

        public override Stream? OpenForWriteFile(string path, int recommendedBufferSize) =>
            this.overwriteFiles ?
                FileSystemAccessor.OpenForOverwriteFile(
                    path, recommendedBufferSize) :
                FileSystemAccessor.OpenForWriteFile(
                    path, recommendedBufferSize);
    }
}
