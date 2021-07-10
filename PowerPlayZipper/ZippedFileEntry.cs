using System;
using System.IO;

using PowerPlayZipper.Internal;

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
            $"{this.NormalizedFileName}: CompressedSize={this.CompressedSize.ToByteSize()}, OriginalSize={this.OriginalSize.ToByteSize()}, Crc32=0x{this.Crc32:x8}";
    }
}
