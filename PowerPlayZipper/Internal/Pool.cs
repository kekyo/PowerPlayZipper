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
        private readonly Stack<T> floodPool = new();
        
        private volatile int returns;
        private volatile int floods;
        private volatile int refills;
        private volatile int missed;
        private volatile int put;
        private volatile int got;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Pool()
        {
            // Preload.
            for (var index = 0; index < (PoolSize / 4); index++)
            {
                this.pool[index] = new T();
            }
        }

        public int Returns =>
            this.returns;
        public int Floods =>
            this.floods;
        public int Refills =>
            this.refills;
        public int Missed =>
            this.missed;
        public int Put =>
            this.put;
        public int Got =>
            this.got;

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
                    Interlocked.Increment(ref this.got);
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

            Interlocked.Increment(ref this.missed);

            return new T();
        }

        /// <summary>
        /// Return an instance.
        /// </summary>
        /// <param name="value">Instance (will remove from argument)</param>
        public void Return(ref T? value)
        {
            Debug.Assert(value != null);

            Interlocked.Increment(ref this.returns);

            for (var retry = 0; retry < 4; retry++)
            {
                for (var index = 0; index < this.pool.Length; index++)
                {
                    var parked = Interlocked.CompareExchange(
                        ref this.pool[index], value, null);
                    if (parked == null)
                    {
                        Interlocked.Increment(ref this.put);
                        value = null;
                        return;
                    }
                }
            }

            lock (this.floodPool)
            {
                this.floodPool.Push(value!);
                value = null;
            }
            
            Interlocked.Increment(ref this.floods);
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

            Interlocked.Increment(ref this.refills);

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
            
            lock (this.floodPool)
            {
                this.floodPool.Push(value);
            }
        }

        public override string ToString() =>
            $"Returns={this.Returns}, Floods={this.Floods}, Refills={this.Refills}, Got={this.Got}, Put={this.Put}, Missed={this.Missed}";
    }
}
