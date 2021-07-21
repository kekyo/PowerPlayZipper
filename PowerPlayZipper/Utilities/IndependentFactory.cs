using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerPlayZipper.Utilities
{
    internal static class IndependentFactory
    {
#if NETCOREAPP1_0 || NETSTANDARD1_3 || NETSTANDARD1_6
        public static Encoding GetSystemDefaultEncoding() =>
            Encoding.UTF8;
#else
        public static Encoding GetSystemDefaultEncoding() =>
            Encoding.Default;
#endif

#if NET20 || NET35
        public static ManualResetEvent CreateManualResetEvent() =>
            new ManualResetEvent(false);

        public static void Wait(this ManualResetEvent ev) =>
            ev.WaitOne();

        public static Exception GetAggregateException(List<Exception> exceptions) =>
            exceptions[0];
#else
        public static ManualResetEventSlim CreateManualResetEvent() =>
            new ManualResetEventSlim(false);

        public static AggregateException GetAggregateException(List<Exception> exceptions) =>
            new AggregateException(exceptions);
#endif
    }
}
