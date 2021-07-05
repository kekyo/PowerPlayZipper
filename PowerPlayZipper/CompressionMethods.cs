using PowerPlayZipper.Internal;
using System;
using System.IO;
using System.IO.Compression;

namespace PowerPlayZipper
{
    public enum CompressionMethods : short
    {
        Directory = -1,
        Stored = 0,
        Deflate = 8,
        Deflate64 = 9,
    }
}
