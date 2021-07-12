using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

using PowerPlayZipper.Compatibility;
using PowerPlayZipper.Advanced;
using PowerPlayZipper.Internal.Unzip;

namespace PowerPlayZipper
{
    public enum DefaultFileNameEncodings
    {
        AlwaysUTF8,
        SystemDefaultIfApplicable
    }

    public sealed class Unzipper : IUnzipper, ISynchronousUnzipper
    {
        private const int DefaultStreamBufferSize = 131072;
        private const int NotifyCount = 100;

        public Unzipper() =>
            this.DefaultFileNameEncoding = Encoding.UTF8;

        public Unzipper(DefaultFileNameEncodings fileNameEncoding) =>
            this.DefaultFileNameEncoding =
                fileNameEncoding == DefaultFileNameEncodings.SystemDefaultIfApplicable ?
                    IndependentFactory.GetSystemDefaultEncoding() :
                    Encoding.UTF8;

        ////////////////////////////////////////////////////////////////////////
        // Properties

        public bool IgnoreDirectoryEntry { get; set; }

        public Encoding DefaultFileNameEncoding { get; set; }
            
        public int MaxParallelCount { get; set; } =
            Environment.ProcessorCount;

        public int StreamBufferSize { get; set; } =
            DefaultStreamBufferSize;

        public event EventHandler<ProcessingEventArgs>? Processing;

        ////////////////////////////////////////////////////////////////////////
        // Unzip core

        private void UnzipCore(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed,
            CancellationToken cancellationToken)
        {
            var directoryConstructor = new DirectoryConstructor();
            var totalFiles = 0;
            var totalCompressedSize = 0L;
            var totalOriginalSize = 0L;

            var sw = new Stopwatch();
            sw.Start();

            var context = new Context(
                zipFilePath,
                this.IgnoreDirectoryEntry,
                (this.MaxParallelCount >= 1) ? this.MaxParallelCount : Environment.ProcessorCount,
                this.DefaultFileNameEncoding ?? IndependentFactory.GetSystemDefaultEncoding(),
                this.StreamBufferSize,
                predicate,
                (entry, compressedStream, streamBuffer) =>
                {
                    var targetPath = targetPathSelector(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;

                    // Invoke event.
                    this.Processing?.Invoke(
                        this,
                        new ProcessingEventArgs(entry, ProcessingStates.Begin, 0));

                    // Create base directory.
                    directoryConstructor.CreateIfNotExist(directoryPath);

                    if (compressedStream != null)
                    {
                        Debug.Assert(streamBuffer != null);

                        // Copy stream data to target file.
                        using (var fs = new FileStream(
                            targetPath,
                            FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                            streamBuffer!.Length))
                        {
                            var notifyCount = NotifyCount;
                            while (true)
                            {
                                var read = compressedStream.Read(
                                    streamBuffer!, 0, streamBuffer!.Length);
                                if (read == 0)
                                {
                                    break;
                                }
                                fs.Write(streamBuffer!, 0, read);

                                if (notifyCount-- <= 0)
                                {
                                    // Invoke event.
                                    if (this.Processing is { } processing)
                                    {
                                        processing(
                                            this,
                                            new ProcessingEventArgs(entry, ProcessingStates.Processing, fs.Position));
                                    }
                                    notifyCount = NotifyCount;
                                }
                            }
                            fs.Flush();
                        }

                        Interlocked.Increment(ref totalFiles);
                        Interlocked.Add(ref totalCompressedSize, entry.CompressedSize);
                        Interlocked.Add(ref totalOriginalSize, entry.OriginalSize);
                    }

                    // Invoke event.
                    this.Processing?.Invoke(
                        this,
                        new ProcessingEventArgs(entry, ProcessingStates.Done, entry.OriginalSize));
                },
                (exceptions, parallelCount, internalStats) =>
                {
                    if (exceptions.Count >= 1)
                    {
                        failed(exceptions);
                    }
                    else
                    {
                        succeeded(new ProcessedResults(
                            totalFiles, totalCompressedSize, totalOriginalSize,
                            sw.Elapsed, parallelCount, internalStats));
                    }
                });

            cancellationToken.Register(context.RequestAbort);
            context.Start();
        }

#if !NET20 && !NET35
        ////////////////////////////////////////////////////////////////////////
        // Asynchronous interface

        /// <summary>
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="predicate"></param>
        /// <param name="targetPathSelector"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<ProcessedResults>();

            this.UnzipCore(
                zipFilePath,
                predicate,
                targetPathSelector,
                results => tcs.SetResult(results),
                exceptions => tcs.SetException(exceptions),
                cancellationToken);

            return tcs.Task;
        }

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            Func<ZippedFileEntry, bool> predicate,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);
#endif

        ////////////////////////////////////////////////////////////////////////
        // Synchronous interface

        private ProcessedResults SynchronousUnzip(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var ev = IndependentFactory.CreateManualResetEvent())
            {
                ProcessedResults? results = null;
                List<Exception>? exceptions = null;

                this.UnzipCore(
                    zipFilePath,
                    predicate,
                    targetPathSelector,
                    r =>
                    {
                        results = r;
                        ev.Set();
                    },
                    exs =>
                    {
                        exceptions = exs;
                        ev.Set();
                    },
                    cancellationToken);

                ev.Wait();

                Debug.Assert(results != null);

                if (exceptions is { })
                {
                    throw IndependentFactory.GetAggregateException(exceptions);
                }
                else
                {
                    return (ProcessedResults)results!;
                }
            }
        }

#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="predicate"></param>
        /// <param name="targetPathSelector"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                zipFilePath,
                predicate,
                targetPathSelector,
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            Func<ZippedFileEntry, bool> predicate,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);
#else
        /// <summary>
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="predicate"></param>
        /// <param name="targetPathSelector"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        public ProcessedResults Unzip(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                zipFilePath,
                predicate,
                targetPathSelector,
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            Func<ZippedFileEntry, bool> predicate,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName),
                cancellationToken);
#endif
    }
}
