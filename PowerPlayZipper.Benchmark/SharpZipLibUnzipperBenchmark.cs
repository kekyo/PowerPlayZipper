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
    public class SharpZipLibUnzipperBenchmark<TArtifact>
        where TArtifact : IArtifact, new()
    {
        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(new TArtifact().ArtifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        private string? szlBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.szlBasePath = UnzipperTestCore.GetTempPath($"SZL{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(this.szlBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            FileSystemAccessor.DeleteDirectoryRecursive(this.szlBasePath!);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipBySharpZipLibAsync(this.setup!, this.szlBasePath!).AsTask();
    }
}
