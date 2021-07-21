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
using System.IO;

namespace PowerPlayZipper.Internal.Unzip
{
    /// <summary>
    /// Read only range constrained stream.
    /// </summary>
    internal sealed class ReadOnlyRangedStream : Stream
    {
        private readonly Stream stream;
        private long initialPosition = 0;
        private long constrainedSize;

        internal ReadOnlyRangedStream(Stream stream)
        {
            this.stream = stream;
            this.constrainedSize = this.stream.Length;
        }

        /// <summary>
        /// Set range and begin constraint.
        /// </summary>
        /// <param name="initialPosition">Initial position on the file</param>
        /// <param name="constrainedSize">Constrained size</param>
        public void SetRange(long initialPosition, long constrainedSize)
        {
            this.stream.Position = initialPosition;
            if ((initialPosition + constrainedSize) >= this.stream.Length)
            {
                throw new IOException("Reached end of file.");
            }
            this.initialPosition = initialPosition;
            this.constrainedSize = constrainedSize;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Dispose();
            }
        }

        /// <summary>
        /// Reset and make free access.
        /// </summary>
        /// <param name="initialPosition">Initial position on the file</param>
        public void ResetRange(long initialPosition)
        {
            this.stream.Position = initialPosition;
            this.constrainedSize = this.stream.Length;
        }

        public override bool CanSeek =>
            false;

        public override long Length =>
            this.constrainedSize;

        public override long Position
        {
            get => this.stream.Position - this.initialPosition;
            set => throw new NotImplementedException();
        }

        public override bool CanRead =>
            true;

        public override bool CanWrite =>
            false;

        public override int Read(byte[] array, int offset, int count)
        {
            var size = ((this.Position + count) >= this.constrainedSize) ?
                (this.constrainedSize - this.Position) :
                count;
            return this.stream.Read(array, offset, (int)size);
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotImplementedException();

        public override void Flush() =>
            throw new NotImplementedException();

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();
    }
}
