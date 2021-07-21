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

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    public sealed class ZippedFileEntry
    {
        public ZippedFileEntry(
            string fileName,
            CompressionMethods compressionMethod,
            long compressedSize, long originalSize, uint crc32, DateTime dateTime)
        {
            this.FileName = fileName;
            this.CompressionMethod = compressionMethod;
            this.CompressedSize = compressedSize;
            this.OriginalSize = originalSize;
            this.Crc32 = crc32;
            this.DateTime = dateTime;
        }

        public string FileName { get; }
        public CompressionMethods CompressionMethod { get; }
        public long CompressedSize { get; }
        public long OriginalSize { get; }
        public uint Crc32 { get; }
        public DateTime DateTime { get; }

        public string NormalizedFileName =>
            this.FileName.
            Replace('\\', Path.DirectorySeparatorChar).
            Replace('/', Path.DirectorySeparatorChar);

        public override string ToString() =>
            $"\"{this.NormalizedFileName}\": CompressedSize={this.CompressedSize.ToBinaryPrefixString()}, OriginalSize={this.OriginalSize.ToBinaryPrefixString()}, Crc32=0x{this.Crc32:x8}";
    }
}
