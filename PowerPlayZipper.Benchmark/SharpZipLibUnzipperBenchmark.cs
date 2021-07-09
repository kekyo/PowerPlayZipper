using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.IO;
using System.Threading.Tasks;

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

            Directory.CreateDirectory(this.szlBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            Directory.Delete(this.szlBasePath!, true);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipBySharpZipLibAsync(this.setup!, this.szlBasePath!).AsTask();
    }
}
