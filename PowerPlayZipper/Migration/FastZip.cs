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
