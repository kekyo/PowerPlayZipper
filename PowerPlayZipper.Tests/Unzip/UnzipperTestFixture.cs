﻿///////////////////////////////////////////////////////////////////////////
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

namespace PowerPlayZipper.Unzip
{
    [TestFixture]
    public sealed class UnzipperTestFixture
    {
        private Configurator? configuration;

        [SetUp]
        public Task SetUp()
        {
            this.configuration = new Configurator(Constant.ArtifactUrl);
            return this.configuration.SetUpAsync().AsTask();
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
                await UnzipperTestCore.UnzipByPowerPlayZipperAsync(
                    this.configuration!.ZipFilePath, ppzBasePath);
                var ppzTime = sw.Elapsed;

                Debug.WriteLine($"PowerPlayZipper.Unzipper={ppzTime}");

#if !NETFRAMEWORK   // Because SharpZipLib is hard-coded non long path aware code, it will cause PathTooLongException on netfx.
                await UnzipperTestCore.UnzipBySharpZipLibAsync(
                    this.configuration!.ZipFilePath, szlBasePath);
                var szlTime = sw.Elapsed;

                Debug.WriteLine($"SharpZipLib.FastZip={szlTime}");

                Debug.WriteLine($"Multiple={(szlTime.TotalSeconds / ppzTime.TotalSeconds):F2}");

                //////////////////////////////////////////////////////////
                // Check unzipped files

                TestUtilities.AssertCompareFiles(ppzBasePath, szlBasePath);
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
                UnzipperTestCore.UnzipByPowerPlayZipperAsync(
                    this.configuration!.ZipFilePath, ppzBasePath).
                    GetAwaiter().
                    GetResult();
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
