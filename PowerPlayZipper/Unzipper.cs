using PowerPlayZipper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    public sealed class Unzipper
    {
        [Flags]
        private enum GeneralPurposeBitFlags : short
        {
            Encrypted = 0x0001,  // bit0
            ProduceDataDescriptor = 0x0008,  // bit3   TODO:
            EntryIsUTF8 = 0x0800,  // bit11
        }

        private readonly bool ignoreDirectoryEntry;
        private readonly bool isOnlySupported;
        private readonly Encoding? encoding;
        private readonly int parallelCount;
        private readonly int streamBufferSize;

        public Unzipper(
            bool ignoreDirectoryEntry = false,
            bool isOnlySupported = true,
            Encoding? encoding = default,
            int parallelCount = -1,
            int streamBufferSize = -1)
        {
            this.ignoreDirectoryEntry = ignoreDirectoryEntry;
            this.isOnlySupported = isOnlySupported;
            this.encoding = encoding;
            this.parallelCount = (parallelCount >= 1) ? parallelCount : Environment.ProcessorCount;
            this.streamBufferSize = (streamBufferSize >= 1) ? streamBufferSize : 65536;
        }

        public event EventHandler<UnzippingEventArgs>? Unzipping;

        private sealed class UnzipCommonRoleContext
        {
            // PAGE_SIZE
            public const int EntryBufferSize = 4096;

            public long HeaderPosition;
            public readonly FileStream EntryStream;

            public UnzipCommonRoleContext(string zipFilePath) =>
                this.EntryStream = new FileStream(
                    zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, EntryBufferSize);

            public void Close() =>
                this.EntryStream.Dispose();
        }
            
        private sealed class UnzipThreadWorker
        {
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

        private sealed class UnzipContext
        {
            private readonly UnzipThreadWorker[] threadWorkers;
            private readonly Func<ZippedFileEntry, bool> predicate;
            private readonly Action<ZippedFileEntry, Stream?> action;

            public readonly Encoding Encoding;
            public volatile UnzipCommonRoleContext? CommonRoleContext;
            
            public UnzipContext(
                string zipFilePath, int parallelCount, Encoding encoding,
                Func<ZippedFileEntry, bool> predicate,
                Action<ZippedFileEntry, Stream?> action)
            {
                this.Encoding = encoding;
                this.predicate = predicate;
                this.action = action;
                this.CommonRoleContext = new UnzipCommonRoleContext(zipFilePath);
                
                this.threadWorkers = new UnzipThreadWorker[parallelCount];
                for (var index = 0; index < this.threadWorkers.Length; index++)
                {
                    this.threadWorkers[index] = new UnzipThreadWorker(zipFilePath, this);
                }
            }

            public bool Predicate(ZippedFileEntry entry) =>
                this.predicate(entry);
            
            public void OnAction(ZippedFileEntry entry, Stream? compressedStream) =>
                this.action(entry, compressedStream);

            public void Start()
            {
                for (var index = 0; index < this.threadWorkers.Length; index++)
                {
                    this.threadWorkers[index].StartConsume();
                }
            }

            public void Finish()
            {
                for (var index = 0; index < this.threadWorkers.Length; index++)
                {
                    this.threadWorkers[index].FinishConsume();
                }
            }
        }

        public async Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector)
        {
            var directoryConstructor = new DirectoryConstructor();
            var totalFiles = 0;
            var totalCompressedSize = 0L;
            var totalOriginalSize = 0L;

            var sw = new Stopwatch();
            sw.Start();

            var context = new UnzipContext(
                zipFilePath, this.parallelCount,
#if NETCOREAPP1_0 || NETSTANDARD1_4
                this.encoding ?? Encoding.UTF8,
#else
                this.encoding ?? Encoding.Default,
#endif
                predicate,
                (entry, compressedStream) =>
                {
                    var targetPath = targetPathSelector(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;
                    
                    this.Unzipping?.Invoke(this, new UnzippingEventArgs(entry, UnzippingStates.Begin, 0));

                    directoryConstructor.CreateIfNotExist(directoryPath);

                    if (compressedStream != null)
                    {
                        try
                        {
                            using (var fs = new FileStream(
                                targetPath,
                                FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                                this.streamBufferSize))
                            {
                                compressedStream.CopyTo(fs);
                                fs.Flush();
                            }

                            Interlocked.Increment(ref totalFiles);
                            Interlocked.Add(ref totalCompressedSize, entry.CompressedSize);
                            Interlocked.Add(ref totalOriginalSize, entry.OriginalSize);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    
                    this.Unzipping?.Invoke(this, new UnzippingEventArgs(entry, UnzippingStates.Done, entry.OriginalSize));
                });
            
            context.Start();

            return new ProcessedResults(
                totalFiles, totalCompressedSize, totalOriginalSize, sw.Elapsed);
        }

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath, Func<ZippedFileEntry, bool> predicate) =>
            UnzipAsync(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath) =>
            UnzipAsync(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));
    }
}
