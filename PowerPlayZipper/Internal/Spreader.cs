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
        private const int RequestSize = 4;

        private enum States
        {
            Run,
            Shutdown,
            Abort,
        }

        private readonly T?[] requests = new T[RequestSize];
        private readonly Queue<T> floodQueue = new Queue<T>();
        private volatile int count;
        private volatile States state = States.Run;

        public void RequestShutdown() =>
            this.state = States.Shutdown;

        public void RequestAbort() =>
            this.state = States.Abort;

        /// <summary>
        /// Request for spreading an instance.
        /// </summary>
        /// <param name="value">Instance (will remove from argument)</param>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Request(ref T? value)
        {
            Debug.Assert(value != null);

            Interlocked.Increment(ref this.count);

            for (var index = 0; index < this.requests.Length; index++)
            {
                var parked = Interlocked.CompareExchange(
                    ref this.requests[index], value, null);
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

        private T? InternalTake()
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

            lock (this.floodQueue)
            {
                if (this.floodQueue.Count >= 1)
                {
                    var count = Interlocked.Decrement(ref this.count);
                    Debug.Assert(count >= 0);

                    return this.floodQueue.Dequeue();
                }
            }

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
    }
}
