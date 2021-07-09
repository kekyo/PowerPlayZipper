using System;

namespace PowerPlayZipper.Advanced
{
#if NET35
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
