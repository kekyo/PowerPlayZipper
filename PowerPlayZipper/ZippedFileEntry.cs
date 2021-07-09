using System;
using System.IO;

namespace PowerPlayZipper
{
    public sealed class ZippedFileEntry
    {
        public readonly string FileName;
        public readonly CompressionMethods CompressionMethod;
        public readonly long CompressedSize;
        public readonly long OriginalSize;
        public readonly uint Crc32;
        public readonly DateTime DateTime;

        public string NormalizedFileName =>
            this.FileName.
            Replace('\\', Path.DirectorySeparatorChar).
            Replace('/', Path.DirectorySeparatorChar);

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

        public override string ToString() =>
            $"{this.NormalizedFileName}: Size=[{this.CompressedSize}/{this.OriginalSize}], Crc32=0x{this.Crc32:x8}";
    }
}
