using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Migration
{
    internal sealed class FastZipMigrationUnzipperTraits : DefaultUnzipperTraits
    {
        private readonly Regex? directoryPattern;
        private readonly FastZip.Overwrite overwrite;
        private readonly FastZip.ConfirmOverwriteDelegate? comfirm;

        public FastZipMigrationUnzipperTraits(
            string zipFilePath, string extractToBasePath, string? filePattern, string? directoryPattern,
            FastZip.Overwrite overwrite, FastZip.ConfirmOverwriteDelegate? comfirm) :
            base(zipFilePath, extractToBasePath, filePattern)
        {
            this.directoryPattern = CompilePattern(directoryPattern);
            this.overwrite = overwrite;
            this.comfirm = comfirm;
        }

        public override bool IsRequiredProcessing(ZippedFileEntry entry) =>
            (entry.CompressionMethod == CompressionMethods.Directory) ?
                (this.directoryPattern?.IsMatch(entry.NormalizedFileName) ?? true) :
                (this.RegexPattern?.IsMatch(entry.NormalizedFileName) ?? true);

        public override Stream? OpenForWriteFile(string path, int recommendedBufferSize)
        {
            switch (this.overwrite)
            {
                case FastZip.Overwrite.Never:
                    return FileSystemAccessor.OpenForWriteFile(
                        path, recommendedBufferSize);
                case FastZip.Overwrite.Prompt:
                    var stream = FileSystemAccessor.OpenForWriteFile(
                        path, recommendedBufferSize);
                    if (stream == null)
                    {
                        if (this.comfirm?.Invoke(path) ?? false)
                        {
                            stream = FileSystemAccessor.OpenForOverwriteFile(
                                path, recommendedBufferSize);
                        }
                    }
                    return stream;
                default:
                    return FileSystemAccessor.OpenForOverwriteFile(
                        path, recommendedBufferSize);
            }
        }
    }
}
