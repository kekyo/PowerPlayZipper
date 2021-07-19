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
            IUnzippingFileFeatures fileFeatures,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed,
            CancellationToken cancellationToken)
        {
            var totalFiles = 0;
            var totalCompressedSize = 0L;
            var totalOriginalSize = 0L;

            var sw = new Stopwatch();
            sw.Start();

            var context = new Context(
                fileFeatures.OpenForRead,
                this.IgnoreDirectoryEntry,
                (this.MaxParallelCount >= 1) ? this.MaxParallelCount : Environment.ProcessorCount,
                this.StreamBufferSize,
                this.DefaultFileNameEncoding ?? IndependentFactory.GetSystemDefaultEncoding(),
                fileFeatures.IsRequiredProcessing,
                (entry, compressedStream, streamBuffer) =>
                {
                    var targetPath = fileFeatures.GetTargetPath(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;

                    // Invoke event.
                    this.Processing?.Invoke(
                        this,
                        new ProcessingEventArgs(entry, ProcessingStates.Begin, 0));

                    // Create base directory.
                    fileFeatures.ConstructDirectory(directoryPath);

                    if (compressedStream != null)
                    {
                        Debug.Assert(streamBuffer != null);

                        // Copy stream data to target file.
                        using (var fs = fileFeatures.OpenForWrite(targetPath, streamBuffer!.Length))
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
        /// <param name="fileFeatures"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ProcessedResults> UnzipAsync(
            IUnzippingFileFeatures fileFeatures,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<ProcessedResults>();

            this.UnzipCore(
                fileFeatures,
                results => tcs.SetResult(results),
                exceptions => tcs.SetException(exceptions),
                cancellationToken);

            return tcs.Task;
        }

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath),
                cancellationToken);

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif

        ////////////////////////////////////////////////////////////////////////
        // Synchronous interface

        private ProcessedResults SynchronousUnzip(
            IUnzippingFileFeatures fileFeatures,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var ev = IndependentFactory.CreateManualResetEvent())
            {
                ProcessedResults? results = null;
                List<Exception>? exceptions = null;

                this.UnzipCore(
                    fileFeatures,
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
        /// <param name="fileFeatures"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults ISynchronousUnzipper.Unzip(
            IUnzippingFileFeatures fileFeatures,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                fileFeatures,
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath),
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#else
        /// <summary>
        /// </summary>
        /// <param name="fileFeatures"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        public ProcessedResults Unzip(
            IUnzippingFileFeatures fileFeatures,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                fileFeatures,
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath),
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                new DefaultUnzippingFileFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif
    }
}
