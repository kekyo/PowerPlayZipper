using System;

using PowerPlayZipper.Internal;

namespace PowerPlayZipper
{
    public sealed class ProcessedResults
    {
        public ProcessedResults(
            int totalFiles, long totalCompressedSize, long totalOriginalSize, TimeSpan elapsed)
        {
            this.TotalFiles = totalFiles;
            this.TotalCompressedSize = totalCompressedSize;
            this.TotalOriginalSize = totalOriginalSize;
            this.Elapsed = elapsed;
        }

        public int TotalFiles { get; }
        public long TotalCompressedSize { get; }
        public long TotalOriginalSize { get; }
        public TimeSpan Elapsed { get; }

        public override string ToString() =>
            $"Files={this.TotalFiles}, CompressedSize={this.TotalCompressedSize.ToByteSize()}, OriginalSize={this.TotalOriginalSize.ToByteSize()}, Ratio={(double)this.TotalCompressedSize/this.TotalOriginalSize*100:F2}%, Elapsed={this.Elapsed}, CompressedDataRate={(this.TotalCompressedSize/this.Elapsed.TotalSeconds).ToByteSize()}/sec, OriginalDataRate={(this.TotalOriginalSize/this.Elapsed.TotalSeconds).ToByteSize()}/sec";
    }
}
