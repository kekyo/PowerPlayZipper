using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Internal.Unzip;

namespace PowerPlayZipper.Advanced
{
    public class DefaultUnzippingFileFeatures : IUnzippingFileFeatures
    {
        private readonly DirectoryConstructor directoryConstructor = new();
        private readonly string zipFilePath;
        private readonly string extractToBasePath;
        private readonly Regex? regexPattern;

        public DefaultUnzippingFileFeatures(string zipFilePath, string extractToBasePath, string? regexPattern = null)
        {
            this.zipFilePath = zipFilePath;
            this.extractToBasePath = extractToBasePath;
            this.regexPattern = (regexPattern != null) ? new Regex(regexPattern, RegexOptions.Compiled) : null;
        }

        public virtual Stream OpenForRead(int recommendedBufferSize) =>
            new FileStream(this.zipFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, recommendedBufferSize);

        public virtual bool IsRequiredProcessing(ZippedFileEntry entry) =>
            regexPattern?.IsMatch(entry.NormalizedFileName) ?? true;

        public virtual string GetTargetPath(ZippedFileEntry entry) =>
            Path.Combine(this.extractToBasePath, entry.NormalizedFileName);

        public virtual void ConstructDirectory(string directoryPath) =>
            this.directoryConstructor.CreateIfNotExist(directoryPath);

        public virtual Stream OpenForWrite(string path, int recommendedBufferSize) =>
            new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, recommendedBufferSize);
    }
}
