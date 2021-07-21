using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using PowerPlayZipper.Advanced;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class Controller
    {
        private const int NotifyCount = 100;
        private const int ParserBufferSize = 16384;

        private Parser? parser;
        private readonly Worker?[] workers;
        private readonly IUnzipperTraits traits;
        private readonly Action<ProcessedResults> succeeded;
        private readonly Action<List<Exception>> failed;
        private readonly List<Exception> caughtExceptions = new();
        private readonly Stopwatch elapsed = new();

        private volatile int runningThreads;
        private volatile int totalFiles;
        private long totalCompressedSize;
        private long totalOriginalSize;

        public readonly bool IgnoreEmptyDirectoryEntry;
        public readonly Encoding Encoding;

        public readonly ArrayPool<byte> BufferPool = new(ParserBufferSize, 16, 64);
        public readonly LockFreeStack<RequestInformation> RequestPool = new(256, 16384);
        public readonly LockFreeQueue<RequestInformation> RequestSpreader = new();

        public Controller(
            IUnzipperTraits traits,
            bool ignoreEmptyDirectoryEntry,
            int parallelCount,
            int streamBufferSize,
            Encoding encoding,
            Action<ProcessedResults> succeeded,
            Action<List<Exception>> failed)
        {
            this.IgnoreEmptyDirectoryEntry = ignoreEmptyDirectoryEntry;
            this.Encoding = encoding;

            this.traits = traits;
            this.succeeded = succeeded;
            this.failed = failed;

            this.workers = new Worker[parallelCount];
            for (var index = 0; index < this.workers.Length; index++)
            {
                this.workers[index] = new Worker(traits.OpenForReadZipFile(streamBufferSize), streamBufferSize, this);
            }

            this.parser = new Parser(traits.OpenForReadZipFile(ParserBufferSize), this);
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
                return this.traits.IsRequiredProcessing(entry);
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
                    using (var fs = this.traits.OpenForWriteFile(targetPath, streamBuffer!.Length))
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

        internal void OnParserFinished() =>
            this.RequestSpreader.RequestShutdown();

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
                        var internalStats =
                            $"BufferPool=[{this.BufferPool}], RequestPool=[{this.RequestPool}], Spreader=[{this.RequestSpreader}], ParserElapsed={this.parser!.Elapsed}";
                        this.succeeded(
                            new ProcessedResults(
                                this.totalFiles,
                                this.totalCompressedSize,
                                this.totalOriginalSize,
                                this.elapsed.Elapsed,
                                this.workers.Length,
                                internalStats));
                    }
                }
                finally
                {
                    // Make GC safer.
                    for (var index = 0; index < this.workers.Length; index++)
                    {
                        this.workers[index] = null!;
                    }
                    this.parser = null;
                }
            }
        }

        /// <summary>
        /// Start unzipping operation.
        /// </summary>
        public void Start()
        {
            Debug.Assert(this.runningThreads == 0);
            Debug.Assert(this.parser != null);

            this.traits.Started();

            this.elapsed.Start();

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
