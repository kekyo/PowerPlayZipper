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
    public class PowerPlayUnzipperBenchmark
    {
        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(Program.ArtifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        private string? ppzBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.ppzBasePath = UnzipperTestCore.GetTempPath($"PPZ{now}");

            Directory.CreateDirectory(this.ppzBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            Directory.Delete(this.ppzBasePath!, true);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipByPowerPlayZipperAsync(this.setup!, this.ppzBasePath!).AsTask();
    }
}
