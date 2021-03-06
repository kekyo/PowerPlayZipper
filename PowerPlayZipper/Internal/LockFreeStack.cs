///////////////////////////////////////////////////////////////////////////
//
// PowerPlayZipper - An implementation of Lightning-Fast Zip file
// compression/decompression library on .NET.
// Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
///////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal abstract class StackableElement
    {
        internal volatile StackableElement? Next;

        public string PrettyPrint =>
            $"{this.GetHashCode()} --> {this.Next?.GetHashCode().ToString() ?? "(EOE)"}";
        public string PrettyPrintRecursive =>
            $"{this.GetHashCode()} --> {this.Next?.PrettyPrintRecursive ?? "(EOE)"}";

        public override string ToString() =>
            this.PrettyPrint;
    }

    /// <summary>
    /// Fast object instance lock-free stack.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal sealed class LockFreeStack<T>
        where T : StackableElement, new()
    {
        private readonly int maxPoolCount;
        private volatile StackableElement? head;
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
        public LockFreeStack(int preload, int maxPoolCount)
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
        public T Pop()
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
                var currentHeadNext = currentHead.Next;
                var result = Interlocked.CompareExchange(
                    ref this.head, currentHeadNext, currentHead);
                if (object.ReferenceEquals(result, currentHead))
                {
                    var pc = Interlocked.Decrement(ref this.poolCount);
                    Debug.Assert(pc >= 0);
                    Interlocked.Increment(ref this.got);
                    result.Next = null;
                    return (T)result;
                }
            }
        }

        /// <summary>
        /// Return an instance.
        /// </summary>
        /// <param name="value">Instance (will remove from argument)</param>
        public void Push(ref T? value)
        {
            Debug.Assert(value != null);

            // Optimistic: Non atomic overflow check.
            if (this.poolCount < this.maxPoolCount)
            {
                Interlocked.Increment(ref this.poolCount);

                do
                {
                    // Set next reference.
                    var currentHead = this.head;
                    value!.Next = currentHead;

                    // CAS lock-free chaining.
                    var result = Interlocked.CompareExchange(
                        ref this.head, value, currentHead);
                    if (object.ReferenceEquals(result, currentHead))
                    {
                        Interlocked.Increment(ref this.put);
                        value = null;
                        return;
                    }
                }
                // Optimistic: Non atomic overflow check.
                while (this.poolCount < this.maxPoolCount);

                Interlocked.Decrement(ref this.poolCount);
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

                Interlocked.Increment(ref this.poolCount);

                var value = new T();

                while (true)
                {
                    // Set next reference.
                    var currentHead = this.head;
                    value!.Next = currentHead;

                    // CAS lock-free chaining.
                    var result = Interlocked.CompareExchange(
                        ref this.head, value, currentHead);
                    if (object.ReferenceEquals(result, currentHead))
                    {
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
