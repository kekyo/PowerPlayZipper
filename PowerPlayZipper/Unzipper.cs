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
using PowerPlayZipper.Utilities;

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

        public Unzipper() =>
            this.DefaultFileNameEncoding = Encoding.UTF8;

        public Unzipper(DefaultFileNameEncodings fileNameEncoding) =>
            this.DefaultFileNameEncoding =
                fileNameEncoding == DefaultFileNameEncodings.SystemDefaultIfApplicable ?
                    IndependentFactory.GetSystemDefaultEncoding() :
                    Encoding.UTF8;

        ////////////////////////////////////////////////////////////////////////
        // Properties

        public bool OverwriteIfExist { get; set; } =
            true;

        public bool IgnoreEmptyDirectoryEntry { get; set; }

        public Encoding DefaultFileNameEncoding { get; set; }
            
        public int MaxParallelCount { get; set; } =
            Environment.ProcessorCount;

        public int StreamBufferSize { get; set; } =
            DefaultStreamBufferSize;

        public event EventHandler<ProcessingEventArgs>? Processing;

        ////////////////////////////////////////////////////////////////////////
        // Unzip core

        private void UnzipCore(
            IUnzipperTraits traits,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed,
            CancellationToken cancellationToken)
        {
            var context = new Controller(
                traits,
                this.IgnoreEmptyDirectoryEntry,
                (this.MaxParallelCount >= 1) ? this.MaxParallelCount : Environment.ProcessorCount,
                this.StreamBufferSize,
                this.DefaultFileNameEncoding ?? IndependentFactory.GetSystemDefaultEncoding(),
                succeeded,
                failed);

            cancellationToken.Register(context.RequestAbort);
            context.Start();
        }

        private BypassProcessingUnzipperTraits CreateBypassUnzipperTraits(
            string zipFilePath, string extractToBasePath, string? regexPattern)
        {
            var traits = new BypassProcessingUnzipperTraits(zipFilePath, extractToBasePath, regexPattern);
            traits.Processing += (s, e) => this.Processing?.Invoke(this, e);
            return traits;
        }

#if !NET20 && !NET35
        ////////////////////////////////////////////////////////////////////////
        // Asynchronous interface

        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ProcessedResults> UnzipAsync(
            IUnzipperTraits traits,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<ProcessedResults>();

            this.UnzipCore(
                traits,
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
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, null),
                cancellationToken);

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            UnzipAsync(
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif

        ////////////////////////////////////////////////////////////////////////
        // Synchronous interface

        private ProcessedResults SynchronousUnzip(
            IUnzipperTraits traits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var ev = IndependentFactory.CreateManualResetEvent())
            {
                ProcessedResults? results = null;
                List<Exception>? exceptions = null;

                this.UnzipCore(
                    traits,
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

        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
#if !NET20 && !NET35
        internal
#else
        public
#endif
        ProcessedResults Unzip(
            IUnzipperTraits traits,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                traits,
                cancellationToken);

#if !NET20 && !NET35
        internal
#else
        public
#endif
        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, null),
                cancellationToken);

#if !NET20 && !NET35
        internal
#else
        public
#endif
        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            SynchronousUnzip(
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);

#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults ISynchronousUnzipper.Unzip(
            IUnzipperTraits traits,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                traits,
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, null),
                cancellationToken);

        ProcessedResults ISynchronousUnzipper.Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken) =>
            SynchronousUnzip(
                this.CreateBypassUnzipperTraits(zipFilePath, extractToBasePath, regexPattern),
                cancellationToken);
#endif
    }
}
