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
using System.Diagnostics;
using System.IO;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class Parser
    {
        private const int PK0304HeaderSize = 30;

        private readonly Controller context;
        private readonly Stream stream;
        private readonly Thread thread;
        private long bufferPosition;
        private int lastReadSize;
        private volatile bool isAborting;

        public Parser(Stream stream, Controller context)
        {
            this.context = context;
            this.stream = stream;
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

                    var request = this.context.RequestPool.Pop();
                    Debug.Assert(request != null);

                    request!.Buffer = buffer;
                    request.BufferSize = this.lastReadSize;
                    request.BufferPosition = this.bufferPosition;
                    request.BufferOffsetOfEntry = bufferOffset;

                    var fileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(
                        buffer, bufferOffset + 26);
                    if (fileNameLength == 0)
                    {
                        this.context.RequestPool.Push(ref request);
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
                    this.context.RequestSpreader.Enqueue(ref request);
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
