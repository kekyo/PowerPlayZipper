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
            IUnzippingFeatures features,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed,
            CancellationToken cancellationToken)
        {
            var directoryConstructor = new DirectoryConstructor(features.CreateDirectoryIfNotExist);

            var totalFiles = 0;
            var totalCompressedSize = 0L;
            var totalOriginalSize = 0L;

            var sw = new Stopwatch();
            sw.Start();

            var context = new Context(
                features.OpenForReadZipFile,
                this.IgnoreDirectoryEntry,
                (this.MaxParallelCount >= 1) ? this.MaxParallelCount : Environment.ProcessorCount,
                this.StreamBufferSize,
                this.DefaultFileNameEncoding ?? IndependentFactory.GetSystemDefaultEncoding(),
                features.IsRequiredProcessing,
                (entry, compressedStream, streamBuffer) =>
                {
                    var targetPath = features.GetTargetPath(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;

                    // Invoke event.
                    features.OnProcessing(entry, ProcessingStates.Begin, 0);

                    // Create base directory.
                    directoryConstructor.CreateIfNotExist(directoryPath);

                    if (compressedStream != null)
                    {
                        Debug.Assert(streamBuffer != null);

                        // Copy stream data to target file.
                        using (var fs = features.OpenForWriteFile(targetPath, streamBuffer!.Length))
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
                                    features.OnProcessing(entry, ProcessingStates.Processing, fs.Position);
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
                    features.OnProcessing(entry, ProcessingStates.Done, entry.OriginalSize);
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

        private DefaultUnzippingFeatures CreateDefaultUnzippingFeatures(
            string zipFilePath, string extractToBasePath, string? regexPattern)
        {
            var features = new DefaultUnzippingFeatures(zipFilePath, extractToBasePath, regexPattern);
            features.Processing += (s, e) => this.Processing?.Invoke(this, e);
            return features;
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
            IUnzippingFeatures fileFeatures,
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
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, null),
                cancellationToken);

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif

        ////////////////////////////////////////////////////////////////////////
        // Synchronous interface

        private ProcessedResults SynchronousUnzip(
            IUnzippingFeatures fileFeatures,
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
            IUnzippingFeatures fileFeatures,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                fileFeatures,
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, null),
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#else
        /// <summary>
        /// </summary>
        /// <param name="fileFeatures"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        public ProcessedResults Unzip(
            IUnzippingFeatures fileFeatures,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                fileFeatures,
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, null),
                cancellationToken);

        public ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                this.CreateDefaultUnzippingFeatures(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif
    }
}
