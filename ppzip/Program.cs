using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Mono.Options;
using PowerPlayZipper.Advanced;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    public static class Program
    {
        private static async Task ExecuteUnzipAsync(
            string unzipTargetBasePath,
            int? maxParallelCount,
            bool doVerbose,
            bool doShowDebugStatistics,
            IReadOnlyList<string> parsed)
        {
            var unzipper = new Unzipper();
            if (maxParallelCount is { } mpc)
            {
                unzipper.MaxParallelCount = mpc;
            }

            if (doVerbose)
            {
                unzipper.Processing += (s, e) =>
                {
                    if ((e.Entry.CompressionMethod != CompressionMethods.Directory) &&
                        (e.State == ProcessingStates.Done))
                    {
                        Console.WriteLine($"  {e.Entry.CompressionMethod switch { CompressionMethods.Deflate => "Inflated", _ => "Stored  " }} : \"{e.Entry.NormalizedFileName}\" [{e.Entry.OriginalSize.ToBinaryPrefixString()}]");
                    }
                };
            }

            for (var index = 0; index < parsed.Count; index++)
            {
                var zipFilePath = parsed[index];

                if (doVerbose)
                {
                    Console.WriteLine($"Unzipping: \"{zipFilePath}\" ...");
                    Console.WriteLine();
                }
                else
                {
                    Console.Write($"{zipFilePath}: Unzipping ...");
                }

#if NET35_OR_GREATER
                var features = LongPathAwareUnzippingFeatures.Create(zipFilePath, unzipTargetBasePath);
#else
                var features = new DefaultUnzippingFeatures(zipFilePath, unzipTargetBasePath);
#endif
                var result = await unzipper.UnzipAsync(features).ConfigureAwait(false);

                if (doVerbose)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Unzipped: \"{zipFilePath}\"");
                }
                else
                {
                    Console.WriteLine(" Done.");
                }

                Console.WriteLine($"  Elapsed    : {result.Elapsed}");
                Console.WriteLine($"  Files      : {result.TotalFiles} [{(double)result.TotalFiles / result.Elapsed.TotalSeconds:F2} files/sec]");
                Console.WriteLine($"  Compressed : {result.TotalCompressedSize.ToBinaryPrefixString()} [{(result.TotalCompressedSize / result.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec]");
                Console.WriteLine($"  Expanded   : {result.TotalOriginalSize.ToBinaryPrefixString()} [{(result.TotalOriginalSize / result.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec]");
                Console.WriteLine($"  Ratio      : {(double)result.TotalOriginalSize / result.TotalCompressedSize * 100:F2} %");
                Console.WriteLine($"  Parallel   : {result.ParallelCount} [{unzipper.MaxParallelCount}]");
                if (doShowDebugStatistics)
                {
                    Console.WriteLine($"  Stats      : {result.InternalStats}");
                }

                if ((index + 1) < parsed.Count)
                {
                    Console.WriteLine();
                }
            }
        }

        private static void WriteUsage(OptionSet options)
        {
            Console.WriteLine();
            Console.WriteLine($"PowerPlayZipper {ThisAssembly.AssemblyVersion} [{ThisAssembly.AssemblyMetadata.TargetFramework}] [{ThisAssembly.AssemblyInformationalVersion}]");
            Console.WriteLine("https://github.com/kekyo/PowerPlayZipper");
            Console.WriteLine("Copyright (c) 2021 Kouji Matsui");
            Console.WriteLine("License under Apache v2");
            Console.WriteLine();
            Console.WriteLine("usage: ppzip -u [options] <zipFile> [<zipFile> ...]");
            Console.WriteLine("usage: ppzip -z [options] <zipFile> [<file> ...]");

            options.WriteOptionDescriptions(Console.Out);
        }

        public static async Task<int> Main(string[] args)
        {
            bool? doZip = null;
            var unzipTargetBasePath = Directory.GetCurrentDirectory();
            int? maxParallelCount = null;
            var doVerbose = false;
            var doShowDebugStatistics = false;
            var doHelp = false;

            var options = new OptionSet
            {
                { "u|unzip", "Unzip target files", v => doZip = false },
                { "z|zip", "Zip target files", v => doZip = true },
                { "o=", "Unzipped output directory path", v => unzipTargetBasePath = v },
                { "p|parallel", "Maximum parallel count", (int v) => maxParallelCount = v },
                { "v|verbose", "Verbose processing", v => doVerbose = true },
                { "s|statistics", "Show debug statistics", v => doShowDebugStatistics = true },
                { "h|help", "Show this help", v => doHelp = true },
            };

            try
            {
                var parsed = options.Parse(args);

                if (doHelp || (doZip == null) || (maxParallelCount < 1) || (parsed.Count == 0))
                {
                    WriteUsage(options);
                    return 1;
                }

                if (doZip == false)
                {
                    await ExecuteUnzipAsync(
                        unzipTargetBasePath, maxParallelCount,
                        doVerbose, doShowDebugStatistics,
                        parsed).
                        ConfigureAwait(false);
                }
                else
                {
                    // TODO:
                }

                return 0;
            }
            catch (OptionException)
            {
                WriteUsage(options);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Marshal.GetHRForException(ex);
            }
        }
    }
}
