using System.Text;

namespace PowerPlayZipper.Migration
{
    /// <summary>
    /// Migration class for System.IO.Compression.ZipFile.
    /// </summary>
    public static class ZipFile
    {
        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, true);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable);
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="overwriteFiles">Overwrite if exists</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable);
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="encoding">Default file name encoding</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, Encoding encoding)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, true);
            var unzipper = new Unzipper
            {
                DefaultFileNameEncoding = encoding
            };
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="encoding">Default file name encoding</param>
        /// <param name="overwriteFiles">Overwrite if exists</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, Encoding encoding, bool overwriteFiles)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            var unzipper = new Unzipper
            {
                DefaultFileNameEncoding = encoding
            };
            unzipper.Unzip(traits);
        }
    }
}
