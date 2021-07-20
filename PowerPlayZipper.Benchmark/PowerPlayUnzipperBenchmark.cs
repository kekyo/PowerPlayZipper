using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PowerPlayZipper.Compatibility;
using System;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class PowerPlayUnzipperBenchmark<TArtifact>
        where TArtifact : IArtifact, new()
    {
        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(new TArtifact().ArtifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        private string? ppzBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.ppzBasePath = UnzipperTestCore.GetTempPath($"PPZ{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(this.ppzBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            FileSystemAccessor.DeleteDirectoryRecursive(this.ppzBasePath!);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipByPowerPlayZipperAsync(this.setup!, this.ppzBasePath!).AsTask();
    }
}
