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
            UnzipperTestSetup setup, string basePath)
        {
            var unzipper = new Unzipper();
            unzipper.ParallelCount = 1;
            await unzipper.UnzipAsync(setup.ZipFilePath, basePath);
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
