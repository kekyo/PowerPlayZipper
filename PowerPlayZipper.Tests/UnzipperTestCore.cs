using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    public static class UnzipperTestCore
    {
        public static string GetTempPath(string suffix) =>
            Path.Combine(Path.GetTempPath(), $"Unzipper_{suffix}");

        public static async ValueTask UnzipByPowerPlayZipperAsync(
            UnzipperTestSetup setup, string basePath, int pcount = -1)
        {
            var unzipper = new Unzipper();
            if (pcount >= 1)
            {
                unzipper.MaxParallelCount = pcount;
            }
            var features = LongPathAwareUnzippingFeatures.Create(setup.ZipFilePath, basePath);
            var result = await unzipper.UnzipAsync(features);
            Console.WriteLine(result.PrettyPrint);
        }

        public static ValueTask UnzipBySharpZipLibAsync(
            UnzipperTestSetup setup, string basePath)
        {
            var fastZip = new FastZip();
            fastZip.ExtractZip(setup.ZipFilePath, basePath, "");
            return default;
        }
    }
}
