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

using System.Runtime.CompilerServices;

namespace PowerPlayZipper.Internal.Unzip
{
    /// <summary>
    /// Request packet from parser to worker.
    /// </summary>
    internal sealed class RequestInformation : StackableElement
    {
        /// <summary>
        /// Stored first time data.
        /// </summary>
        public byte[]? Buffer;

        /// <summary>
        /// Read and stored into buffer from zip file position.
        /// </summary>
        public long BufferPosition;

        /// <summary>
        /// Read size (<= Buffer.Length)
        /// </summary>
        public int BufferSize;

        /// <summary>
        /// Base buffer offset of current file entry.
        /// </summary>
        public int BufferOffsetOfEntry;

        /// <summary>
        /// File name offset in buffer.
        /// </summary>
        public int FileNameOffset;

        /// <summary>
        /// File name length (in byte).
        /// </summary>
        public ushort FileNameLength;

        /// <summary>
        /// Comment offset in buffer.
        /// </summary>
        public int CommentOffset;

        /// <summary>
        /// Comment length (in byte).
        /// </summary>
        public ushort CommentLength;

        /// <summary>
        /// Compressed data size.
        /// </summary>
        public uint CompressedSize;

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Clear()
        {
            // Will be collected by GC.
            this.Buffer = null;
#if DEBUG
            this.BufferPosition = 0;
            this.BufferSize = 0;
            this.BufferOffsetOfEntry = 0;
            this.FileNameOffset = 0;
            this.FileNameLength = 0;
            this.CommentOffset = 0;
            this.CommentLength = 0;
            this.CompressedSize = 0;
#endif
        }
    }
}
