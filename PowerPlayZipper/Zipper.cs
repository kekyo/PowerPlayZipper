///////////////////////////////////////////////////////////////////////////
//
// PowerPlayZipper - An implementation of Lightning-Fast Zip file
// compression/decompression library on .NET.
// Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Compatibility;
using PowerPlayZipper.Internal.Zip;
using PowerPlayZipper.Synchronously;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    public sealed class Zipper : IZipper, ISynchronousZipper
    {
        private const int DefaultStreamBufferSize = 131072;

        public Zipper() =>
            this.DefaultFileNameEncoding = Encoding.UTF8;

        public Zipper(DefaultFileNameEncodings fileNameEncoding) =>
            this.DefaultFileNameEncoding =
                fileNameEncoding == DefaultFileNameEncodings.SystemDefaultIfApplicable ?
                    IndependentFactory.GetSystemDefaultEncoding() :
                    Encoding.UTF8;

        ////////////////////////////////////////////////////////////////////////
        // Properties

        public bool IgnoreEmptyDirectory { get; set; }

        public Encoding DefaultFileNameEncoding { get; set; }
            
        public int MaximumParallelCount { get; set; } =
            Environment.ProcessorCount;

        public int StreamBufferSize { get; set; } =
            DefaultStreamBufferSize;

        public event EventHandler<ProcessingEventArgs>? Processing;

        ////////////////////////////////////////////////////////////////////////
        // Zip core

        private void ZipCore(
            IZipperTraits traits,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed,
            CancellationToken cancellationToken)
        {
            var context = new Controller(
                traits,
                this.IgnoreEmptyDirectory,
                (this.MaximumParallelCount >= 1) ? this.MaximumParallelCount : Environment.ProcessorCount,
                this.StreamBufferSize,
                this.DefaultFileNameEncoding ?? IndependentFactory.GetSystemDefaultEncoding(),
                succeeded,
                failed);

            cancellationToken.Register(context.RequestAbort);
            context.Run();
        }

        private BypassProcessingZipperTraits CreateBypassZipperTraits(
            string basePath, string zipFilePath, string? regexPattern)
        {
            var traits = new BypassProcessingZipperTraits(basePath, zipFilePath, regexPattern);
            if (this.Processing is { } processing)
            {
                traits.Processing += (_, e) => processing.Invoke(this, e);
            }
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
        public Task<ProcessedResults> ZipAsync(
            IZipperTraits traits,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<ProcessedResults>();

            this.ZipCore(
                traits,
                results => tcs.SetResult(results),
                exceptions => tcs.SetException(exceptions),
                cancellationToken);

            return tcs.Task;
        }

        public Task<ProcessedResults> ZipAsync(
            string extractToBasePath,
            string zipFilePath,
            CancellationToken cancellationToken = default) =>
            ZipAsync(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, null),
                cancellationToken);

        public Task<ProcessedResults> ZipAsync(
            string extractToBasePath,
            string zipFilePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            ZipAsync(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, regexPattern),
                cancellationToken);
#endif

        ////////////////////////////////////////////////////////////////////////
        // Synchronous interface

        private ProcessedResults SynchronousZip(
            IZipperTraits traits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var ev = IndependentFactory.CreateManualResetEvent())
            {
                ProcessedResults? results = null;
                List<Exception>? exceptions = null;

                this.ZipCore(
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
                    return results!;
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
        ProcessedResults Zip(
            IZipperTraits traits,
            CancellationToken cancellationToken = default) =>
            SynchronousZip(
                traits,
                cancellationToken);

#if !NET20 && !NET35
        internal
#else
        public
#endif
        ProcessedResults Zip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default) =>
            SynchronousZip(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, null),
                cancellationToken);

#if !NET20 && !NET35
        internal
#else
        public
#endif
        ProcessedResults Zip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default) =>
            SynchronousZip(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, regexPattern),
                cancellationToken);

#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults ISynchronousZipper.Zip(
            IZipperTraits traits,
            CancellationToken cancellationToken) =>
            SynchronousZip(
                traits,
                cancellationToken);

        ProcessedResults ISynchronousZipper.Zip(
            string extractToBasePath,
            string zipFilePath,
            CancellationToken cancellationToken) =>
            SynchronousZip(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, null),
                cancellationToken);

        ProcessedResults ISynchronousZipper.Zip(
            string extractToBasePath,
            string zipFilePath,
            string regexPattern,
            CancellationToken cancellationToken) =>
            SynchronousZip(
                this.CreateBypassZipperTraits(extractToBasePath, zipFilePath, regexPattern),
                cancellationToken);
#endif
    }
}
