using System.Collections.Generic;
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
        where T : class
    {
        private const int RequestSize = 16;

        private enum States
        {
            Run,
            Shutdown,
            Abort,
        }

        private readonly T?[] requests = new T[RequestSize];
        private readonly Queue<T> floodQueue = new();
        private volatile int count;
        private volatile States state = States.Run;

        private volatile int totalRequests;
        private volatile int floods;
        private long missed;
        
        public int Requests =>
            this.totalRequests;
        public int Floods =>
            this.floods;
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
        public void Request(ref T? request)
        {
            Debug.Assert(request != null);

            Interlocked.Increment(ref this.count);
            Interlocked.Increment(ref this.totalRequests);

            for (var index = 0; index < this.requests.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.requests[index], request, null);
                if (parked == null)
                {
                    request = null;
                    return;
                }
            }

            lock (this.floodQueue)
            {
                this.floodQueue.Enqueue(request!);
                request = null;
            }

            Interlocked.Increment(ref this.floods);
        }
        
        private T? InternalTake()
        {
            for (var retry = 0; retry < 4; retry++)
            {
                for (var index = 0; index < this.requests.Length; index++)
                {
                    var request = Interlocked.Exchange(
                        ref this.requests[index], null);
                    if (request != null)
                    {
                        var count = Interlocked.Decrement(ref this.count);
                        Debug.Assert(count >= 0);

                        return request;
                    }
                }
            }

            lock (this.floodQueue)
            {
                if (this.floodQueue.Count >= 1)
                {
                    var count = Interlocked.Decrement(ref this.count);
                    Debug.Assert(count >= 0);

                    return this.floodQueue.Dequeue();
                }
            }

            Interlocked.Increment(ref this.missed);

            return null;
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

            while ((this.state == States.Shutdown) && (this.count >= 1))
            {
                if (this.InternalTake() is { } request)
                {
                    return request;
                }
            }

            return null;
        }

        public override string ToString() =>
            $"Requests={this.Requests}, Floods={this.Floods}, Missed={this.Missed}";
    }
}
