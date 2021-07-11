using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class UnzipWorker
    {
        [Flags]
        private enum GeneralPurposeBitFlags : short
        {
            Encrypted = 0x0001,  // bit0
            ProduceDataDescriptor = 0x0008,  // bit3   TODO:
            EntryIsUTF8 = 0x0800,  // bit11
        }

        private readonly UnzipContext context;
        private readonly ReadOnlyRangedStream rangedStream;
        private readonly byte[] streamBuffer;
        private readonly Thread thread;

        public UnzipWorker(string zipFilePath, UnzipContext context)
        {
            this.context = context;
            this.rangedStream = new ReadOnlyRangedStream(zipFilePath, context.StreamBufferSize);
            this.streamBuffer = new byte[context.StreamBufferSize];
            this.thread = new Thread(this.ThreadEntry);
            this.thread.IsBackground = true;
        }

        public void StartConsume() =>
            this.thread.Start();

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

        private void UnzipCore()
        {
            while (true)
            {
                // Refill array pool (on this worker thread).
                this.context.BufferPool.Refill();

                // Received abort request.
                var request = this.context.RequestSpreader.Take();
                if (request == null)
                {
                    return;
                }

                Debug.Assert(request.Buffer != null);

                //var versionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(
                //    request.Buffer, request.BufferOffset + 4);
                var generalPurposeBitFlag = (GeneralPurposeBitFlags)BinaryPrimitives.ReadInt16LittleEndian(
                    request.Buffer!, request.BufferOffset + 6);
                var compressionMethod = (CompressionMethods)BinaryPrimitives.ReadInt16LittleEndian(
                    request.Buffer!, request.BufferOffset + 8);

                if (!IsSupported(compressionMethod, generalPurposeBitFlag))
                {
                    request.Clear();
                    this.context.RequestPool.Return(ref request);
                    continue;
                }

                var time = BinaryPrimitives.ReadUInt16LittleEndian(
                    request.Buffer!, request.BufferOffset + 10);
                var date = BinaryPrimitives.ReadUInt16LittleEndian(
                    request.Buffer!, request.BufferOffset + 12);
                var crc32 = BinaryPrimitives.ReadUInt32LittleEndian(
                    request.Buffer!, request.BufferOffset + 14);
                var originalSize = BinaryPrimitives.ReadUInt32LittleEndian(
                    request.Buffer!, request.BufferOffset + 22);

                var encoding =
                    ((generalPurposeBitFlag & GeneralPurposeBitFlags.EntryIsUTF8) == GeneralPurposeBitFlags.EntryIsUTF8) ?
                        Encoding.UTF8 :
                        this.context.Encoding;

                string fileName;

                // Can copy all file name string from the buffer.
                var bufferRemains = request.BufferSize - request.FileNameOffset;
                if (request.FileNameLength <= bufferRemains)
                {
                    try
                    {
                        fileName = encoding.GetString(
                            request.Buffer!, request.FileNameOffset, request.FileNameLength);
                    }
                    // Invalid Unicode code point or else.
                    catch (Exception ex)
                    {
                        request.Clear();
                        this.context.RequestPool.Return(ref request);
                        this.context.OnError(ex);
                        continue;
                    }
                }
                // Required last file name string fragment from the zip file.
                else
                {
                    var temporaryBuffer = new byte[request.FileNameLength];
                    var firstFragmentLength = bufferRemains;
                    var lastFragmentLength = request.FileNameLength - firstFragmentLength;

                    // Copy first file name string fragment from the buffer.
                    Array.Copy(
                        request.Buffer!, request.FileNameOffset,
                        temporaryBuffer, 0, firstFragmentLength);

                    try
                    {
                        // Reset stream position.
                        this.rangedStream.ResetRange(
                            request.BufferPosition + request.FileNameOffset + firstFragmentLength);

                        // Read last file name string fragment from the zip file.
                        var readLastFragmentLength = this.rangedStream.Read(
                            temporaryBuffer,
                            firstFragmentLength,
                            lastFragmentLength);
                        if (readLastFragmentLength != lastFragmentLength)
                        {
                            // Start finishing by EOF. (Has invalid header)
                            request.Clear();
                            this.context.RequestPool.Return(ref request);
                            this.context.OnError(new FormatException("TODO:"));
                            continue;
                        }

                        fileName = encoding.GetString(temporaryBuffer);
                    }
                    // IO problem, invalid Unicode code point or else.
                    catch (Exception ex)
                    {
                        request!.Clear();
                        this.context.RequestPool.Return(ref request);
                        this.context.OnError(ex);
                        continue;
                    }
                }

                var bodyPosition = request.CommentOffset + request.CommentLength;
                var compressedSize = request.CompressedSize;

                request!.Clear();
                this.context.RequestPool.Return(ref request);

                var isDirectory = IsDirectory(compressionMethod, fileName);
                if (this.context.IgnoreDirectoryEntry && isDirectory)
                {
                    continue;
                }

                // TODO:
                var dateTime = default(DateTime);

                var entry = new ZippedFileEntry(
                    fileName,
                    isDirectory ? CompressionMethods.Directory : compressionMethod,
                    compressedSize,
                    originalSize,
                    crc32,
                    dateTime);

                if (!this.context.Evaluate(entry))
                {
                    continue;
                }

                ///////////////////////////////////////////////////////////////////////

                switch (entry.CompressionMethod)
                {
                    case CompressionMethods.Stored:
                        this.rangedStream.SetRange(bodyPosition, compressedSize);
                        this.context.OnAction(
                            entry, rangedStream, this.streamBuffer);
                        break;
                    case CompressionMethods.Deflate:
                        this.rangedStream.SetRange(bodyPosition, compressedSize);
                        var compressedStream = new DeflateStream(
                            this.rangedStream, CompressionMode.Decompress, false);
                        this.context.OnAction(
                            entry, compressedStream, this.streamBuffer);
                        break;
                    case CompressionMethods.Directory:
                        this.context.OnAction(
                            entry, null, null);
                        break;
                }
            }
        }

        private void ThreadEntry()
        {
            try
            {
                this.UnzipCore();
            }
            catch (Exception ex)
            {
                this.context.OnError(ex);
            }
            finally
            {
                this.rangedStream.Dispose();
                this.context.OnFinished();
            }
        }
    }
}
