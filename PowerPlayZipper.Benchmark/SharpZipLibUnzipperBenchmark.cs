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

namespace PowerPlayZipper
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class SharpZipLibUnzipperBenchmark<TArtifact>
        where TArtifact : IArtifact, new()
    {
        private UnzipperTestSetup? setup;

        [GlobalSetup]
        public Task GlobalSetup()
        {
            this.setup = new UnzipperTestSetup(new TArtifact().ArtifactUrl);
            return this.setup.SetUpAsync().AsTask();
        }

        private string? szlBasePath;

        [IterationSetup]
        public void Setup()
        {
            var now = DateTime.Now.ToString("mmssfff");
            this.szlBasePath = UnzipperTestCore.GetTempPath($"SZL{now}");

            FileSystemAccessor.CreateDirectoryIfNotExist(this.szlBasePath);
        }

        [IterationCleanup]
        public void Cleanup() =>
            FileSystemAccessor.DeleteDirectoryRecursive(this.szlBasePath!);

        [Benchmark]
        public Task Run() =>
            UnzipperTestCore.UnzipBySharpZipLibAsync(this.setup!, this.szlBasePath!).AsTask();
    }
}
