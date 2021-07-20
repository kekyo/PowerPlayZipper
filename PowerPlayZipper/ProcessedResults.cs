﻿using System;
using System.ComponentModel;
using PowerPlayZipper.Utilities;

namespace PowerPlayZipper
{
    public sealed class ProcessedResults
    {
        public ProcessedResults(
            int totalFiles, long totalCompressedSize, long totalOriginalSize,
            TimeSpan elapsed, int parallelCount, string internalStats)
        {
            this.TotalFiles = totalFiles;
            this.TotalCompressedSize = totalCompressedSize;
            this.TotalOriginalSize = totalOriginalSize;
            this.Elapsed = elapsed;
            this.ParallelCount = parallelCount;
            this.InternalStats = internalStats;
        }

        public int TotalFiles { get; }
        public long TotalCompressedSize { get; }
        public long TotalOriginalSize { get; }
        public TimeSpan Elapsed { get; }
        public int ParallelCount { get; }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public string InternalStats { get; }

        public string PrettyPrint =>
            $"Files={this.TotalFiles}, CompressedSize={this.TotalCompressedSize.ToBinaryPrefixString()}, OriginalSize={this.TotalOriginalSize.ToBinaryPrefixString()}, Ratio={(double)this.TotalCompressedSize/this.TotalOriginalSize*100:F2}%, Elapsed={this.Elapsed}, CompressedDataRate={(this.TotalCompressedSize/this.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec, OriginalDataRate={(this.TotalOriginalSize/this.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec, {this.InternalStats}";

        public override string ToString() =>
            $"Files={this.TotalFiles}, CompressedSize={this.TotalCompressedSize.ToBinaryPrefixString()}, OriginalSize={this.TotalOriginalSize.ToBinaryPrefixString()}, Ratio={(double)this.TotalCompressedSize/this.TotalOriginalSize*100:F2}%, Elapsed={this.Elapsed}, CompressedDataRate={(this.TotalCompressedSize/this.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec, OriginalDataRate={(this.TotalOriginalSize/this.Elapsed.TotalSeconds).ToBinaryPrefixString()}/sec";
    }
}
