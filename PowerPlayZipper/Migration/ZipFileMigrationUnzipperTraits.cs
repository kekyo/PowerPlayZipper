﻿using System.IO;

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Migration
{
    internal sealed class ZipFileMigrationUnzipperTraits : DefaultUnzipperTraits
    {
        private readonly bool overwriteFiles;

        public ZipFileMigrationUnzipperTraits(
            string zipFilePath, string extractToBasePath, bool overwriteFiles) :
            base(zipFilePath, extractToBasePath) =>
            this.overwriteFiles = overwriteFiles;

        public override Stream? OpenForWriteFile(string path, int recommendedBufferSize) =>
            FileSystemAccessor.OpenForWriteFile(
                path, overwriteFiles, recommendedBufferSize);
    }
}