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
    internal sealed class ArrayHolder<TElement>
    {
        internal volatile ArrayHolder<TElement>? Next;
        public readonly TElement[] Array;

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal ArrayHolder(int elementSize) =>
            this.Array = new TElement[elementSize];
    }
    
    /// <summary>
    /// Fast array instance lock-free pooler.
    /// </summary>
    /// <typeparam name="TElement">Array element type</typeparam>
    internal sealed class ArrayPool<TElement>
    {
        private readonly int maxPoolCount;
        private volatile ArrayHolder<TElement>? head;
        private volatile int poolCount;
        
        private volatile int floods;
        private volatile int refills;
        private volatile int missed;
        private volatile int put;
        private volatile int got;

        public readonly int ElementSize;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="elementSize">Array element size</param>
        /// <param name="preload"></param>
        /// <param name="maxPoolCount"></param>
        public ArrayPool(int elementSize, int preload, int maxPoolCount)
        {
            Debug.Assert(elementSize >= 1);
            Debug.Assert(preload >= 1);
            Debug.Assert(maxPoolCount >= 1);

            this.ElementSize = elementSize;
            
            this.maxPoolCount = maxPoolCount;
            this.poolCount = preload;
            
            var value = new ArrayHolder<TElement>(elementSize);
            this.head = value;

            if (preload >= 2)
            {
                for (var index = 1; index < preload; index++)
                {
                    var next = new ArrayHolder<TElement>(elementSize);
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
        /// Rent an array.
        /// </summary>
        /// <returns>Array instance</returns>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public ArrayHolder<TElement> Rent()
        {
            while (true)
            {
                var currentHead = this.head;
                
                // Empty: Generate by optimistics.
                if (currentHead == null)
                {
                    Interlocked.Increment(ref this.missed);
                    return new ArrayHolder<TElement>(this.ElementSize);
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
        /// Return an array instance.
        /// </summary>
        /// <param name="array">Array instance (will remove from argument)</param>
#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Return(ref ArrayHolder<TElement>? array)
        {
            Debug.Assert(array != null);
            
            while (this.poolCount < this.maxPoolCount)
            {
                // Set next reference.
                var currentHead = this.head;
                array!.Next = currentHead;
                
                // CAS lock-free chaining.
                var result = Interlocked.CompareExchange(ref this.head, array, currentHead);
                if (object.ReferenceEquals(result, currentHead))
                {
                    Interlocked.Increment(ref this.poolCount);
                    Interlocked.Increment(ref this.put);
                    array = null;
                    return;
                }
            }

            Interlocked.Increment(ref this.floods);
            array!.Next = null;
            array = null;
        }


        /// <summary>
        /// Refill an array if required.
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

                var array = new ArrayHolder<TElement>(this.ElementSize);

                while (true)
                {
                    // Set next reference.
                    var currentHead = this.head;
                    array!.Next = currentHead;

                    // CAS lock-free chaining.
                    var result = Interlocked.CompareExchange(ref this.head, array, currentHead);
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
