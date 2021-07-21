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

namespace PowerPlayZipper.Utilities
{
    public static class PrettyPrintExtension
    {
        public static string ToBinaryPrefixString(this long value)
        {
            if (value == 0)
            {
                return $"0 Byte";
            }
            else if (value < 100000)
            {
                return $"{value} Bytes";
            }
            else if (value < (long)(1024.0 * 1024.0 / 0.9))
            {
                return $"{value/1024.0:F2} KiB";
            }
            else if (value < (long)(1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0:F2} MiB";
            }
            else if (value < (long)(1024.0 * 1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0:F2} GiB";
            }
            else
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0 / 1024.0:F2} TiB";
            }
        }

        public static string ToBinaryPrefixString(this double value)
        {
            if (value == 0.0)
            {
                return $"0 Byte";
            }
            else if (value < 10000.0)
            {
                return $"{value:F2} Bytes";
            }
            else if (value < (1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0:F2} KiB";
            }
            else if (value < (1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0:F2} MiB";
            }
            else if (value < (1024.0 * 1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0:F2} GiB";
            }
            else
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0 / 1024.0:F2} TiB";
            }
        }
    }
}
