using System;

namespace PowerPlayZipper.Compatibility
{
#if NET20 || NET35
    public struct CancellationToken
    {
        public void Register(Action callback)
        {
        }

        public void ThrowIfCancellationRequested()
        {
        }
    }
#endif
}
