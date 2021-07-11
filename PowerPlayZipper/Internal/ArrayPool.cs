using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal sealed class ArrayPool<T>
    {
        private const int PoolSize = 32;

        private readonly T[]?[] pool = new T[PoolSize][];
        private readonly Stack<T[]> floodPool = new Stack<T[]>();

        public readonly int ElementSize;

        public ArrayPool(int elementSize)
        {
            this.ElementSize = elementSize;

            // Preload.
            for (var index = 0; index < (PoolSize / 4); index++)
            {
                this.pool[index] = new T[this.ElementSize];
            }
        }

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public T[] Rent()
        {
            for (var index = 0; index < this.pool.Length; index++)
            {
                var array = Interlocked.Exchange(
                    ref this.pool[index], null);
                if (array != null)
                {
                    return array;
                }
            }

            lock (this.floodPool)
            {
                if (this.floodPool.Count >= 1)
                {
                    return this.floodPool.Pop();
                }
            }

            return new T[this.ElementSize];
        }

        public void Return(ref T[]? array)
        {
            Debug.Assert(array != null);

            for (var index = 0; index < this.pool.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.pool[index], array, null);
                if (parked == null)
                {
                    array = null;
                    return;
                }
            }

            lock (this.floodPool)
            {
                this.floodPool.Push(array!);
                array = null;
            }
        }

        public void Refill()
        {
            lock (this.floodPool)
            {
                if (this.floodPool.Count >= 1)
                {
                    return;
                }
            }

            var array = new T[this.ElementSize];

            for (var index = 0; index < this.pool.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.pool[index], array, null);
                if (parked == null)
                {
                    return;
                }
            }
        }
    }
}
