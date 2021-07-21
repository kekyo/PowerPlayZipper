using System.IO;

namespace PowerPlayZipper.Advanced
{
    public interface IUnzipperTraits
    {
        void Started();

        Stream OpenForReadZipFile(int recommendedBufferSize);

        bool IsRequiredProcessing(ZippedFileEntry entry);

        string GetTargetPath(ZippedFileEntry entry);

        string GetDirectoryName(string path);

        void CreateDirectoryIfNotExist(string directoryPath);

        Stream? OpenForWriteFile(string path, int recommendedBufferSize);

        void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position);

        void Finished();
    }
}
