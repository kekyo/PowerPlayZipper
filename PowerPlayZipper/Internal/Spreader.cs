using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    /// <summary>
    /// Fast instance spread controller.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal sealed class Spreader<T>
        where T : Poolable<T>, new()
    {
        private enum States
        {
            Run,
            Shutdown,
            Abort,
        }

        private volatile T? head;
        private volatile T? tail;
        private volatile int queueCount;
        private volatile States state = States.Run;

        private volatile int totalRequests;
        private long missed;

        public Spreader()
        {
        }
        
        public int Requests =>
            this.totalRequests;
        public long Missed =>
            this.missed;

        public void RequestShutdown() =>
            this.state = States.Shutdown;

        public void RequestAbort() =>
            this.state = States.Abort;

        /// <summary>
        /// Request for spreading an instance.
        /// </summary>
        /// <param name="request">Instance (will remove from argument)</param>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Spread(ref T? request)
        {
            Debug.Assert(request != null);

            Interlocked.Increment(ref this.totalRequests);

            // Lock-free enqueue.
            while (true)
            {
                var currentTail = this.tail;
                if (currentTail == null)
                {
                    var result = Interlocked.CompareExchange(ref this.head, request, null);
                    if (result == null)
                    {
                        Interlocked.Increment(ref this.queueCount);
                        Interlocked.CompareExchange(ref this.tail, request, null);
                        request = null;
                        break;
                    }
                }
                else
                {
                    var currentTailNext = currentTail.Next;
                    var result = Interlocked.CompareExchange(ref currentTail.Next, request, currentTailNext);
                    if (object.ReferenceEquals(result, currentTailNext))
                    {
                        Interlocked.Increment(ref this.queueCount);
                        Interlocked.CompareExchange(ref this.tail, request, currentTail);
                        request = null;
                        break;
                    }
                }
            }
        }
        
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private T? InternalTake()
        {
            while (true)
            {
                // Empty
                var currentHead = this.head;
                if (currentHead == null)
                {
                    Interlocked.Increment(ref this.missed);
                    return null;
                }
            
                // CAS lock-free chaining.
                var next = currentHead.Next;
                var result = Interlocked.CompareExchange(ref this.head, next, currentHead);
                if (object.ReferenceEquals(result, currentHead))
                {
                    var qc = Interlocked.Decrement(ref this.queueCount);
                    Debug.Assert(qc >= 0);
                    result.Next = null;
                    return result;
                }
            }
        }

        /// <summary>
        /// Take a requested instance.
        /// </summary>
        /// <returns>Instance if succeeded</returns>
        public T? Take()
        {
            while (this.state == States.Run)
            {
                if (this.InternalTake() is { } request)
                {
                    return request;
                }
            }

            while ((this.state == States.Shutdown) && (this.queueCount >= 1))
            {
                if (this.InternalTake() is { } request)
                {
                    return request;
                }
            }

            return null;
        }

        public override string ToString() =>
            $"Requests={this.Requests}, Missed={this.Missed}";
    }
}
