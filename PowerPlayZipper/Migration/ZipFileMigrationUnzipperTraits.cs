using System.IO;

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Migration
{
    internal sealed class ZipFileMigrationUnzipperTraits : DefaultUnzipperTraits
    {
        private readonly bool overwriteFiles;

        public ZipFileMigrationUnzipperTraits(
            string zipFilePath, string extractToBasePath, bool overwriteFiles) :
            base(zipFilePath, extractToBasePath) =>
            this.overwriteFiles = overwriteFiles;

        public override Stream? OpenForWriteFile(string path, int recommendedBufferSize) =>
            this.overwriteFiles ?
                FileSystemAccessor.OpenForOverwriteFile(
                    path, recommendedBufferSize) :
                FileSystemAccessor.OpenForWriteFile(
                    path, recommendedBufferSize);
    }
}
