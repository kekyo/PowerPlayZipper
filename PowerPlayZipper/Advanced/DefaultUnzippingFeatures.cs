using System;
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Internal.Unzip;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzippingFeatures : IUnzippingFeatures
    {
        private readonly DirectoryConstructor directoryConstructor = new();
        private readonly string zipFilePath;
        private readonly string extractToBasePath;
        private readonly Regex? regexPattern;

        public DefaultUnzippingFeatures(string zipFilePath, string extractToBasePath, string? regexPattern = null)
        {
            this.zipFilePath = zipFilePath;
            this.extractToBasePath = extractToBasePath;
            this.regexPattern = (regexPattern != null) ? new Regex(regexPattern, RegexOptions.Compiled) : null;
        }

        public event EventHandler<ProcessingEventArgs>? Processing;

        public virtual Stream OpenForReadZipFile(int recommendedBufferSize) =>
            new FileStream(this.zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        public virtual bool IsRequiredProcessing(ZippedFileEntry entry) =>
            this.regexPattern?.IsMatch(entry.NormalizedFileName) ?? true;

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            Path.Combine(this.extractToBasePath, entry.NormalizedFileName);

        public virtual void CreateDirectory(string directoryPath) =>
            this.directoryConstructor.CreateIfNotExist(directoryPath);

        public virtual Stream OpenForWriteFile(string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position) =>
            this.Processing?.Invoke(this, new ProcessingEventArgs(entry, state, position));
    }
}
