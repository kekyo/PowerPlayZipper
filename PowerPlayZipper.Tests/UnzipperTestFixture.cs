///////////////////////////////////////////////////////////////////////////
//
// PowerPlayZipper - An implementation of Lightning-Fast Zip file
// compression/decompression library on .NET.
// Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    [TestFixture]
    public sealed class UnzipperTestFixture
    {
        private static readonly string artifactUrl =
            //@"https://github.com/dotnet/sourcelink/archive/4b584dbc392bb1aad49c2eb1ab84d8b489b6dccc.zip";
            @"https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip";
            
        private UnzipperTestSetup? setup;

        [SetUp]
        public Task SetUp()
        {
            this.setup = new UnzipperTestSetup(artifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        [Test]
#if NETFRAMEWORK
        public async Task Inflate()
#else
        public async Task Compare()
#endif
        {
            var now = DateTime.Now.ToString("mmssfff");
            var ppzBasePath = UnzipperTestCore.GetTempPath($"PPZ{now}");
            var szlBasePath = UnzipperTestCore.GetTempPath($"SZL{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(ppzBasePath);
            FileSystemAccessor.CreateDirectoryIfNotExist(szlBasePath);

            var sw = new Stopwatch();

            try
            {
                //////////////////////////////////////////////////////////
                // Unzip by both libs

                sw.Start();
                await UnzipperTestCore.UnzipByPowerPlayZipperAsync(this.setup!, ppzBasePath);
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Unzipper={ppzTime}");

#if !NETFRAMEWORK   // Because SharpZipLib is hard-coded non long path aware code, it will cause PathTooLongException on netfx.
                await UnzipperTestCore.UnzipBySharpZipLibAsync(this.setup!, szlBasePath);
                var szlTime = sw.Elapsed;

                Debug.WriteLine($"SharpZipLib.FastZip={szlTime}");

                Debug.WriteLine($"Multiple={(szlTime.TotalSeconds / ppzTime.TotalSeconds):F2}");

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
#endif
            }
            finally
            {
                FileSystemAccessor.DeleteDirectoryRecursive(ppzBasePath);
                FileSystemAccessor.DeleteDirectoryRecursive(szlBasePath);
            }
        }

        [Test]
        public void Profile()
        {
            var now = DateTime.Now.ToString("mmssfff");
            var ppzBasePath = UnzipperTestCore.GetTempPath($"PPZ{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(ppzBasePath);

            var sw = new Stopwatch();

            try
            {
                //////////////////////////////////////////////////////////
                // Unzip by both libs

                sw.Start();
                UnzipperTestCore.UnzipByPowerPlayZipperAsync(this.setup!, ppzBasePath).GetAwaiter().GetResult();
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Unzipper={ppzTime}");
            }
            finally
            {
                FileSystemAccessor.DeleteDirectoryRecursive(ppzBasePath);
            }
        }
    }
}
