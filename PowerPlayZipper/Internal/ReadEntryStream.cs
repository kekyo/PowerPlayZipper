using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace PowerPlayZipper.Internal
{
    internal sealed class ReadEntryStream : FileStream
    {
        private readonly byte[] buffer = new byte[4];

        public ReadEntryStream(string path) : base(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)
        {
        }

        public async ValueTask<short> ReadInt16Async()
        {
            var read = await base.ReadAsync(buffer, 0, 2).ConfigureAwait(false);
            if (read != 2)
            {
                throw new IOException();
            }

            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public async ValueTask<int> ReadInt32Async()
        {
            var read = await base.ReadAsync(buffer, 0, 4).ConfigureAwait(false);
            if (read != 4)
            {
                throw new IOException();
            }

            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public async ValueTask<(bool, int)> TryReadInt32Async()
        {
            var read = await base.ReadAsync(buffer, 0, 4).ConfigureAwait(false);
            if (read == 0)
            {
                return (false, 0);
            }
            if (read != 4)
            {
                throw new IOException();
            }
            return (true, BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }
    }
}
