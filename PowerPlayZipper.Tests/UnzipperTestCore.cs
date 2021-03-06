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
using System.IO;
using System.Threading.Tasks;

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    public static class UnzipperTestCore
    {
        public static string GetTempPath(string suffix, string? basePath = null) =>
            FileSystemAccessor.CombinePath(
                basePath ?? Path.GetTempPath(),
                $"Unzipper_{suffix}");

        public static async ValueTask UnzipByPowerPlayZipperAsync(
            UnzipperTestSetup setup, string basePath, int pcount = -1)
        {
            var unzipper = new Unzipper();
            if (pcount >= 1)
            {
                unzipper.MaxParallelCount = pcount;
            }
            var result = await unzipper.UnzipAsync(setup.ZipFilePath, basePath);
            Console.WriteLine(result.PrettyPrint);
        }

        public static ValueTask UnzipBySharpZipLibAsync(
            UnzipperTestSetup setup, string basePath)
        {
            var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fastZip.ExtractZip(setup.ZipFilePath, basePath, "");
            return default;
        }

        public static ValueTask UnzipByZipFileAsync(
            UnzipperTestSetup setup, string basePath)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(setup.ZipFilePath, basePath);
            return default;
        }
    }
}
