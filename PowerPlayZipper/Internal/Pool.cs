using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal abstract class Poolable<TDerived>
        where TDerived : class, new()
    {
        internal volatile TDerived? Next;
    }
    
    /// <summary>
    /// Fast object instance lock-free pooler.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal sealed class Pool<T>
        where T : Poolable<T>, new()
    {
        private readonly int maxPoolCount;
        private volatile T? head;
        private volatile int poolCount;
        
        private volatile int floods;
        private volatile int refills;
        private volatile int missed;
        private volatile int put;
        private volatile int got;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="preload"></param>
        /// <param name="maxPoolCount"></param>
        public Pool(int preload, int maxPoolCount)
        {
            Debug.Assert(preload >= 1);
            Debug.Assert(maxPoolCount >= 1);
            
            this.maxPoolCount = maxPoolCount;
            this.poolCount = preload;
            
            var value = new T();
            this.head = value;

            if (preload >= 2)
            {
                for (var index = 1; index < preload; index++)
                {
                    var next = new T();
                    value.Next = next;
                    value = next;
                }
            }
        }

        public int Current =>
            this.poolCount;
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
            while (true)
            {
                var currentHead = this.head;

                // Empty: Generate by optimistics.
                if (currentHead == null)
                {
                    Interlocked.Increment(ref this.missed);
                    return new T();
                }

                // CAS lock-free chaining.
                var next = currentHead.Next;
                var result = Interlocked.CompareExchange(ref this.head, next, currentHead);
                if (object.ReferenceEquals(result, currentHead))
                {
                    var pc = Interlocked.Decrement(ref this.poolCount);
                    Debug.Assert(pc >= 0);
                    Interlocked.Increment(ref this.got);
                    result.Next = null;
                    return result;
                }
            }
        }

        /// <summary>
        /// Return an instance.
        /// </summary>
        /// <param name="value">Instance (will remove from argument)</param>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Return(ref T? value)
        {
            Debug.Assert(value != null);
            
            while (this.poolCount < this.maxPoolCount)
            {
                // Set next reference.
                var currentHead = this.head;
                value!.Next = currentHead;
                
                // CAS lock-free chaining.
                var result = Interlocked.CompareExchange(ref this.head, value, currentHead);
                if (object.ReferenceEquals(result, currentHead))
                {
                    Interlocked.Increment(ref this.poolCount);
                    Interlocked.Increment(ref this.put);
                    value = null;
                    return;
                }
            }

            Interlocked.Increment(ref this.floods);
            value!.Next = null;
            value = null;
        }

        /// <summary>
        /// Refill an instance if required.
        /// </summary>
        public void Refill(int count)
        {
            // TODO: Improve: pre-construct links.
            
            for (var index = 0; index < count; index++)
            {
                // Optimistic: Non atomic overflow check.
                if (this.poolCount >= this.maxPoolCount)
                {
                    break;
                }

                var value = new T();

                while (true)
                {
                    // Set next reference.
                    var currentHead = this.head;
                    value!.Next = currentHead;

                    // CAS lock-free chaining.
                    var result = Interlocked.CompareExchange(ref this.head, value, currentHead);
                    if (object.ReferenceEquals(result, currentHead))
                    {
                        Interlocked.Increment(ref this.poolCount);
                        Interlocked.Increment(ref this.refills);
                        break;
                    }
                }
            }
        }

        public override string ToString() =>
            $"Current={this.Current}, Got={this.Got}, Put={this.Put}, Missed={this.Missed}, Floods={this.floods}, Refills={this.Refills}";
    }
}
