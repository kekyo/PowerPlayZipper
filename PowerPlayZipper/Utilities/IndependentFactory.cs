///////////////////////////////////////////////////////////////////////////
//
// PowerPlayZipper - An implementation of Lightning-Fast Zip file
// compression/decompression library on .NET.
// Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
///////////////////////////////////////////////////////////////////////////

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
