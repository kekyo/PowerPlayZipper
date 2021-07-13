using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class Context
    {
        private const int BufferSize = 16384;

        private Parser? parser;
        private readonly Worker?[] workers;
        private readonly Func<ZippedFileEntry, bool> predicate;
        private readonly Action<ZippedFileEntry, Stream?, byte[]?> action;
        private readonly List<Exception> caughtExceptions = new();
        private readonly Action<List<Exception>, int, string> finished;

        private volatile int runningThreads;

        public readonly bool IgnoreDirectoryEntry;
        public readonly Encoding Encoding;
        public readonly int StreamBufferSize;

        public readonly ArrayPool<byte> BufferPool = new(BufferSize, 16, 64);
        public readonly Pool<RequestInformation> RequestPool = new(256, 16384);
        public readonly Spreader<RequestInformation> RequestSpreader = new();

        public Context(
            string zipFilePath,
            bool ignoreDirectoryEntry,
            int parallelCount,
            Encoding encoding,
            int streamBufferSize,
            Func<ZippedFileEntry, bool> predicate,
            Action<ZippedFileEntry, Stream?, byte[]?> action,
            Action<List<Exception>, int, string> finished)
        {
            this.IgnoreDirectoryEntry = ignoreDirectoryEntry;
            this.Encoding = encoding;
            this.StreamBufferSize = streamBufferSize;

            this.predicate = predicate;
            this.action = action;
            this.finished = finished;

            this.workers = new Worker[parallelCount];
            for (var index = 0; index < this.workers.Length; index++)
            {
                this.workers[index] = new Worker(zipFilePath, this);
            }

            this.parser = new Parser(this, zipFilePath);
        }

        /// <summary>
        /// Evaluate file entry.
        /// </summary>
        /// <param name="entry">File entry</param>
        /// <returns>True if required processing</returns>
        internal bool OnEvaluate(ZippedFileEntry entry)
        {
            try
            {
                return this.predicate(entry);
            }
            catch (Exception ex)
            {
                lock (this.caughtExceptions)
                {
                    this.caughtExceptions.Add(ex);
                }
            }

            return false;
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
                this.action(entry, compressedStream, streamBuffer);
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

        /// <summary>
        /// Mark finished a worker.
        /// </summary>
        internal void OnFinished()
        {
            var runningThreads = Interlocked.Decrement(ref this.runningThreads);
            Debug.Assert(runningThreads >= 0);

            // Last one.
            if (runningThreads <= 0)
            {
                this.finished(
                    this.caughtExceptions,
                    this.workers.Length,
                    $"BufferPool=[{this.BufferPool}], RequestPool=[{this.RequestPool}], Spreader=[{this.RequestSpreader}]");

                // Make GC safer.
                for (var index = 0; index < this.workers.Length; index++)
                {
                    this.workers[index] = null!;
                }
                this.parser = null;
            }
        }

        internal void OnParserFinished() =>
            this.RequestSpreader.RequestShutdown();

        /// <summary>
        /// Start unzipping operation.
        /// </summary>
        public void Start()
        {
            Debug.Assert(this.runningThreads == 0);
            Debug.Assert(this.parser != null);

            this.runningThreads = this.workers.Length;
            for (var index = 0; index < this.workers.Length; index++)
            {
                Debug.Assert(this.workers[index] != null);
                this.workers[index]!.StartConsume();
            }

            this.parser!.Start();
        }

        /// <summary>
        /// Request abort for unzipping operation.
        /// </summary>
        /// <remarks>Will invoke "finished" delegate when all workers are finished.</remarks>
        public void RequestAbort()
        {
            this.parser?.RequestAbort();
            this.RequestSpreader.RequestAbort();
        }
    }
}
