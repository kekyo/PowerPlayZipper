using BenchmarkDotNet.Running;

namespace PowerPlayZipper
{
    public interface IArtifact
    {
        string ArtifactUrl { get; }
    }

    public sealed class ArtifactFromDotnetDocs : IArtifact
    {
        public string ArtifactUrl =>
            @"https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip";
    }

    public sealed class ArtifactFromMixedRealityToolKit : IArtifact
    {
        public string ArtifactUrl =>
            @"https://github.com/microsoft/MixedRealityToolkit/archive/b63b40b9a4bd4e350f35986d450dd5393c6e58a0.zip";
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<PowerPlayUnzipperBenchmark<ArtifactFromDotnetDocs>>();
            BenchmarkRunner.Run<SharpZipLibUnzipperBenchmark<ArtifactFromDotnetDocs>>();
            BenchmarkRunner.Run<ZipFileUnzipperBenchmark<ArtifactFromDotnetDocs>>();

            BenchmarkRunner.Run<PowerPlayUnzipperBenchmark<ArtifactFromMixedRealityToolKit>>();
            BenchmarkRunner.Run<SharpZipLibUnzipperBenchmark<ArtifactFromMixedRealityToolKit>>();
            BenchmarkRunner.Run<ZipFileUnzipperBenchmark<ArtifactFromMixedRealityToolKit>>();
        }
    }
}
