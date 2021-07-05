using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    [TestFixture]
    public sealed class UnzipperTestFixture
    {
        private static readonly string artifactUrl =
            @"https://github.com/dotnet/sourcelink/archive/4b584dbc392bb1aad49c2eb1ab84d8b489b6dccc.zip";

        private UnzipperTestSetup? setup;

        [SetUp]
        public Task SetUp()
        {
            this.setup = new UnzipperTestSetup(artifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        [Test]
        public async Task Compare()
        {
            var now = DateTime.Now.ToString("mmssfff");
            var ppzBasePath = UnzipperTestCore.GetTempPath($"PPZ{now}");
            var szlBasePath = UnzipperTestCore.GetTempPath($"SZL{now}");

            Directory.CreateDirectory(ppzBasePath);
            Directory.CreateDirectory(szlBasePath);

            var sw = new Stopwatch();

            try
            {
                //////////////////////////////////////////////////////////
                // Unzip by both libs

                sw.Start();
                await UnzipperTestCore.UnzipByPowerPlayZipperAsync(this.setup!, ppzBasePath);
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Unzipper={ppzTime}");

                await UnzipperTestCore.UnzipBySharpZipLibAsync(this.setup!, szlBasePath);
                var szlTime = sw.Elapsed;

                Debug.WriteLine($"SharpZipLib.FastZip={szlTime}");

                Debug.WriteLine($"Multiple={(szlTime.TotalSeconds / ppzTime.TotalSeconds):F}");

                //////////////////////////////////////////////////////////
                // Check unzipped files

                var ppzFiles = new HashSet<string>(
                    Directory.EnumerateFiles(ppzBasePath, "*", SearchOption.AllDirectories).
                    Select(ppzFile => ppzFile.Substring(ppzBasePath.Length + 1)));
                var szlFiles = new HashSet<string>(
                    Directory.EnumerateFiles(szlBasePath, "*", SearchOption.AllDirectories).
                    Select(szlFile => szlFile.Substring(szlBasePath.Length + 1)));

                var ppzExistBySzl = new HashSet<string>(
                    ppzFiles.Where(ppzFile => szlFiles.Contains(ppzFile)));
                var szlExistByPpz = new HashSet<string>(
                    szlFiles.Where(szlFile => ppzFiles.Contains(szlFile)));

                // Matched file count is same.
                Assert.AreEqual(ppzExistBySzl.Count, ppzFiles.Count);
                Assert.AreEqual(szlExistByPpz.Count, szlFiles.Count);

                // All files have to equal.
                Parallel.ForEach(ppzFiles, file =>
                {
                    using (var ppzStream = File.OpenRead(Path.Combine(ppzBasePath, file)))
                    {
                        using (var szlStream = File.OpenRead(Path.Combine(szlBasePath, file)))
                        {
                            var ppzBuffer = new byte[65536];
                            var szlBuffer = new byte[65536];
                            var ppzRead = ppzStream.Read(ppzBuffer, 0, ppzBuffer.Length);
                            var szlRead = szlStream.Read(szlBuffer, 0, szlBuffer.Length);

                            Assert.AreEqual(ppzRead, szlRead);
                            for (var index = 0; index < ppzRead; index++)
                            {
                                if (ppzBuffer[index] != szlBuffer[index])
                                {
                                    Assert.Fail($"{file}: Differ: Index={index}");
                                }
                            }
                        }
                    }
                });
            }
            finally
            {
                Directory.Delete(ppzBasePath, true);
                Directory.Delete(szlBasePath, true);
            }
        }
    }
}
