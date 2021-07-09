using System;

namespace PowerPlayZipper
{
    public enum ProcessingStates
    {
        Begin,
        Processing,
        Done
    }
    
    public sealed class ProcessingEventArgs : EventArgs
    {
        public readonly ZippedFileEntry Entry;
        public readonly ProcessingStates State;
        public readonly long PositionOnOriginal;

        public ProcessingEventArgs(
            ZippedFileEntry entry, ProcessingStates state, long positionOnOriginal)
        {
            this.Entry = entry;
            this.State = state;
            this.PositionOnOriginal = positionOnOriginal;
        }

        public override string ToString() =>
            $"{this.State}: {this.Entry.FileName}, Position={this.PositionOnOriginal}, Percent={(double)this.PositionOnOriginal/this.Entry.OriginalSize*100.0:F3}%";
    }
}
