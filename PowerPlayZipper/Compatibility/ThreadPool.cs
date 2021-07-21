#if NETSTANDARD1_3 || NETSTANDARD1_6
using System;
using System.Threading.Tasks;
#endif

namespace PowerPlayZipper.Compatibility
{
#if NETSTANDARD1_3 || NETSTANDARD1_6
    public static class ThreadPool
    {
        public static void QueueUserWorkItem(Action<object> action) =>
            Task.Run(() => action(null!));
    }
#endif
}
