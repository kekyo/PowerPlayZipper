using System;
using System.Text;
using System.Threading;

#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper
{
    public interface IUnzipper : IZipperProcessing
    {
        bool IgnoreEmptyDirectoryEntry { get; }

        Encoding DefaultFileNameEncoding { get; }

        int MaxParallelCount { get; }

        int StreamBufferSize { get; }

#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ProcessedResults> UnzipAsync(
            IUnzipperTraits traits,
            CancellationToken cancellationToken = default);

        Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default);

        Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default);
#else
        /// <summary>
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults Unzip(
            IUnzipperTraits traits,
            CancellationToken cancellationToken = default);

        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default);

        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            string regexPattern,
            CancellationToken cancellationToken = default);
#endif
    }
}
