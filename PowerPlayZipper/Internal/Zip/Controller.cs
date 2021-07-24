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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using PowerPlayZipper.Advanced;

namespace PowerPlayZipper.Internal.Zip
{
    internal sealed class Controller
    {
        private const int NotifyCount = 100;
        private const int ParserBufferSize = 16384;

        //private readonly Worker?[] workers;
        //private Parser? parser;

        private readonly IZipperTraits traits;
        private readonly Action<ProcessedResults> succeeded;
        private readonly Action<List<Exception>> failed;
        private readonly List<Exception> caughtExceptions = new();
        private readonly Stopwatch elapsed = new();
        
        private readonly LockFreeStack<RequestInformation> requestPool = new(256, 16384);

        private volatile int runningThreads;
        private volatile int totalFiles;
        private long totalCompressedSize;
        private long totalOriginalSize;

        public readonly bool IgnoreEmptyDirectory;
        public readonly Encoding Encoding;

        public readonly LockFreeQueue<RequestInformation> RequestSpreader = new();
        public readonly LockFreeQueue<RequestInformation> RequestCombiner = new();

        public Controller(
            IZipperTraits traits,
            bool ignoreEmptyDirectory,
            int parallelCount,
            int streamBufferSize,
            Encoding encoding,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed)
        {
            this.IgnoreEmptyDirectory = ignoreEmptyDirectory;
            this.Encoding = encoding;

            this.traits = traits;
            this.succeeded = succeeded;
            this.failed = failed;
            
            //this.workers = new Worker[parallelCount];
            //for (var index = 0; index < this.workers.Length; index++)
            //{
            //    this.workers[index] = new Worker(traits.OpenForReadZipFile(streamBufferSize), streamBufferSize, this);
            //}
        }

        /// <summary>
        /// Process a file entry.
        /// </summary>
        /// <param name="entry">File entry</param>
        /// <param name="compressedStream">Read from this stream if available</param>
        /// <param name="streamBuffer">Can use this stream buffer if available</param>
        internal void OnProcess(
            ZippedFileEntry entry, Stream? compressedStream, byte[]? streamBuffer)
        {
            try
            {
                var targetPath = this.traits.GetTargetPath(entry);
                var directoryPath = this.traits.GetDirectoryName(targetPath)!;

                // Invoke event.
                this.traits.OnProcessing(entry, ProcessingStates.Begin, 0);

                // Create base directory.
                this.traits.CreateDirectoryIfNotExist(directoryPath);

                if (compressedStream != null)
                {
                    Debug.Assert(streamBuffer != null);

                    // Copy stream data to target file.
                    using (var fs = this.traits.OpenForReadFile(targetPath, streamBuffer!.Length))
                    {
                        // If opened.
                        if (fs != null)
                        {
                            var notifyCount = NotifyCount;
                            while (true)
                            {
                                var read = compressedStream.Read(
                                    streamBuffer!, 0, streamBuffer!.Length);
                                if (read == 0)
                                {
                                    break;
                                }
                                fs.Write(streamBuffer!, 0, read);

                                if (notifyCount-- <= 0)
                                {
                                    // Invoke event.
                                    this.traits.OnProcessing(
                                        entry, ProcessingStates.Processing, fs.Position);
                                    notifyCount = NotifyCount;
                                }
                            }
                            fs.Flush();

                            Interlocked.Increment(ref this.totalFiles);
                            Interlocked.Add(ref this.totalCompressedSize, entry.CompressedSize);
                            Interlocked.Add(ref this.totalOriginalSize, entry.OriginalSize);
                        }
                    }
                }

                // Invoke event.
                this.traits.OnProcessing(entry, ProcessingStates.Done, entry.OriginalSize);
            }
            catch (Exception ex)
            {
                lock (this.caughtExceptions)
                {
                    this.caughtExceptions.Add(ex);
                }
            }
        }

        /// <summary>
        /// Record an exception.
        /// </summary>
        /// <param name="ex">Exception</param>
        internal void OnError(Exception ex)
        {
            lock (this.caughtExceptions)
            {
                this.caughtExceptions.Add(ex);
            }
        }

        //internal void OnParserFinished() =>
        //    this.RequestSpreader.RequestShutdown();

        /// <summary>
        /// Mark finished a worker.
        /// </summary>
        internal void OnWorkerFinished()
        {
            var runningThreads = Interlocked.Decrement(ref this.runningThreads);
            Debug.Assert(runningThreads >= 0);

            // Last one.
            if (runningThreads <= 0)
            {
                this.elapsed.Stop();

                try
                {
                    this.traits.Finished();

                    if (this.caughtExceptions.Count >= 1)
                    {
                        this.failed(this.caughtExceptions);
                    }
                    else
                    {
                        //var internalStats =
                        //    $"BufferPool=[{this.BufferPool}], RequestPool=[{this.RequestPool}], Spreader=[{this.RequestSpreader}], ParserElapsed={this.parser!.Elapsed}";
                        this.succeeded(
                            new ProcessedResults(
                                this.totalFiles,
                                this.totalCompressedSize,
                                this.totalOriginalSize,
                                this.elapsed.Elapsed,
                                0,
                                //this.workers.Length,
                                ""));
                                //internalStats));
                    }
                }
                finally
                {
                    // Make GC safer.
                    //for (var index = 0; index < this.workers.Length; index++)
                    //{
                    //    this.workers[index] = null!;
                    //}
                    //this.parser = null;
                }
            }
        }

        /// <summary>
        /// Start zipping operation.
        /// </summary>
        public void Run()
        {
            Debug.Assert(this.runningThreads == 0);
            //Debug.Assert(this.parser != null);

            this.traits.Started();

            this.elapsed.Start();

            ////////////////////////

            foreach (var entry in this.traits.EnumerateTargetPaths())
            {
                var required = false;
                try
                {
                    required = this.traits.IsRequiredProcessing(entry);
                }
                catch (Exception ex)
                {
                    lock (this.caughtExceptions)
                    {
                        this.caughtExceptions.Add(ex);
                    }
                }

                if (!required)
                {
                    continue;
                }

                var request = this.requestPool.Pop();
                Debug.Assert(string.IsNullOrEmpty(request.TargetFilePath));
                request.TargetFilePath = entry.Path;

                this.RequestSpreader.Enqueue(ref request);
                Debug.Assert(request == null);
            }
            
            //this.parser!.Start();
        }

        /// <summary>
        /// Request abort for zipping operation.
        /// </summary>
        /// <remarks>Will invoke "finished" delegate when all workers are finished.</remarks>
        public void RequestAbort()
        {
            //this.parser?.RequestAbort();
            //this.RequestSpreader.RequestAbort();
        }
    }
}
