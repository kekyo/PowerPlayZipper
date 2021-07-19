using System;
using PowerPlayZipper.Advanced;

namespace PowerPlayZipper
{
    internal sealed class LongPathAwareUnzippingFeatures : DefaultUnzippingFeatures
    {
        private LongPathAwareUnzippingFeatures(string zipFilePath, string extractToBasePath) :
            base(zipFilePath, extractToBasePath)
        {
        }

#if NET35_OR_GREATER
        public override System.IO.Stream OpenForReadZipFile(int recommendedBufferSize) =>
            Alphaleonis.Win32.Filesystem.File.OpenRead(this.ZipFilePath);

        public override string GetTargetPath(ZippedFileEntry entry) =>
            Alphaleonis.Win32.Filesystem.Path.Combine(this.ExtractToBasePath, entry.NormalizedFileName);

        public override void CreateDirectoryIfNotExist(string directoryPath)
        {
            if (!Alphaleonis.Win32.Filesystem.Directory.Exists(directoryPath))
            {
                try
                {
                    Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(directoryPath);
                }
                catch
                {
                }
            }
        }

        public override System.IO.Stream OpenForWriteFile(string path, int recommendedBufferSize) =>
            Alphaleonis.Win32.Filesystem.File.Create(path, recommendedBufferSize);

        public static IUnzippingFeatures Create(string zipFilePath, string extractToBasePath) =>
            (Environment.OSVersion.Platform == PlatformID.Win32NT) ?
                new LongPathAwareUnzippingFeatures(zipFilePath, extractToBasePath) :
                new DefaultUnzippingFeatures(zipFilePath, extractToBasePath);
#else
        public static IUnzippingFeatures Create(string zipFilePath, string extractToBasePath) =>
                new DefaultUnzippingFeatures(zipFilePath, extractToBasePath);
#endif
    }
}
