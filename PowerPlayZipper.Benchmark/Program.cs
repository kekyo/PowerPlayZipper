using BenchmarkDotNet.Running;

namespace PowerPlayZipper
{
    public static class Program
    {
        // .NET docs repo has too long path names, so test will be failed in net461.
        // The unit test delegation process doesn't have a manifest for long path name behavior.
        // https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file
        internal static readonly string ArtifactUrl =
            @"https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip";

        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<PowerPlayUnzipperBenchmark>();
            BenchmarkRunner.Run<SharpZipLibUnzipperBenchmark>();
        }
    }
}
