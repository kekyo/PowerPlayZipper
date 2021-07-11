using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal sealed class Spreader<T>
        where T : class
    {
        private const int ReserveSize = 4;

        private readonly T?[] reserves = new T[ReserveSize];
        private readonly Queue<T> floodQueue = new Queue<T>();
        private volatile bool isAborting;

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Post(ref T? value)
        {
            Debug.Assert(value != null);

            for (var index = 0; index < this.reserves.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.reserves[index], value, null);
                if (parked == null)
                {
                    value = null;
                    return;
                }
            }

            lock (this.floodQueue)
            {
                this.floodQueue.Enqueue(value!);
                value = null;
            }
        }

        public T? Take()
        {
            while (!this.isAborting)
            {
                for (var index = 0; index < this.reserves.Length; index++)
                {
                    var reserved = Interlocked.Exchange(
                        ref this.reserves[index], null);
                    if (reserved != null)
                    {
                        return reserved;
                    }
                }

                lock (this.floodQueue)
                {
                    if (this.floodQueue.Count >= 1)
                    {
                        return this.floodQueue.Dequeue();
                    }
                }
            }

            return null;
        }

        public void RequestAbort() =>
            this.isAborting = true;
    }
}
