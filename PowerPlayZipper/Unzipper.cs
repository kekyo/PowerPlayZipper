using PowerPlayZipper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private static bool IsSupported(CompressionMethods cm, GeneralPurposeBitFlags gpbf) =>
            (cm, gpbf) switch
            {
                (_, GeneralPurposeBitFlags.Encrypted | GeneralPurposeBitFlags.ProduceDataDescriptor) => false,
                (_, GeneralPurposeBitFlags.Encrypted) => false,
                (_, GeneralPurposeBitFlags.ProduceDataDescriptor) => false,
                (CompressionMethods.Deflate, _) => true,
                (CompressionMethods.Stored, _) => true,
                _ => false
            };

        private static bool IsDirectory(CompressionMethods cm, string fileName) =>
            (cm == CompressionMethods.Stored) && fileName.EndsWith("/");

        private async IAsyncEnumerable<ZippedFileEntry> EnumerateFilesAsync(
            string zipFilePath, ReadOnlyRangedStreamFactory factory)
        {
            using (var stream = new ReadEntryStream(zipFilePath))
            {
                while (true)
                {
                    var (result, signature) = await stream.TryReadInt32Async().ConfigureAwait(false);
                    if (!result)
                    {
                        break;
                    }
                    if (signature != 0x04034b50)
                    {
                        break;
                    }

                    var versionNeededToExtract = await stream.ReadInt16Async().ConfigureAwait(false);
                    var generalPurposeBitFlag = (GeneralPurposeBitFlags)await stream.ReadInt16Async().ConfigureAwait(false);
                    var compressionMethod = (CompressionMethods)await stream.ReadInt16Async().ConfigureAwait(false);
                    var time = await stream.ReadInt16Async().ConfigureAwait(false);
                    var date = await stream.ReadInt16Async().ConfigureAwait(false);
                    var crc32 = await stream.ReadInt32Async().ConfigureAwait(false);
                    var compressedSize = await stream.ReadInt32Async().ConfigureAwait(false);
                    var originalSize = await stream.ReadInt32Async().ConfigureAwait(false);
                    var fileNameLength = await stream.ReadInt16Async().ConfigureAwait(false);
                    var commentLength = await stream.ReadInt16Async().ConfigureAwait(false);

                    // TODO:
                    var dateTime = default(DateTime);

                    var fileNameBuffer = new byte[fileNameLength];
                    var fileNameRead = await stream.ReadAsync(
                        fileNameBuffer, 0, fileNameBuffer.Length).
                        ConfigureAwait(false);
                    if (fileNameRead != fileNameBuffer.Length)
                    {
                        throw new IOException();
                    }

                    if (!this.isOnlySupported ||
                        IsSupported(compressionMethod, generalPurposeBitFlag))
                    {
                        var fileName =
                            ((generalPurposeBitFlag & GeneralPurposeBitFlags.EntryIsUTF8) == GeneralPurposeBitFlags.EntryIsUTF8) ?
                            Encoding.UTF8.GetString(fileNameBuffer) :
                            (this.encoding?.GetString(fileNameBuffer) ?? Encoding.Default.GetString(fileNameBuffer));

                        var isDirectory = IsDirectory(compressionMethod, fileName);

                        if (!this.ignoreDirectoryEntry || !isDirectory)
                        {
                            var op1 = stream.Position;
                            var np1 = stream.Seek(commentLength, SeekOrigin.Current);
                            if (np1 != (op1 + commentLength))
                            {
                                throw new IOException();
                            }

                            var streamPosition = stream.Position;

                            yield return new ZippedFileEntry(
                                fileName,
                                isDirectory ? CompressionMethods.Directory : compressionMethod,
                                compressedSize, originalSize,  // TODO: Deflate64
                                crc32, dateTime, streamPosition,
                                factory);

                            var np2 = stream.Seek(compressedSize, SeekOrigin.Current);
                            if (np2 != (streamPosition + compressedSize))
                            {
                                throw new IOException();
                            }

                            continue;
                        }
                    }

                    var op = stream.Position;
                    var np = stream.Seek(commentLength + compressedSize, SeekOrigin.Current);
                    if (np != (op + commentLength + compressedSize))
                    {
                        throw new IOException();
                    }
                }
            }
        }

        public async ValueTask ParallelForEachAsync(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, ValueTask> action)
        {
            using (var factory = new ReadOnlyRangedStreamFactory(zipFilePath, this.streamBufferSize))
            {
                var runningTasks = new List<Task>();
                var exs = default(List<Exception>?);

                async ValueTask WhenAnyAsync()
                {
                    Debug.Assert(runningTasks.Count >= 1);

                    try
                    {
                        var task = await Task.WhenAny(runningTasks).
                            ConfigureAwait(false);
                        runningTasks!.Remove(task);

                        await task.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (exs == default)
                        {
                            exs = new List<Exception>();
                        }
                        exs.Add(ex);
                    }
                }

                await foreach (var entry in this.EnumerateFilesAsync(zipFilePath, factory).
                    ConfigureAwait(false))
                {
                    if (predicate(entry))
                    {
                        this.Unzipping?.Invoke(this, new UnzippingEventArgs(entry));

                        var valueTask = action(entry);
                        if (!valueTask.IsCompletedSuccessfully)
                        {
                            runningTasks.Add(valueTask.AsTask());
                        }

                        if (runningTasks.Count >= this.parallelCount)
                        {
                            await WhenAnyAsync().ConfigureAwait(false);
                        }
                    }
                }

                // Exhausts remains.
                while (runningTasks.Count >= 1)
                {
                    await WhenAnyAsync().ConfigureAwait(false);
                }

                if (exs != default)
                {
                    throw new AggregateException(exs);
                }
            }
        }

        public async ValueTask<ProcessedResults> UnzipAsync(
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

            await this.ParallelForEachAsync(
                zipFilePath,
                predicate,
                async entry =>
                {
                    var targetPath = targetPathSelector(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;

                    await directoryConstructor.CreateIfNotExistAsync(directoryPath).
                        ConfigureAwait(false);

                    if (entry.CompressionMethod != CompressionMethods.Directory)
                    {
                        using (var fs = new FileStream(
                            targetPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, streamBufferSize, true))
                        {
                            using (var stream = entry.GetDecompressedStream())
                            {
                                await stream.CopyToAsync(fs).ConfigureAwait(false);
                            }
                            await fs.FlushAsync().ConfigureAwait(false);
                        }

                        Interlocked.Increment(ref totalFiles);
                        Interlocked.Add(ref totalCompressedSize, entry.CompressedSize);
                        Interlocked.Add(ref totalOriginalSize, entry.OriginalSize);
                    }
                });

            return new ProcessedResults(
                totalFiles, totalCompressedSize, totalOriginalSize, sw.Elapsed);
        }

        public ValueTask<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath, Func<ZippedFileEntry, bool> predicate) =>
            UnzipAsync(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));

        public ValueTask<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath) =>
            UnzipAsync(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));
    }
}
