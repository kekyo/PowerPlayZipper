using System.Runtime.CompilerServices;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class RequestInformation
    {
        public byte[]? Buffer;
        public long BufferPosition;
        public int BufferSize;
        public int BufferOffset;
        public int FileNameOffset;
        public ushort FileNameLength;
        public int CommentOffset;
        public ushort CommentLength;
        public uint CompressedSize;

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Clear()
        {
            // Will be collected by GC.
            this.Buffer = null;
#if DEBUG
            this.BufferPosition = 0;
            this.BufferSize = 0;
            this.BufferOffset = 0;
            this.FileNameOffset = 0;
            this.FileNameLength = 0;
            this.CommentOffset = 0;
            this.CommentLength = 0;
            this.CompressedSize = 0;
#endif
        }
    }
}
