using System.Diagnostics;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    /// <summary>
    /// Fast object instance lock-free queue.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal sealed class LockFreeQueue<T>
        where T : class, new()
    {
        private sealed class Container
        {
            public volatile T? Element;
            public volatile Container? NextContainer;

            public Container()
            {
            }

            public Container(T element) =>
                this.Element = element;

            public string PrettyPrint =>
                $"{this.GetHashCode()} --> {this.NextContainer?.GetHashCode().ToString() ?? "(EOC)"}";
            public string PrettyPrintRecursive =>
                $"{this.GetHashCode()} --> {this.NextContainer?.PrettyPrintRecursive ?? "(EOC)"}";

            public override string ToString() =>
                this.PrettyPrint;
        }

        private enum States
        {
            Run,
            Shutdown,
            Abort,
        }

        private volatile States state = States.Run;

        private volatile Container head;
        private volatile Container tail;

        private volatile int totalRequests;
        private volatile int queueCount;

        public LockFreeQueue()
        {
            var node = new Container();
            this.head = node;
            this.tail = node;
        }

        public int Requests =>
            this.totalRequests;
        public long Current =>
            this.queueCount;

        public void RequestShutdown() =>
            this.state = States.Shutdown;

        public void RequestAbort() =>
            this.state = States.Abort;

        /// <summary>
        /// Request for spreading an instance.
        /// </summary>
        /// <param name="request">Instance (will remove from argument)</param>
        public void Enqueue(ref T? request)
        {
            Debug.Assert(request != null);

            Interlocked.Increment(ref this.totalRequests);
            Interlocked.Increment(ref this.queueCount);

            var container = new Container(request!);
            var baseTail = this.tail;
            var currentTail = baseTail;

            while (true)
            {
                var currentTailNext = currentTail.NextContainer;
                if (currentTailNext == null)
                {
                    if (Interlocked.CompareExchange(ref currentTail.NextContainer, container, null) == null)
                    {
                        if (!object.ReferenceEquals(currentTail, baseTail))
                        {
                            Interlocked.CompareExchange(ref this.tail, container, baseTail);
                        }
                        request = null;
                        return;
                    }
                }
                else if (currentTail == currentTailNext)
                {
                    var lastBaseTail = baseTail;
                    baseTail = this.tail;
                    if (baseTail != lastBaseTail)
                    {
                        currentTail = baseTail;
                    }
                    else
                    {
                        currentTail = this.head;
                    }
                }
                else if (currentTail != baseTail)
                {
                    var lastBaseTail = baseTail;
                    baseTail = this.tail;
                    if (baseTail != lastBaseTail)
                    {
                        currentTail = baseTail;
                    }
                    else
                    {
                        currentTail = currentTailNext;
                    }
                }
                else
                {
                    currentTail = currentTailNext;
                }
            }
        }

        /// <summary>
        /// Take a requested instance.
        /// </summary>
        /// <returns>Instance if succeeded</returns>
        public T? Dequeue()
        {
            while (true)
            {
                var baseHead = this.head;
                var currentHead = baseHead;

                while (true)
                {
                    var element = currentHead.Element;
                    if (element != null)
                    {
                        // Could get a element?
                        var result1 = Interlocked.CompareExchange(ref currentHead.Element, null, element);
                        if (object.ReferenceEquals(result1, element))
                        {
                            // If already traversed one or few steps on the link chain.
                            if (!object.ReferenceEquals(currentHead, baseHead))
                            {
                                var currentHeadNext = currentHead.NextContainer;
                                var nextHead = currentHeadNext ?? currentHead;

                                if (!object.ReferenceEquals(nextHead, baseHead))
                                {
                                    var result2 = Interlocked.CompareExchange(ref this.head, nextHead, baseHead);
                                    if (object.ReferenceEquals(result2, baseHead))
                                    {
                                        // Make be garbage.
                                        baseHead.NextContainer = baseHead;
                                    }
                                }
                            }

                            Interlocked.Decrement(ref this.queueCount);
                            return element;
                        }
                    }
                    else
                    {
                        var currentHeadNext = currentHead.NextContainer;
                        if (currentHeadNext == null)
                        {
                            if (!object.ReferenceEquals(currentHead, baseHead))
                            {
                                var result = Interlocked.CompareExchange(ref this.head, currentHead, baseHead);
                                if (object.ReferenceEquals(result, baseHead))
                                {
                                    // Make be garbage.
                                    baseHead.NextContainer = baseHead;
                                }
                            }

                            if (this.state == States.Run)
                            {
                                // Retry at first step.
                                break;
                            }
                            else if ((this.state == States.Shutdown) && (this.queueCount >= 1))
                            {
                                // Retry at first step.
                                break;
                            }
                            else
                            {
                                // Retired (by current state).
                                return null;
                            }
                        }

                        // Will exhaust already garbage container.
                        if (object.ReferenceEquals(currentHead, currentHeadNext))
                        {
                            // Retry at first step.
                            break;
                        }

                        // Progress next container on the linked chain.
                        currentHead = currentHeadNext;
                    }
                }
            }
        }

        public override string ToString() =>
            $"Current={this.Current}, Requests={this.Requests}";
    }
}
