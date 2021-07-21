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

using PowerPlayZipper.Compatibility;
using System;
using System.Collections.Generic;
using System.Threading;

#if NET20 || NET35
using ManualResetEventSlim = System.Threading.ManualResetEvent;
#endif

namespace PowerPlayZipper.Utilities
{
    /// <summary>
    /// Fast and lightweight directory creator.
    /// </summary>
    public sealed class DirectoryConstructor
    {
        private readonly Action<string> createIfNotExist;
        private readonly Dictionary<string, ManualResetEventSlim?> processings = new();

        public DirectoryConstructor(Action<string> createIfNotExist) =>
            this.createIfNotExist = createIfNotExist;

        public void Clear() =>
            this.processings.Clear();

        /// <summary>
        /// Create directory if not exist.
        /// </summary>
        /// <param name="directoryPath">Target directory path</param>
        public void CreateIfNotExist(string directoryPath)
        {
            var firstTime = false;
            ManualResetEventSlim? locker;
            lock (this.processings)
            {
                if (!this.processings.TryGetValue(directoryPath, out locker))
                {
                    firstTime = true;
                    locker = IndependentFactory.CreateManualResetEvent();
                    this.processings.Add(directoryPath, locker);
                }
            }

            if (firstTime)
            {
                try
                {
                    this.createIfNotExist(directoryPath);
                }
                finally
                {
                    lock (this.processings)
                    {
                        this.processings[directoryPath] = null;
                    }
                    locker!.Set();
                }
            }
            else
            {
                if (locker != null)
                {
                    // Will block short time when ran the first time task.
                    locker.Wait();
                }
            }
        }
    }
}
