using BenchmarkDotNet.Running;

namespace PowerPlayZipper
{
    public static class Program
    {
        public static void Main(string[] args) =>
            BenchmarkRunner.Run<UnzipperBenchmark>();
    }
}
