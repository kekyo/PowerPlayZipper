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

namespace PowerPlayZipper.Zip
{
    [TestFixture]
    public sealed class ZipperTestFixture
    {
        private Configurator? configuration;

        [SetUp]
        public Task SetUp()
        {
            this.configuration = new Configurator(Constant.ArtifactUrl);
            return this.configuration.SetUpAsync().AsTask();
        }

#if !NETFRAMEWORK
        [Test]
        public async Task Compare()
        {
            var now = DateTime.Now.ToString("mmssfff");
            var basePath = ZipperTestCore.GetTempPath($"BASE{now}");
            var ppzZipFilePath = ZipperTestCore.GetTempPath($"PPZ{now}.zip");
            var extractToBasePath = ZipperTestCore.GetTempPath($"EXT{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(basePath);

            var sw = new Stopwatch();

            try
            {
                //////////////////////////////////////////////////////////
                // Stable unzipping to setup artifacts.

                await Unzip.UnzipperTestCore.UnzipBySharpZipLibAsync(
                    this.configuration!.ZipFilePath, basePath);

                //////////////////////////////////////////////////////////
                // Zip by PowerPlayZipper

                sw.Start();
                await ZipperTestCore.ZipByPowerPlayZipperAsync(
                    this.configuration!.ZipFilePath, ppzZipFilePath);
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Zipper={ppzTime}");

                //////////////////////////////////////////////////////////
                // Stable unzipping to zipped files.

                await Unzip.UnzipperTestCore.UnzipBySharpZipLibAsync(
                    this.configuration!.ZipFilePath, basePath);

                //////////////////////////////////////////////////////////
                // Check unzipped files

                TestUtilities.AssertCompareFiles(basePath, extractToBasePath);
            }
            finally
            {
                FileSystemAccessor.DeleteDirectoryRecursive(basePath);
                FileSystemAccessor.DeleteDirectoryRecursive(extractToBasePath);
                File.Delete(ppzZipFilePath);
            }
        }
#endif

        //[Test]
        public void Profile()
        {
            var now = DateTime.Now.ToString("mmssfff");
            var basePath = ZipperTestCore.GetTempPath($"BASE{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(basePath);

            var sw = new Stopwatch();

            try
            {
                //////////////////////////////////////////////////////////
                // Zip by both libs

                sw.Start();
                ZipperTestCore.ZipByPowerPlayZipperAsync(
                    this.configuration!.ZipFilePath, basePath).
                    GetAwaiter().
                    GetResult();
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Zipper={ppzTime}");
            }
            finally
            {
                FileSystemAccessor.DeleteDirectoryRecursive(basePath);
            }
        }
    }
}
