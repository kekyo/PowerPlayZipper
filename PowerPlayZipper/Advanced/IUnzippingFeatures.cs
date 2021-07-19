using System.IO;

namespace PowerPlayZipper.Advanced
{
    public interface IUnzippingFeatures
    {
        Stream OpenForReadZipFile(int recommendedBufferSize);

        bool IsRequiredProcessing(ZippedFileEntry entry);
        string GetTargetPath(ZippedFileEntry entry);

        void CreateDirectoryIfNotExist(string directoryPath);

        Stream OpenForWriteFile(string path, int recommendedBufferSize);

        void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position);
    }
}
