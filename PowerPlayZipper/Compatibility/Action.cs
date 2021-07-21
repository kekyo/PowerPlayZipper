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

namespace PowerPlayZipper.Compatibility
{
#if NET20
    public delegate void Action();
    public delegate void Action<T0, T1>(T0 arg0, T1 arg1);
    public delegate void Action<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);
    public delegate void Action<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3);
#endif
}
