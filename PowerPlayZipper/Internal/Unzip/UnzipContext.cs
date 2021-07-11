using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class UnzipContext
    {
        private const int BufferSize = 4096;

        private Parser? parser;
        private readonly UnzipWorker?[] inflators;
        private readonly Func<ZippedFileEntry, bool> predicate;
        private readonly Action<ZippedFileEntry, Stream?, byte[]?> action;
        private readonly List<Exception> caughtExceptions = new();
        private readonly Action<List<Exception>, int> finished;

        private volatile int runningThreads;

        public readonly bool IgnoreDirectoryEntry;
        public readonly Encoding Encoding;
        public readonly int StreamBufferSize;

        public readonly ArrayPool<byte> BufferPool = new(BufferSize);
        public readonly Pool<RequestInformation> RequestPool = new();
        public readonly Spreader<RequestInformation> RequestSpreader = new();

        public UnzipContext(
            string zipFilePath,
            bool ignoreDirectoryEntry,
            int parallelCount,
            Encoding encoding,
            int streamBufferSize,
            Func<ZippedFileEntry, bool> predicate,
            Action<ZippedFileEntry, Stream?, byte[]?> action,
            Action<List<Exception>, int> finished)
        {
            this.IgnoreDirectoryEntry = ignoreDirectoryEntry;
            this.Encoding = encoding;
            this.StreamBufferSize = streamBufferSize;

            this.predicate = predicate;
            this.action = action;
            this.finished = finished;

            this.inflators = new UnzipWorker[parallelCount];
            for (var index = 0; index < this.inflators.Length; index++)
            {
                this.inflators[index] = new UnzipWorker(zipFilePath, this);
            }

            this.parser = new Parser(this, zipFilePath);
        }

        public bool Evaluate(ZippedFileEntry entry)
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

        public void OnAction(
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

        public void OnError(Exception ex)
        {
            lock (this.caughtExceptions)
            {
                this.caughtExceptions.Add(ex);
            }
        }

        public void OnFinished()
        {
            var runningThreads = Interlocked.Decrement(ref this.runningThreads);
            Debug.Assert(runningThreads >= 0);

            // Last one.
            if (runningThreads <= 0)
            {
                this.finished(this.caughtExceptions, this.inflators.Length);

                // Make GC safer.
                for (var index = 0; index < this.inflators.Length; index++)
                {
                    this.inflators[index] = null!;
                }
                this.parser = null;
            }
        }

        public void Start()
        {
            Debug.Assert(this.runningThreads == 0);
            Debug.Assert(this.parser != null);

            this.runningThreads = this.inflators.Length;
            for (var index = 0; index < this.inflators.Length; index++)
            {
                Debug.Assert(this.inflators[index] != null);
                this.inflators[index]!.StartConsume();
            }

            this.parser!.Start();
        }

        public void RequestAbort()
        {
            this.parser?.RequestAbort();
            this.RequestSpreader.RequestAbort();
        }
    }
}
