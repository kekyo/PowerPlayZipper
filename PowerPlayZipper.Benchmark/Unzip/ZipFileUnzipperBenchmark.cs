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
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PowerPlayZipper.Utilities;

namespace PowerPlayZipper.Unzip
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class ZipFileUnzipperBenchmark<TArtifact>
        where TArtifact : IConstant, new()
    {
        private Configurator? configuration;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.configuration = new Configurator(new TArtifact().ArtifactUrl);
            return this.configuration.SetUpAsync().AsTask();
        }

        private string? zfBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.zfBasePath = UnzipperTestCore.GetTempPath($"ZF{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(this.zfBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            FileSystemAccessor.DeleteDirectoryRecursive(this.zfBasePath!);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipByZipFileAsync(this.configuration!, this.zfBasePath!).AsTask();
    }
}
