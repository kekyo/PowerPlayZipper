using System;
using System.IO;
using System.Text.RegularExpressions;

using PowerPlayZipper.Advanced;
using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class BypassProcessingUnzipperTraits : DefaultUnzipperTraits
    {
        public BypassProcessingUnzipperTraits(
            string zipFilePath, string extractToBasePath, string? regexPattern) :
            base(zipFilePath, extractToBasePath, regexPattern)
        { }

        public event EventHandler<ProcessingEventArgs>? Processing;

        public override void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position) =>
            this.Processing?.Invoke(this, new ProcessingEventArgs(entry, state, position));
    }
}
