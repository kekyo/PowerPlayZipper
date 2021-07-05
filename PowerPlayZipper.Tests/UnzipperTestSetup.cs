using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PowerPlayZipper
{
    public sealed class UnzipperTestSetup
    {
        public readonly string ZipFilePath;

        private readonly Uri artifactUrl;

        public UnzipperTestSetup(string artifactUrl)
        {
            this.artifactUrl = new Uri(artifactUrl, UriKind.RelativeOrAbsolute);
            var testBasePath = Path.GetDirectoryName(typeof(UnzipperTestSetup).Assembly.Location)!;
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
