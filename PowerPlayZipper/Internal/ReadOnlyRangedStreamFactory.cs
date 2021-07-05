using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal sealed class ReadOnlyRangedStreamFactory : IDisposable
    {
        private readonly string path;
        private readonly int streamBufferSize;
#if DEBUG
        private int leaseCount = 0;
#endif

        private readonly Stack<ReadOnlyRangedStream> streams = new Stack<ReadOnlyRangedStream>();

        public ReadOnlyRangedStreamFactory(string path, int streamBufferSize)
        {
            this.path = path;
            this.streamBufferSize = streamBufferSize;
        }

        public void Dispose()
        {
            while (this.streams.Count >= 1)
            {
                this.streams.Pop().Destroy();
            }
        }

        public ReadOnlyRangedStream Rent(long initialPosition, long constrainedSize)
        {
            ReadOnlyRangedStream? stream = default;
            lock (this.streams)
            {
                if (this.streams.Count >= 1)
                {
                    stream = this.streams.Pop();
                }
            }

            if (stream == default)
            {
                stream = new ReadOnlyRangedStream(this, this.path, this.streamBufferSize);
#if DEBUG
                Interlocked.Increment(ref this.leaseCount);
#endif
            }

            stream.SetRange(initialPosition, constrainedSize);
            return stream;
        }

        internal void Return(ReadOnlyRangedStream stream)
        {
            lock (this.streams)
            {
                this.streams.Push(stream);
#if DEBUG
                Interlocked.Decrement(ref this.leaseCount);
#endif
            }
        }

#if DEBUG
        public override string ToString() =>
            $"LeaseCount={this.leaseCount}";
#endif
    }
}
