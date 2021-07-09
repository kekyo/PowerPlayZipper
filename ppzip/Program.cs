using Mono.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var doZip = false;
            var unzipTargetBasePath = Directory.GetCurrentDirectory();
            var doVerbose = false;
            var doHelp = false;

            var options = new OptionSet
            {
                { "u|unzip", "Unzip target files", v => doZip = false },
                { "z|zip", "Zip target files", v => doZip = true },
                { "o", "Unzipped output directory path", v => unzipTargetBasePath = v },
                { "v|verbose", "Verbose processing", v => doVerbose = true },
                { "h|help", "Show this help", v => doHelp = true },
            };

            try
            {
                var parsed = options.Parse(args);

                if (doHelp || (parsed.Count == 0))
                {
                    Console.WriteLine();
                    Console.WriteLine($"PowerPlayZipper {ThisAssembly.AssemblyVersion} [{ThisAssembly.AssemblyInformationalVersion}]");
                    Console.WriteLine("https://github.com/kekyo/PowerPlayZipper");
                    Console.WriteLine("Copyright (c) 2021 Kouji Matsui");
                    Console.WriteLine("License under Apache v2");
                    Console.WriteLine();
                    Console.WriteLine("usage: ppzip -u [options] <zipFile> [<zipFile> ...]");
                    Console.WriteLine("usage: ppzip -z [options] <zipFile> [<file> ...]");
                    options.WriteOptionDescriptions(Console.Out);
                    return 1;
                }

                if (!doZip)
                {
                    var unzipper = new Unzipper();
                    if (doVerbose)
                    {
                        unzipper.Processing += (s, e) =>
                            Console.WriteLine($"Unzipping: {e.Entry.NormalizedFileName}");
                    }

                    foreach (var zipFilePath in parsed)
                    {
                        var result = await unzipper.
                            UnzipAsync(zipFilePath, unzipTargetBasePath).
                            ConfigureAwait(false);

                        Console.WriteLine($"{zipFilePath}: {result}");
                    }
                }
                else
                {
                    // TODO:
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Marshal.GetHRForException(ex);
            }
        }
    }
}
