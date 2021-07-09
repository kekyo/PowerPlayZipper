using System;
using System.Text;
using System.Threading;

#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper
{
    public interface IUnzipper : IZipperProcessing
    {
        bool IgnoreDirectoryEntry { get; }

        Encoding DefaultFileNameEncoding { get; }

        int ParallelCount { get; }

        int StreamBufferSize { get; }

#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="predicate"></param>
        /// <param name="targetPathSelector"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken = default);

        Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            Func<ZippedFileEntry, bool> predicate,
            CancellationToken cancellationToken = default);

        Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default);
#else
        /// <summary>
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="predicate"></param>
        /// <param name="targetPathSelector"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults Unzip(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector,
            CancellationToken cancellationToken = default);

        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            Func<ZippedFileEntry, bool> predicate,
            CancellationToken cancellationToken = default);

        ProcessedResults Unzip(
            string zipFilePath,
            string extractToBasePath,
            CancellationToken cancellationToken = default);
#endif
    }
}
