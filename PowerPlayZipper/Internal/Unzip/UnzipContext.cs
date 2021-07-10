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
        private readonly UnzipWorker?[] workers;
        private readonly Func<ZippedFileEntry, bool> predicate;
        private readonly Action<ZippedFileEntry, Stream?, byte[]?> action;
        private readonly List<Exception> caughtExceptions = new List<Exception>();
        private readonly Action<List<Exception>> finished;

        private volatile int runningThreads;

        public readonly bool IgnoreDirectoryEntry;
        public readonly Encoding Encoding;
        public readonly int StreamBufferSize;

        public volatile UnzipCommonRoleContext? CommonRoleContext;

        public UnzipContext(
            string zipFilePath,
            bool ignoreDirectoryEntry,
            int parallelCount,
            Encoding encoding,
            int streamBufferSize,
            Func<ZippedFileEntry, bool> predicate,
            Action<ZippedFileEntry, Stream?, byte[]?> action,
            Action<List<Exception>> finished)
        {
            this.IgnoreDirectoryEntry = ignoreDirectoryEntry;
            this.Encoding = encoding;
            this.StreamBufferSize = streamBufferSize;
            this.predicate = predicate;
            this.action = action;
            this.finished = finished;
            this.CommonRoleContext = new UnzipCommonRoleContext(zipFilePath);

            this.workers = new UnzipWorker[parallelCount];
            for (var index = 0; index < this.workers.Length; index++)
            {
                this.workers[index] = new UnzipWorker(zipFilePath, this);
            }
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
                this.finished(this.caughtExceptions);

                // Make GC safer.
                for (var index = 0; index < this.workers.Length; index++)
                {
                    this.workers[index] = null!;
                }

                // Close entry stream.
                Debug.Assert(this.CommonRoleContext != null);
                this.CommonRoleContext?.EntryStream.Dispose();
            }
        }

        public void Start()
        {
            Debug.Assert(this.runningThreads == 0);

            this.runningThreads = this.workers.Length;
            for (var index = 0; index < this.workers.Length; index++)
            {
                Debug.Assert(this.workers[index] != null);
                this.workers[index]!.StartConsume();
            }
        }

        public void RequestAbort()
        {
            while (this.runningThreads >= 1)
            {
                // Take role context. (Spin loop)
                var commonRoleContext = Interlocked.Exchange(ref this.CommonRoleContext, null);
                if (commonRoleContext == null)
                {
                    continue;
                }

                // Request abort.
                commonRoleContext.HeaderPosition = -1;
                this.CommonRoleContext = commonRoleContext;
                break;
            }
        }
    }
}
