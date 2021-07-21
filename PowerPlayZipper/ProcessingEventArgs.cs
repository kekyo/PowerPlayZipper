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

namespace PowerPlayZipper
{
    public enum ProcessingStates
    {
        Begin,
        Processing,
        Done
    }
    
    public sealed class ProcessingEventArgs : EventArgs
    {
        public readonly ZippedFileEntry Entry;
        public readonly ProcessingStates State;
        public readonly long PositionOnOriginal;

        public ProcessingEventArgs(
            ZippedFileEntry entry, ProcessingStates state, long positionOnOriginal)
        {
            this.Entry = entry;
            this.State = state;
            this.PositionOnOriginal = positionOnOriginal;
        }

        public override string ToString() =>
            $"{this.State}: {this.Entry.FileName}, Position={this.PositionOnOriginal}, Percent={(double)this.PositionOnOriginal/this.Entry.OriginalSize*100.0:F3}%";
    }
}
