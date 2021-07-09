using System;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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

        private const int PK0304HeaderSize = 32;

        private readonly UnzipContext context;
        private readonly ReadOnlyRangedStream rangedStream;
        private readonly byte[] entryBuffer;
        private readonly byte[] streamBuffer;
        private readonly Thread thread;

        public UnzipWorker(string zipFilePath, UnzipContext context)
        {
            this.context = context;
            this.rangedStream = new ReadOnlyRangedStream(zipFilePath, context.StreamBufferSize);
            this.entryBuffer = new byte[UnzipCommonRoleContext.EntryBufferSize];
            this.streamBuffer = new byte[context.StreamBufferSize];
            this.thread = new Thread(() =>
            {
                try
                {
                    this.ThreadEntryCore();
                }
                catch (Exception ex)
                {
                    this.context.OnError(ex);
                }
                finally
                {
                    this.context.OnFinished();
                }
            });
            this.thread.IsBackground = true;
        }

        public void StartConsume() =>
            this.thread.Start();

        public void WaitForFinishConsume() =>
            this.thread.Join();

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

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private UnzipCommonRoleContext TakeCommonRole()
        {
            while (true)
            {
                // Take role context. (Spin loop)
                var commonRoleContext = Interlocked.Exchange(ref this.context.CommonRoleContext, null);
                if (commonRoleContext != null)
                {
                    return commonRoleContext;
                }
            }
        }

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void ReleaseCommonRole(ref UnzipCommonRoleContext? commonRoleContext)
        {
            this.context.CommonRoleContext = commonRoleContext;
            commonRoleContext = null;
        }

        private void ThreadEntryCore()
        {
            while (true)
            {
                // Dequeue next header fetching.
                var commonRoleContext = this.TakeCommonRole();

                ///////////////////////////////////////////////////////////////////////
                // Common thread role region.

                // Received abort request.
                if (commonRoleContext.HeaderPosition == -1)
                {
                    this.ReleaseCommonRole(ref commonRoleContext);
                    break;
                }

                // Read first header bytes.
                commonRoleContext.EntryStream.Position = commonRoleContext.HeaderPosition;
                var read = commonRoleContext.EntryStream.Read(
                    this.entryBuffer, 0, UnzipCommonRoleContext.EntryBufferSize);
                if (read < PK0304HeaderSize)
                {
                    // Start finishing by EOF. (Has unknown data)
                    commonRoleContext.HeaderPosition = -1;
                    this.ReleaseCommonRole(ref commonRoleContext);
                    break;
                }

                var signature = BitConverter.ToUInt32(this.entryBuffer, 0);
                if (signature != 0x04034b50) // PK0304
                {
                    // Start finishing by EOF. (Has unknown header)
                    commonRoleContext.HeaderPosition = -1;
                    this.ReleaseCommonRole(ref commonRoleContext);
                    break;
                }

                var fileNameLength = BitConverter.ToUInt16(this.entryBuffer, 28);
                if (fileNameLength == 0)
                {
                    // Raise fatal header error.
                    commonRoleContext.HeaderPosition = -1;
                    this.ReleaseCommonRole(ref commonRoleContext);
                    this.context.OnError(new FormatException("TODO:"));
                    break;
                }

                var compressedSize = BitConverter.ToUInt32(this.entryBuffer, 20);
                var commentLength = BitConverter.ToUInt16(this.entryBuffer, 30);

                // Update next header position.
                var fileNamePosition = commonRoleContext.HeaderPosition + PK0304HeaderSize;
                var streamPosition = fileNamePosition + fileNameLength + commentLength;
                commonRoleContext.HeaderPosition = streamPosition + compressedSize;

                // Enqueue next header.
                this.ReleaseCommonRole(ref commonRoleContext);

                ///////////////////////////////////////////////////////////////////////
                // Worker thread role.

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
                        try
                        {
                            fileName = encoding.GetString(this.entryBuffer, PK0304HeaderSize, fileNameLength);
                        }
                        // IO problem, invalid Unicode code point or else.
                        catch (Exception ex)
                        {
                            this.context.OnError(ex);
                            continue;
                        }
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

                        try
                        {
                            var fileNameRemains = this.rangedStream.Read(
                                temporaryBuffer,
                                UnzipCommonRoleContext.EntryBufferSize - PK0304HeaderSize,
                                fileNameReaminsSize);
                            if (fileNameRemains != fileNameReaminsSize)
                            {
                                // Start finishing by EOF. (Has invalid header)
                                this.context.OnError(new FormatException("TODO:"));
                                continue;
                            }

                            fileName = encoding.GetString(temporaryBuffer);
                        }
                        // IO problem, invalid Unicode code point or else.
                        catch (Exception ex)
                        {
                            this.context.OnError(ex);
                            continue;
                        }
                    }

                    var isDirectory = IsDirectory(compressionMethod, fileName);
                    if (!this.context.IgnoreDirectoryEntry || !isDirectory)
                    {
                        var entry = new ZippedFileEntry(
                            fileName,
                            isDirectory ? CompressionMethods.Directory : compressionMethod,
                            compressedSize,
                            originalSize,
                            crc32,
                            dateTime);

                        if (this.context.Evaluate(entry))
                        {
                            ///////////////////////////////////////////////////////////////////////
                            // Unzip core.

                            switch (entry.CompressionMethod)
                            {
                                case CompressionMethods.Stored:
                                    this.rangedStream.SetRange(streamPosition, entry.CompressedSize);
                                    this.context.OnAction(entry, rangedStream, this.streamBuffer);
                                    break;
                                case CompressionMethods.Deflate:
                                    this.rangedStream.SetRange(streamPosition, entry.CompressedSize);
                                    var compressedStream = new DeflateStream(
                                        this.rangedStream, CompressionMode.Decompress, false);
                                    this.context.OnAction(entry, compressedStream, this.streamBuffer);
                                    break;
                                case CompressionMethods.Directory:
                                    this.context.OnAction(entry, null, null);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
