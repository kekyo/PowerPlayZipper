using System;

namespace PowerPlayZipper
{
    public interface IZipperProcessing
    {
        event EventHandler<ProcessingEventArgs>? Processing;
    }
}
