using System;
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzipperTraits : IUnzipperTraits
    {
        public readonly string ZipFilePath;
        public readonly string ExtractToBasePath;
        public readonly Regex? RegexPattern;

        public DefaultUnzipperTraits(
            string zipFilePath, string extractToBasePath,
            string? regexPattern = null,
            Action<ZippedFileEntry, ProcessingStates, long>? processing = null)
        {
            this.ZipFilePath = zipFilePath;
            this.ExtractToBasePath = extractToBasePath;
            this.RegexPattern = (regexPattern != null) ?
                new Regex(regexPattern, RegexOptions.Compiled) :
                null;
        }

        public virtual Stream OpenForReadZipFile(int recommendedBufferSize) =>
            FileSystemAccessor.OpenForReadFile(this.ZipFilePath, recommendedBufferSize);

        public virtual bool IsRequiredProcessing(ZippedFileEntry entry) =>
            this.RegexPattern?.IsMatch(entry.NormalizedFileName) ?? true;

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            FileSystemAccessor.CombinePath(this.ExtractToBasePath, entry.NormalizedFileName);

        public virtual void CreateDirectoryIfNotExist(string directoryPath) =>
            FileSystemAccessor.CreateDirectoryIfNotExist(directoryPath);

        public virtual Stream? OpenForWriteFile(string path, int recommendedBufferSize) =>
            FileSystemAccessor.OpenForWriteFile(path, true, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position)
        {
        }
    }
}
