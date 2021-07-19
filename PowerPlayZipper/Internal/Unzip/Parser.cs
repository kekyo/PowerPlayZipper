using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class Parser
    {
        private const int PK0304HeaderSize = 30;

        private readonly Context context;
        private readonly FileStream stream;
        private readonly Thread thread;
        private long bufferPosition;
        private int lastReadSize;
        private volatile bool isAborting;

        public Parser(Context context, string zipFilePath)
        {
            this.context = context;
            this.stream = new FileStream(
                zipFilePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                this.context.BufferPool.ElementSize);
            this.thread = new Thread(this.ThreadEntry);
            this.thread.Name = $"Unzipeer.Parser[{this.thread.ManagedThreadId}]";
            this.thread.IsBackground = true;
        }

        public TimeSpan Elapsed { get; private set; }

        public void Start() =>
            this.thread.Start();

        public void RequestAbort() =>
            this.isAborting = true;

        private void Parse()
        {
            while (!this.isAborting)
            {
                var rend = this.context.BufferPool.Rent();
                Debug.Assert(rend != null);
                var buffer = rend!.Array;

                this.stream.Position = this.bufferPosition;
                this.lastReadSize = this.stream.Read(
                    buffer, 0, buffer.Length);

                if (this.lastReadSize < PK0304HeaderSize)
                {
                    // Reached EOF. (Has unknown data)
                    this.context.BufferPool.Return(ref rend);
                    Debug.Assert(rend == null);
                    return;
                }

                var bufferOffset = 0;

                while (!this.isAborting)
                {
                    var signature = BinaryPrimitives.ReadUInt32LittleEndian(
                        buffer, bufferOffset);
                    if (signature != 0x04034b50) // PK0304
                    {
                        // Reached EOF. (Has unknown data)
                        this.context.BufferPool.Return(ref rend);
                        Debug.Assert(rend == null);
                        return;
                    }

                    var request = this.context.RequestPool.Rent();
                    Debug.Assert(request != null);

                    request!.Buffer = buffer;
                    request.BufferSize = this.lastReadSize;
                    request.BufferPosition = this.bufferPosition;
                    request.BufferOffsetOfEntry = bufferOffset;

                    var fileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(
                        buffer, bufferOffset + 26);
                    if (fileNameLength == 0)
                    {
                        this.context.RequestPool.Return(ref request);
                        Debug.Assert(request == null);
                        this.context.BufferPool.Return(ref rend);
                        Debug.Assert(rend == null);

                        // Raise fatal header error.
                        this.context.OnError(new FormatException("TODO:"));
                        return;
                    }

                    var fileNameOffset = bufferOffset + 30;
                    var commentOffset = fileNameOffset + fileNameLength;

                    var compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(
                        buffer, bufferOffset + 18);
                    var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(
                        buffer, bufferOffset + 28);

                    request.FileNameOffset = fileNameOffset;
                    request.FileNameLength = fileNameLength;
                    request.CommentOffset = commentOffset;
                    request.CommentLength = commentLength;
                    request.CompressedSize = compressedSize;

                    // Enqueue
                    this.context.RequestSpreader.Spread(ref request);
                    Debug.Assert(request == null);

                    // Reached buffer tail?
                    var nextOffset = commentOffset + commentLength + compressedSize;
                    if ((nextOffset + PK0304HeaderSize) >= this.lastReadSize)
                    {
                        this.bufferPosition += nextOffset;
                        break;
                    }

                    bufferOffset = (int)nextOffset;
                }
            }
        }

        private void ThreadEntry()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            try
            {
                this.Parse();
            }
            catch (Exception ex)
            {
                this.context.OnError(ex);
            }
            finally
            {
                this.Elapsed = sw.Elapsed;
                
                this.stream.Dispose();
                this.context.OnParserFinished();
            }
        }
    }
}
