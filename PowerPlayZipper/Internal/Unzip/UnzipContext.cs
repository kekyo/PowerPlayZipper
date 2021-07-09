using System;
using System.IO;
using System.Text;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class UnzipContext
    {
        private readonly UnzipThreadWorker[] threadWorkers;
        private readonly Func<ZippedFileEntry, bool> predicate;
        private readonly Action<ZippedFileEntry, Stream?> action;

        public readonly Encoding Encoding;
        public volatile UnzipCommonRoleContext? CommonRoleContext;

        public UnzipContext(
            string zipFilePath, int parallelCount, Encoding encoding,
            Func<ZippedFileEntry, bool> predicate,
            Action<ZippedFileEntry, Stream?> action)
        {
            this.Encoding = encoding;
            this.predicate = predicate;
            this.action = action;
            this.CommonRoleContext = new UnzipCommonRoleContext(zipFilePath);

            this.threadWorkers = new UnzipThreadWorker[parallelCount];
            for (var index = 0; index < this.threadWorkers.Length; index++)
            {
                this.threadWorkers[index] = new UnzipThreadWorker(zipFilePath, this);
            }
        }

        public bool Predicate(ZippedFileEntry entry) =>
            this.predicate(entry);

        public void OnAction(ZippedFileEntry entry, Stream? compressedStream) =>
            this.action(entry, compressedStream);

        public void Start()
        {
            for (var index = 0; index < this.threadWorkers.Length; index++)
            {
                this.threadWorkers[index].StartConsume();
            }
        }

        public void Finish()
        {
            for (var index = 0; index < this.threadWorkers.Length; index++)
            {
                this.threadWorkers[index].FinishConsume();
            }
        }
    }
}
