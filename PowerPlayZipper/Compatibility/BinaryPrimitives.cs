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
using System.Runtime.CompilerServices;

namespace PowerPlayZipper.Compatibility
{
    internal static class BinaryPrimitives
    {
        static BinaryPrimitives()
        {
            var bytes = BitConverter.GetBytes(0x1234);
            isLittleEndian = bytes[0] == 0x34;
        }

        private static readonly bool isLittleEndian;

        // It's rare case, today we have many LE (configured) CPU.
        private static byte[] MakeReverse(byte[] data)
        {
            var reversed = new byte[data.Length];
            for (var index = 0; index < data.Length; index++)
            {
                reversed[reversed.Length - index - 1] = data[index];
            }
            return reversed;
        }

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static short ReadInt16LittleEndian(byte[] data, int offset) =>
            isLittleEndian ?
                BitConverter.ToInt16(data, offset) :
                BitConverter.ToInt16(MakeReverse(data), offset);

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static ushort ReadUInt16LittleEndian(byte[] data, int offset) =>
            isLittleEndian ?
                BitConverter.ToUInt16(data, offset) :
                BitConverter.ToUInt16(MakeReverse(data), offset);

#if !NET20 && !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static uint ReadUInt32LittleEndian(byte[] data, int offset) =>
            isLittleEndian ?
                BitConverter.ToUInt32(data, offset) :
                BitConverter.ToUInt32(MakeReverse(data), offset);
    }
}
