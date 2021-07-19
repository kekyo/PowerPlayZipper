using System.IO;

namespace PowerPlayZipper.Advanced
{
    public interface IUnzippingFileFeatures
    {
        Stream OpenForRead(int recommendedBufferSize);

        bool IsRequiredProcessing(ZippedFileEntry entry);
        string GetTargetPath(ZippedFileEntry entry);

        void ConstructDirectory(string directoryPath);

        Stream OpenForWrite(string path, int recommendedBufferSize);
    }
}
