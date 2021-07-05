using System;

namespace PowerPlayZipper
{
    public struct ProcessedResults
    {
        public readonly int TotalFiles;
        public readonly long TotalCompressedSize;
        public readonly long TotalOriginalSize;
        public readonly TimeSpan Elapsed;

        public ProcessedResults(
            int totalFiles, long totalCompressedSize, long totalOriginalSize, TimeSpan elapsed)
        {
            this.TotalFiles = totalFiles;
            this.TotalCompressedSize = totalCompressedSize;
            this.TotalOriginalSize = totalOriginalSize;
            this.Elapsed = elapsed;
        }

        public override string ToString() =>
            $"Files={this.TotalFiles}, Size=[{this.TotalCompressedSize}/{this.TotalOriginalSize}], Ratio={(double)this.TotalCompressedSize/this.TotalOriginalSize*100.0:F5}%, Duration={this.Elapsed}";
    }
}
