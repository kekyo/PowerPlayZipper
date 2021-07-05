using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PowerPlayZipper.Internal
{
    internal sealed class ReadOnlyRangedStream : Stream
    {
        private readonly ReadOnlyRangedStreamFactory factory;
        private readonly FileStream stream;
        private long initialPosition = 0;
        private long constrainedSize = 0;

        internal ReadOnlyRangedStream(
            ReadOnlyRangedStreamFactory factory, string path, int streamBufferSize)
        {
            this.factory = factory;
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, streamBufferSize, true);
        }

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

        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var size = ((this.Position + count) >= this.constrainedSize) ?
                (this.constrainedSize - this.Position) :
                count;
            return this.stream.ReadAsync(buffer, offset, (int)size, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotImplementedException();

        public override void Flush() =>
            throw new NotImplementedException();

        public override Task FlushAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public override void Close() =>
            this.factory.Return(this);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.factory.Return(this);
            }
        }

        internal void Destroy() =>
            this.stream.Close();
    }
}
