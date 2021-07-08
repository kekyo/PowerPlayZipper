using System;

namespace PowerPlayZipper
{
    public readonly struct ProcessedResults
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
            $"Files={this.TotalFiles}, Size=[{this.TotalCompressedSize}B,{this.TotalOriginalSize}B], Ratio={(double)this.TotalCompressedSize/this.TotalOriginalSize*100.0:F5}%, Elapsed={this.Elapsed}, Rate=[{this.TotalCompressedSize/this.Elapsed.TotalSeconds:F5}B/sec,{this.TotalOriginalSize/this.Elapsed.TotalSeconds:F5}B/sec]";
    }
}
