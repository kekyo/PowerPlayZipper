using System;
using System.ComponentModel;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Advanced
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISynchronousUnzipper
    {
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
    }
}
