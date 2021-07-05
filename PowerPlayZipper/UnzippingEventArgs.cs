using System;

namespace PowerPlayZipper
{
    public sealed class UnzippingEventArgs : EventArgs
    {
        public readonly ZippedFileEntry Entry;

        public UnzippingEventArgs(ZippedFileEntry entry) =>
            this.Entry = entry;

        public override string ToString() =>
            $"Unzipping: {this.Entry}";
    }
}
