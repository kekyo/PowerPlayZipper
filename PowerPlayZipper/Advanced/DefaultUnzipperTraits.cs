using System;
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzipperTraits : IUnzipperTraits
    {
        public readonly string ZipFilePath;
        public readonly string ExtractToBasePath;
        public readonly Regex? RegexPattern;

        private readonly DirectoryConstructor directoryConstructor;

        public DefaultUnzipperTraits(
            string zipFilePath, string extractToBasePath,
            string? regexPattern = null)
        {
            this.ZipFilePath = zipFilePath;
            this.ExtractToBasePath = extractToBasePath;
            this.RegexPattern = CompilePattern(regexPattern);

            this.directoryConstructor = new(this.CreateDirectoryIfNotExist);
        }

        internal static Regex? CompilePattern(string? regexPattern) =>
#if NET20 || NET35
            string.IsNullOrEmpty(regexPattern) ? null : new Regex(regexPattern, RegexOptions.Compiled);
#else
            string.IsNullOrWhiteSpace(regexPattern) ? null : new Regex(regexPattern, RegexOptions.Compiled);
#endif

        public void Started()
        {
            this.directoryConstructor.Clear();
            this.OnStarted();
        }

        protected virtual void OnStarted()
        {
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
            FileSystemAccessor.OpenForOverwriteFile(path, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position)
        {
        }

        public void Finished()
        {
            this.directoryConstructor.Clear();
            this.OnFinished();
        }

        protected virtual void OnFinished()
        {
        }
    }
}
