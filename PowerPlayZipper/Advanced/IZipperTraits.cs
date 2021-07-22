﻿///////////////////////////////////////////////////////////////////////////
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

using System.IO;

namespace PowerPlayZipper.Advanced
{
    public interface IZipperTraits
    {
        void Started();

        Stream? OpenForReadFile(string path, int recommendedBufferSize);

        bool IsRequiredProcessing(ZippedFileEntry entry);

        string GetTargetPath(ZippedFileEntry entry);

        string GetDirectoryName(string path);

        void CreateDirectoryIfNotExist(string directoryPath);

        Stream OpenForWriteZipFile(int recommendedBufferSize);

        void OnProcessing(ZippedFileEntry entry, ProcessingStates state, long position);

        void Finished();
    }
}
