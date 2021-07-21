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

using System.Text;

namespace PowerPlayZipper.Migration
{
    /// <summary>
    /// Migration class for System.IO.Compression.ZipFile.
    /// </summary>
    public static class ZipFile
    {
        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, true);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable);
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="overwriteFiles">Overwrite if exists</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            var unzipper = new Unzipper(DefaultFileNameEncodings.SystemDefaultIfApplicable);
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="encoding">Default file name encoding</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, Encoding encoding)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, true);
            var unzipper = new Unzipper
            {
                DefaultFileNameEncoding = encoding
            };
            unzipper.Unzip(traits);
        }

        /// <summary>
        /// Extract zip archive.
        /// </summary>
        /// <param name="sourceArchiveFileName">Zip archive file path</param>
        /// <param name="destinationDirectoryName">Store path</param>
        /// <param name="encoding">Default file name encoding</param>
        /// <param name="overwriteFiles">Overwrite if exists</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName, string destinationDirectoryName, Encoding encoding, bool overwriteFiles)
        {
            var traits = new ZipFileMigrationUnzipperTraits(
                sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            var unzipper = new Unzipper
            {
                DefaultFileNameEncoding = encoding
            };
            unzipper.Unzip(traits);
        }
    }
}
