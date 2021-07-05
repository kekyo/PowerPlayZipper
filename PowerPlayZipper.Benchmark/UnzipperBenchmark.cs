using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    [SimpleJob(RuntimeMoniker.Net48)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class UnzipperBenchmark
    {
        // .NET docs repo has too long path names, so test will be failed in net461.
        // The unit test delegation process doesn't have a manifest for long path name behavior.
        // https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file
        private static readonly string artifactUrl =
            @"https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip";

        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(artifactUrl);
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
