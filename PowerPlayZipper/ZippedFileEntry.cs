using PowerPlayZipper.Internal;
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
        public readonly int Crc32;
        public readonly DateTime DateTime;

        internal readonly long StreamPosition;
        private readonly ReadOnlyRangedStreamFactory factory;

        public string NormalizedFileName =>
            this.FileName.
            Replace('\\', Path.DirectorySeparatorChar).
            Replace('/', Path.DirectorySeparatorChar);

        internal ZippedFileEntry(
            string fileName,
            CompressionMethods compressionMethod,
            long compressedSize, long originalSize,
            int crc32, DateTime dateTime, long streamPosition,
            ReadOnlyRangedStreamFactory factory)
        {
            this.FileName = fileName;
            this.CompressionMethod = compressionMethod;
            this.CompressedSize = compressedSize;
            this.OriginalSize = originalSize;
            this.Crc32 = crc32;
            this.DateTime = dateTime;
            this.StreamPosition = streamPosition;
            this.factory = factory;
        }

        public Stream GetDecompressedStream()
        {
            var compressedStream =
                this.factory.Rent(this.StreamPosition, this.CompressedSize);
            return this.CompressionMethod switch
            {
                CompressionMethods.Deflate => new DeflateStream(compressedStream, CompressionMode.Decompress, false),
                CompressionMethods.Stored => compressedStream,
                _ => throw new InvalidOperationException()
            };
        }

        public override string ToString() =>
            $"{this.NormalizedFileName}: Size=[{this.CompressedSize}/{this.OriginalSize}], Crc32=0x{this.Crc32:x8}";
    }
}
