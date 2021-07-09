using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerPlayZipper.Internal
{
    internal static class IndependentFactory
    {
#if NETCOREAPP1_0 || NETSTANDARD1_3
        public static Encoding GetDefaultEncoding() =>
            Encoding.UTF8;
#else
        public static Encoding GetDefaultEncoding() =>
            Encoding.Default;
#endif

#if NET35
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
