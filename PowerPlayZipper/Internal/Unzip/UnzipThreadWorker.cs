using System;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class UnzipThreadWorker
    {
        [Flags]
        private enum GeneralPurposeBitFlags : short
        {
            Encrypted = 0x0001,  // bit0
            ProduceDataDescriptor = 0x0008,  // bit3   TODO:
            EntryIsUTF8 = 0x0800,  // bit11
        }

        private const int PK0304HeaderSize = 32;

        private readonly UnzipContext context;
        private readonly ReadOnlyRangedStream rangedStream;
        private readonly byte[] entryBuffer;
        private readonly Thread thread;

        public UnzipThreadWorker(string zipFilePath, UnzipContext context)
        {
            this.context = context;
            this.rangedStream = new ReadOnlyRangedStream(zipFilePath, 65536);
            this.entryBuffer = new byte[UnzipCommonRoleContext.EntryBufferSize];
            this.thread = new Thread(this.ThreadEntry);
            this.thread.IsBackground = true;
        }

        public void StartConsume() =>
            this.thread.Start();

        public void FinishConsume()
        {
            // TODO:
        }

        private static bool IsSupported(CompressionMethods cm, GeneralPurposeBitFlags gpbf)
        {
            if ((gpbf & (GeneralPurposeBitFlags.Encrypted | GeneralPurposeBitFlags.ProduceDataDescriptor)) != 0)
            {
                return false;
            }
            return (cm == CompressionMethods.Deflate) || (cm == CompressionMethods.Stored);
        }

        private static bool IsDirectory(CompressionMethods cm, string fileName) =>
            (cm == CompressionMethods.Stored) && fileName.EndsWith("/");

        private void ThreadEntry()
        {
            while (true)
            {
                ///////////////////////////////////////////////////////////////////////
                // Dequeue next header fetching.

                // Spin loop
                var commonContext = Interlocked.Exchange(ref this.context.CommonRoleContext, null);
                if (commonContext == null)
                {
                    continue;
                }

                ///////////////////////////////////////////////////////////////////////
                // Common thread role.

                // Read first header bytes.
                commonContext.EntryStream.Position = commonContext.HeaderPosition;
                var read = commonContext.EntryStream.Read(
                    this.entryBuffer, 0, UnzipCommonRoleContext.EntryBufferSize);
                if (read < PK0304HeaderSize)
                {
                    // TODO: Finish
                    break;
                }

                var signature = BitConverter.ToUInt32(this.entryBuffer, 0);
                if (signature == 0x04034b50) // PK0304
                {
                    // TODO: Finish
                    break;
                }

                var fileNameLength = BitConverter.ToUInt16(this.entryBuffer, 28);
                if (fileNameLength == 0)
                {
                    // TODO: error
                    break;
                }

                var compressedSize = BitConverter.ToUInt32(this.entryBuffer, 20);
                var commentLength = BitConverter.ToUInt16(this.entryBuffer, 30);

                // Update next header position.
                var fileNamePosition = commonContext.HeaderPosition + PK0304HeaderSize;
                var streamPosition = fileNamePosition + fileNameLength + commentLength;
                commonContext.HeaderPosition = streamPosition + compressedSize;

                // Enqueue next header.
                this.context.CommonRoleContext = commonContext;

                ///////////////////////////////////////////////////////////////////////
                // Worker thread role.

                // Make safer.
                commonContext = null!;

                //var versionNeededToExtract = BitConverter.ToUInt16(this.entryBuffer, 4);
                var generalPurposeBitFlag = (GeneralPurposeBitFlags)BitConverter.ToInt16(this.entryBuffer, 6);
                var compressionMethod = (CompressionMethods)BitConverter.ToInt16(this.entryBuffer, 8);
                if (IsSupported(compressionMethod, generalPurposeBitFlag))
                {
                    var time = BitConverter.ToUInt16(this.entryBuffer, 10);
                    var date = BitConverter.ToUInt16(this.entryBuffer, 12);
                    var crc32 = BitConverter.ToUInt32(this.entryBuffer, 14);
                    var originalSize = BitConverter.ToUInt32(this.entryBuffer, 24);

                    // TODO:
                    var dateTime = default(DateTime);

                    var encoding =
                        ((generalPurposeBitFlag & GeneralPurposeBitFlags.EntryIsUTF8) == GeneralPurposeBitFlags.EntryIsUTF8) ?
                            Encoding.UTF8 :
                            this.context.Encoding;

                    string fileName;
                    if (fileNameLength <= (UnzipCommonRoleContext.EntryBufferSize - PK0304HeaderSize))
                    {
                        fileName = encoding.GetString(
                            this.entryBuffer, PK0304HeaderSize, fileNameLength);
                    }
                    // Rare case: Very long file name.
                    else
                    {
                        var temporaryBuffer = new byte[fileNameLength];

                        Array.Copy(
                            this.entryBuffer, 32,
                            temporaryBuffer, 0, UnzipCommonRoleContext.EntryBufferSize - PK0304HeaderSize);

                        this.rangedStream.ResetRange(fileNamePosition);
                        var fileNameReaminsSize =
                            fileNameLength - (UnzipCommonRoleContext.EntryBufferSize - PK0304HeaderSize);
                        var fileNameRemains = this.rangedStream.Read(
                            temporaryBuffer,
                            UnzipCommonRoleContext.EntryBufferSize - PK0304HeaderSize,
                            fileNameReaminsSize);
                        if (fileNameRemains != fileNameReaminsSize)
                        {
                            // TODO: EOF
                        }

                        fileName = encoding.GetString(temporaryBuffer);
                    }

                    var isDirectory = IsDirectory(compressionMethod, fileName);

                    var entry = new ZippedFileEntry(
                        fileName,
                        isDirectory ? CompressionMethods.Directory : compressionMethod,
                        compressedSize, originalSize,
                        crc32, dateTime, streamPosition);

                    if (this.context.Predicate(entry))
                    {
                        ///////////////////////////////////////////////////////////////////////
                        // Unzip core.

                        switch (entry.CompressionMethod)
                        {
                            case CompressionMethods.Stored:
                                this.rangedStream.SetRange(entry.StreamPosition, entry.CompressedSize);
                                this.context.OnAction(entry, rangedStream);
                                break;
                            case CompressionMethods.Deflate:
                                this.rangedStream.SetRange(entry.StreamPosition, entry.CompressedSize);
                                var compressedStream = new DeflateStream(
                                    this.rangedStream, CompressionMode.Decompress, false);
                                this.context.OnAction(entry, compressedStream);
                                break;
                            case CompressionMethods.Directory:
                                this.context.OnAction(entry, null);
                                break;
                        }
                    }
                }
            }
        }
    }
}
