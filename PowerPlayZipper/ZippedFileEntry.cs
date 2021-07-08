using System;
using System.IO;
using System.IO.Compression;

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

        internal readonly long StreamPosition;

        public string NormalizedFileName =>
            this.FileName.
            Replace('\\', Path.DirectorySeparatorChar).
            Replace('/', Path.DirectorySeparatorChar);

        internal ZippedFileEntry(
            string fileName,
            CompressionMethods compressionMethod,
            long compressedSize, long originalSize,
            uint crc32, DateTime dateTime, long streamPosition)
        {
            this.FileName = fileName;
            this.CompressionMethod = compressionMethod;
            this.CompressedSize = compressedSize;
            this.OriginalSize = originalSize;
            this.Crc32 = crc32;
            this.DateTime = dateTime;
            this.StreamPosition = streamPosition;
        }

        public override string ToString() =>
            $"{this.NormalizedFileName}: Size=[{this.CompressedSize}/{this.OriginalSize}], Crc32=0x{this.Crc32:x8}";
    }
}
