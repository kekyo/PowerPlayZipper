using System.Runtime.CompilerServices;

namespace PowerPlayZipper.Internal.Unzip
{
    /// <summary>
    /// Request packet from parser to worker.
    /// </summary>
    internal sealed class RequestInformation :
        Poolable<RequestInformation>
    {
        /// <summary>
        /// Stored first time data.
        /// </summary>
        public byte[]? Buffer;

        /// <summary>
        /// Read and stored into buffer from zip file position.
        /// </summary>
        public long BufferPosition;

        /// <summary>
        /// Read size (<= Buffer.Length)
        /// </summary>
        public int BufferSize;

        /// <summary>
        /// Base buffer offset of current file entry.
        /// </summary>
        public int BufferOffsetOfEntry;

        /// <summary>
        /// File name offset in buffer.
        /// </summary>
        public int FileNameOffset;

        /// <summary>
        /// File name length (in byte).
        /// </summary>
        public ushort FileNameLength;

        /// <summary>
        /// Comment offset in buffer.
        /// </summary>
        public int CommentOffset;

        /// <summary>
        /// Comment length (in byte).
        /// </summary>
        public ushort CommentLength;

        /// <summary>
        /// Compressed data size.
        /// </summary>
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
            this.BufferOffsetOfEntry = 0;
            this.FileNameOffset = 0;
            this.FileNameLength = 0;
            this.CommentOffset = 0;
            this.CommentLength = 0;
            this.CompressedSize = 0;
#endif
        }
    }
}
