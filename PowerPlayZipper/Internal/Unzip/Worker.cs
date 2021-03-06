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
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using PowerPlayZipper.Compatibility;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class Worker
    {
        [Flags]
        private enum GeneralPurposeBitFlags : short
        {
            Encrypted = 0x0001,  // bit0
            ProduceDataDescriptor = 0x0008,  // bit3   TODO:
            EntryIsUTF8 = 0x0800,  // bit11
        }

        private readonly Controller context;
        private readonly ReadOnlyRangedStream rangedStream;
        private readonly byte[] streamBuffer;
        private readonly Thread thread;

        public Worker(Stream stream, int streamBufferSize, Controller context)
        {
            this.context = context;
            this.rangedStream = new ReadOnlyRangedStream(stream);
            this.streamBuffer = new byte[streamBufferSize];
            this.thread = new Thread(this.ThreadEntry);
            this.thread.Name = $"Unzipeer.Worker[{this.thread.ManagedThreadId}]";
            this.thread.IsBackground = true;
        }

        public void StartConsume() =>
            this.thread.Start();

        private static bool IsSupported(CompressionMethods cm, GeneralPurposeBitFlags gpbf)
        {
            if ((gpbf & (GeneralPurposeBitFlags.Encrypted | GeneralPurposeBitFlags.ProduceDataDescriptor)) != 0)
            {
                return false;
            }
            return (cm == CompressionMethods.Deflate) || (cm == CompressionMethods.Stored);
        }

        private static bool IsDirectory(CompressionMethods cm, string fileName) =>
            (cm == CompressionMethods.Stored) && fileName.EndsWith("/");

        private void UnzipCore()
        {
            while (true)
            {
                // Refill pools (on this worker thread).
                this.context.BufferPool.Refill(4);
                this.context.RequestPool.Refill(4);

                // Received abort request.
                var request = this.context.RequestSpreader.Dequeue();
                if (request == null)
                {
                    return;
                }

                //var versionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(
                //    request.Buffer, request.BufferOffset + 4);
                var generalPurposeBitFlag = (GeneralPurposeBitFlags)BinaryPrimitives.ReadInt16LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 6);
                var compressionMethod = (CompressionMethods)BinaryPrimitives.ReadInt16LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 8);

                if (!IsSupported(compressionMethod, generalPurposeBitFlag))
                {
                    request.Clear();
                    this.context.RequestPool.Push(ref request);
                    continue;
                }

                var time = BinaryPrimitives.ReadUInt16LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 10);
                var date = BinaryPrimitives.ReadUInt16LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 12);
                var crc32 = BinaryPrimitives.ReadUInt32LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 14);
                var originalSize = BinaryPrimitives.ReadUInt32LittleEndian(
                    request.Buffer!, request.BufferOffsetOfEntry + 22);

                var encoding =
                    ((generalPurposeBitFlag & GeneralPurposeBitFlags.EntryIsUTF8) == GeneralPurposeBitFlags.EntryIsUTF8) ?
                        Encoding.UTF8 :
                        this.context.Encoding;

                string fileName;

                // Can copy all file name string from the buffer.
                var bufferRemains = request.BufferSize - request.FileNameOffset;
                if (request.FileNameLength <= bufferRemains)
                {
                    try
                    {
                        fileName = encoding.GetString(
                            request.Buffer!, request.FileNameOffset, request.FileNameLength);
                    }
                    // Invalid Unicode code point or else.
                    catch (Exception ex)
                    {
                        request.Clear();
                        this.context.RequestPool.Push(ref request);
                        this.context.OnError(ex);
                        continue;
                    }
                }
                // Required last file name string fragment from the zip file.
                else
                {
                    var temporaryBuffer = new byte[request.FileNameLength];
                    var firstFragmentLength = bufferRemains;
                    var lastFragmentLength = request.FileNameLength - firstFragmentLength;

                    // Copy first file name string fragment from the buffer.
                    Array.Copy(
                        request.Buffer!, request.FileNameOffset,
                        temporaryBuffer, 0, firstFragmentLength);

                    try
                    {
                        // Reset stream position.
                        this.rangedStream.ResetRange(
                            request.BufferPosition + request.FileNameOffset + firstFragmentLength);

                        // Read last file name string fragment from the zip file.
                        var readLastFragmentLength = this.rangedStream.Read(
                            temporaryBuffer,
                            firstFragmentLength,
                            lastFragmentLength);
                        if (readLastFragmentLength != lastFragmentLength)
                        {
                            // Start finishing by EOF. (Has invalid header)
                            request.Clear();
                            this.context.RequestPool.Push(ref request);
                            this.context.OnError(new FormatException("TODO:"));
                            continue;
                        }

                        fileName = encoding.GetString(temporaryBuffer);
                    }
                    // IO problem, invalid Unicode code point or else.
                    catch (Exception ex)
                    {
                        request!.Clear();
                        this.context.RequestPool.Push(ref request);
                        this.context.OnError(ex);
                        continue;
                    }
                }

                var bodyPosition = request.BufferPosition +
                    request.CommentOffset + request.CommentLength;
                var compressedSize = request.CompressedSize;

                request!.Clear();
                this.context.RequestPool.Push(ref request);

                var isDirectory = IsDirectory(compressionMethod, fileName);
                if (this.context.IgnoreEmptyDirectoryEntry && isDirectory)
                {
                    continue;
                }

                // TODO:
                var dateTime = default(DateTime);

                var entry = new ZippedFileEntry(
                    fileName,
                    isDirectory ? CompressionMethods.Directory : compressionMethod,
                    compressedSize,
                    originalSize,
                    crc32,
                    dateTime);

                if (!this.context.OnEvaluate(entry))
                {
                    continue;
                }

                ///////////////////////////////////////////////////////////////////////

                switch (entry.CompressionMethod)
                {
                    case CompressionMethods.Stored:
                        this.rangedStream.SetRange(bodyPosition, compressedSize);
                        this.context.OnProcess(
                            entry, rangedStream, this.streamBuffer);
                        break;
                    case CompressionMethods.Deflate:
                        this.rangedStream.SetRange(bodyPosition, compressedSize);
                        var compressedStream = new DeflateStream(
                            this.rangedStream, CompressionMode.Decompress, false);
                        this.context.OnProcess(
                            entry, compressedStream, this.streamBuffer);
                        break;
                    case CompressionMethods.Directory:
                        this.context.OnProcess(
                            entry, null, null);
                        break;
                }
            }
        }

        private void ThreadEntry()
        {
            try
            {
                this.UnzipCore();
            }
            catch (Exception ex)
            {
                this.context.OnError(ex);
            }
            finally
            {
                this.rangedStream.Dispose();
                this.context.OnWorkerFinished();
            }
        }
    }
}
