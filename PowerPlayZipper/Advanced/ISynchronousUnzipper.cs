using System;
using System.ComponentModel;
using System.Threading;

namespace PowerPlayZipper.Advanced
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISynchronousUnzipper : IUnzipper
    {
#if !NET20 && !NET35
        /// <summary>
        /// </summary>
        /// <param name="fileFeatures"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ProcessedResults</returns>
        ProcessedResults Unzip(
            IUnzippingFileFeatures fileFeatures,
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
