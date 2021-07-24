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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    public sealed class Configurator
    {
        public readonly string ZipFilePath;

        private readonly Uri artifactUrl;

        public Configurator(string artifactUrl)
        {
            this.artifactUrl = new Uri(artifactUrl, UriKind.RelativeOrAbsolute);
            var testBasePath = Path.GetDirectoryName(typeof(Configurator).Assembly.Location)!;
            this.ZipFilePath = Path.Combine(testBasePath, this.artifactUrl.PathAndQuery.Split('/').Last());
        }

        public async ValueTask SetUpAsync()
        {
            if (!File.Exists(this.ZipFilePath))
            {
                Debug.WriteLine("Downloading test artifact...");

                var httpClient = new HttpClient();
                using (var stream = await httpClient.GetStreamAsync(this.artifactUrl))
                {
                    using (var fs = File.Create($"{this.ZipFilePath}.temp"))
                    {
                        await stream.CopyToAsync(fs);
                        await fs.FlushAsync();
                    }
                    File.Move($"{this.ZipFilePath}.temp", this.ZipFilePath);
                }

                Debug.WriteLine("Downloaded.");
            }
        }
    }
}
