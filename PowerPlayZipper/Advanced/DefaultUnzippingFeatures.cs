using System;
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Internal.Unzip;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzippingFeatures : IUnzippingFeatures
    {
        public readonly string ZipFilePath;
        public readonly string ExtractToBasePath;
        public readonly Regex? RegexPattern;

        public DefaultUnzippingFeatures(string zipFilePath, string extractToBasePath, string? regexPattern = null)
        {
            this.ZipFilePath = zipFilePath;
            this.ExtractToBasePath = extractToBasePath;
            this.RegexPattern = (regexPattern != null) ? new Regex(regexPattern, RegexOptions.Compiled) : null;
        }

        public event EventHandler<ProcessingEventArgs>? Processing;

        public virtual Stream OpenForReadZipFile(int recommendedBufferSize) =>
            new FileStream(this.ZipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, recommendedBufferSize);

        public virtual bool IsRequiredProcessing(ZippedFileEntry entry) =>
            this.RegexPattern?.IsMatch(entry.NormalizedFileName) ?? true;

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            Path.Combine(this.ExtractToBasePath, entry.NormalizedFileName);

        public virtual void CreateDirectoryIfNotExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch
                {
                }
            }
        }

        public virtual Stream OpenForWriteFile(string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, recommendedBufferSize);

        public virtual void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position) =>
            this.Processing?.Invoke(this, new ProcessingEventArgs(entry, state, position));
    }
}
