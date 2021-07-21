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

namespace PowerPlayZipper.Migration
{
    /// <summary>
    /// Migration class for SharpZipLib.FastZip.
    /// </summary>
    public sealed class FastZip
    {
        public delegate bool ConfirmOverwriteDelegate(string fileName);

        public enum Overwrite
        {
            Prompt = 0,
            Never = 1,
            Always = 2
        }

        public bool CreateEmptyDirectories { get; set; }

        public void ExtractZip(
            string zipFileName,
            string targetDirectory,
            Overwrite overwrite,
            ConfirmOverwriteDelegate confirmDelegate,
            string fileFilter,
            string directoryFilter,
            bool restoreDateTime,  /* TODO: */
            bool allowParentTraversal = false)
        {
            if (allowParentTraversal)
            {
                throw new ArgumentException("Not supported parent traversal feature.");
            }

            var traits = new FastZipMigrationUnzipperTraits(
                zipFileName, targetDirectory, fileFilter, directoryFilter, overwrite, confirmDelegate);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable)
            {
                IgnoreEmptyDirectoryEntry = !this.CreateEmptyDirectories
            };
            unzipper.Unzip(traits);
        }

        public void ExtractZip(
            string zipFileName,
            string targetDirectory,
            string fileFilter)
        {
            var traits = new FastZipMigrationUnzipperTraits(
                zipFileName, targetDirectory, fileFilter, null, Overwrite.Always, null);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable)
            {
                IgnoreEmptyDirectoryEntry = !this.CreateEmptyDirectories
            };
            unzipper.Unzip(traits);
        }

        //public void ExtractZip(
        //    Stream inputStream,
        //    string targetDirectory,
        //    Overwrite overwrite,
        //    ConfirmOverwriteDelegate confirmDelegate,
        //    string fileFilter,
        //    string directoryFilter,
        //    bool restoreDateTime,
        //    bool isStreamOwner,
        //    bool allowParentTraversal = false)
        //{
        //}
    }
}
