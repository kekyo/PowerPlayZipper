using System;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class ZipFileUnzipperBenchmark<TArtifact>
        where TArtifact : IArtifact, new()
    {
        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(new TArtifact().ArtifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        private string? zfBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.zfBasePath = UnzipperTestCore.GetTempPath($"ZF{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(this.zfBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            FileSystemAccessor.DeleteDirectoryRecursive(this.zfBasePath!);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipByZipFileAsync(this.setup!, this.zfBasePath!).AsTask();
    }
}
