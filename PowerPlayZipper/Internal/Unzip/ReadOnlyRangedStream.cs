﻿using System;
using System.IO;

namespace PowerPlayZipper.Internal.Unzip
{
    /// <summary>
    /// Read only range constrained stream.
    /// </summary>
    internal sealed class ReadOnlyRangedStream : Stream
    {
        private readonly FileStream stream;
        private long initialPosition = 0;
        private long constrainedSize;

        internal ReadOnlyRangedStream(string path, int streamBufferSize)
        {
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, streamBufferSize);
            this.constrainedSize = this.stream.Length;
        }

        /// <summary>
        /// Set range and begin constraint.
        /// </summary>
        /// <param name="initialPosition">Initial position on the file</param>
        /// <param name="constrainedSize">Constrained size</param>
        public void SetRange(long initialPosition, long constrainedSize)
        {
            this.stream.Position = initialPosition;
            if ((initialPosition + constrainedSize) >= this.stream.Length)
            {
                throw new IOException("Reached end of file.");
            }
            this.initialPosition = initialPosition;
            this.constrainedSize = constrainedSize;
        }

        /// <summary>
        /// Reset and make free access.
        /// </summary>
        /// <param name="initialPosition">Initial position on the file</param>
        public void ResetRange(long initialPosition)
        {
            this.stream.Position = initialPosition;
            this.constrainedSize = this.stream.Length;
        }

        public override bool CanSeek =>
            false;

        public override long Length =>
            this.constrainedSize;

        public override long Position
        {
            get => this.stream.Position - this.initialPosition;
            set => throw new NotImplementedException();
        }

        public override bool CanRead =>
            true;

        public override bool CanWrite =>
            false;

        public override int Read(byte[] array, int offset, int count)
        {
            var size = ((this.Position + count) >= this.constrainedSize) ?
                (this.constrainedSize - this.Position) :
                count;
            return this.stream.Read(array, offset, (int)size);
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotImplementedException();

        public override void Flush() =>
            throw new NotImplementedException();

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();
    }
}
