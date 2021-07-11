using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    /// <summary>
    /// Fast object instance pooler.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal sealed class Pool<T>
        where T : class, new()
    {
        private const int PoolSize = 32;

        private readonly T?[] pool = new T[PoolSize];
        private readonly Stack<T> floodPool = new Stack<T>();

        public Pool()
        {
            // Preload.
            for (var index = 0; index < (PoolSize / 4); index++)
            {
                this.pool[index] = new T();
            }
        }

        /// <summary>
        /// Rent an instance.
        /// </summary>
        /// <returns>Instance</returns>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public T Rent()
        {
            for (var index = 0; index < this.pool.Length; index++)
            {
                var value = Interlocked.Exchange(
                    ref this.pool[index], null);
                if (value != null)
                {
                    return value;
                }
            }

            lock (this.floodPool)
            {
                if (this.floodPool.Count >= 1)
                {
                    return this.floodPool.Pop();
                }
            }

            return new T();
        }

        /// <summary>
        /// Return an instance.
        /// </summary>
        /// <param name="value">Instance (will remove from argument)</param>
        public void Return(ref T? value)
        {
            Debug.Assert(value != null);

            for (var index = 0; index < this.pool.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.pool[index], value, null);
                if (parked == null)
                {
                    value = null;
                    return;
                }
            }

            lock (this.floodPool)
            {
                this.floodPool.Push(value!);
                value = null;
            }
        }

        /// <summary>
        /// Refill an instance if required.
        /// </summary>
        public void Refill()
        {
            lock (this.floodPool)
            {
                if (this.floodPool.Count >= 1)
                {
                    return;
                }
            }

            var value = new T();

            for (var index = 0; index < this.pool.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.pool[index], value, null);
                if (parked == null)
                {
                    return;
                }
            }
        }
    }
}
