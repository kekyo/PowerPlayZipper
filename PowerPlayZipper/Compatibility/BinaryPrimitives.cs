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
