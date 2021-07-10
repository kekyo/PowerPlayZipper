using System.IO;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class UnzipCommonRoleContext
    {
        // PAGE_SIZE
        public const int EntryBufferSize = 4096;

        public long HeaderPosition;
        public readonly FileStream EntryStream;

        public UnzipCommonRoleContext(string zipFilePath) =>
            this.EntryStream = new FileStream(
                zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, EntryBufferSize);
    }
}
