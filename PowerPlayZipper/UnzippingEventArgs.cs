using System;

namespace PowerPlayZipper
{
    public enum UnzippingStates
    {
        Begin,
        Processing,
        Done
    }
    
    public sealed class UnzippingEventArgs : EventArgs
    {
        public readonly ZippedFileEntry Entry;
        public readonly UnzippingStates State;
        public readonly long Position;

        public UnzippingEventArgs(ZippedFileEntry entry, UnzippingStates state, long position)
        {
            this.Entry = entry;
            this.State = state;
            this.Position = position;
        }

        public override string ToString() =>
            $"Unzip: {this.State}: {this.Entry.FileName}, Position={this.Position}";
    }
}
