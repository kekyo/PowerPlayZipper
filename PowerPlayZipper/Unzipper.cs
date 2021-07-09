using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using PowerPlayZipper.Internal.Unzip;

namespace PowerPlayZipper
{
    public sealed class Unzipper
    {
        private readonly bool ignoreDirectoryEntry;
        private readonly bool isOnlySupported;
        private readonly Encoding? encoding;
        private readonly int parallelCount;
        private readonly int streamBufferSize;

        public Unzipper(
            bool ignoreDirectoryEntry = false,
            bool isOnlySupported = true,
            Encoding? encoding = default,
            int parallelCount = -1,
            int streamBufferSize = -1)
        {
            this.ignoreDirectoryEntry = ignoreDirectoryEntry;
            this.isOnlySupported = isOnlySupported;
            this.encoding = encoding;
            this.parallelCount = (parallelCount >= 1) ? parallelCount : Environment.ProcessorCount;
            this.streamBufferSize = (streamBufferSize >= 1) ? streamBufferSize : 65536;
        }

        public event EventHandler<UnzippingEventArgs>? Unzipping;

        public async Task<ProcessedResults> UnzipAsync(
            string zipFilePath,
            Func<ZippedFileEntry, bool> predicate,
            Func<ZippedFileEntry, string> targetPathSelector)
        {
            var directoryConstructor = new DirectoryConstructor();
            var totalFiles = 0;
            var totalCompressedSize = 0L;
            var totalOriginalSize = 0L;

            var sw = new Stopwatch();
            sw.Start();

            var context = new UnzipContext(
                zipFilePath, this.parallelCount,
#if NETCOREAPP1_0 || NETSTANDARD1_4
                this.encoding ?? Encoding.UTF8,
#else
                this.encoding ?? Encoding.Default,
#endif
                predicate,
                (entry, compressedStream) =>
                {
                    var targetPath = targetPathSelector(entry);
                    var directoryPath = Path.GetDirectoryName(targetPath)!;
                    
                    this.Unzipping?.Invoke(this, new UnzippingEventArgs(entry, UnzippingStates.Begin, 0));

                    directoryConstructor.CreateIfNotExist(directoryPath);

                    if (compressedStream != null)
                    {
                        try
                        {
                            using (var fs = new FileStream(
                                targetPath,
                                FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                                this.streamBufferSize))
                            {
                                compressedStream.CopyTo(fs);
                                fs.Flush();
                            }

                            Interlocked.Increment(ref totalFiles);
                            Interlocked.Add(ref totalCompressedSize, entry.CompressedSize);
                            Interlocked.Add(ref totalOriginalSize, entry.OriginalSize);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    
                    this.Unzipping?.Invoke(this, new UnzippingEventArgs(entry, UnzippingStates.Done, entry.OriginalSize));
                });
            
            context.Start();

            return new ProcessedResults(
                totalFiles, totalCompressedSize, totalOriginalSize, sw.Elapsed);
        }

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath, Func<ZippedFileEntry, bool> predicate) =>
            UnzipAsync(
                zipFilePath,
                predicate,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));

        public Task<ProcessedResults> UnzipAsync(
            string zipFilePath, string extractToBasePath) =>
            UnzipAsync(
                zipFilePath,
                _ => true,
                entry => Path.Combine(extractToBasePath, entry.NormalizedFileName));
    }
}
