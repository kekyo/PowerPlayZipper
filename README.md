# PowerPlay Zipper

[![Project Status: Concept – Minimal or no implementation has been done yet, or the repository is only intended to be a limited example, demo, or proof-of-concept.](https://www.repostatus.org/badges/latest/concept.svg)](https://www.repostatus.org/#concept)

|Status|Badge|
|:---|:---|
|NuGet|[![NuGet PowerPlayZipper](https://img.shields.io/nuget/v/PowerPlayZipper.svg?style=flat)](https://www.nuget.org/packages/PowerPlayZipper)|
|CI|[![PowerPlayZipper CI build (main)](https://github.com/kekyo/PowerPlayZipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/PowerPlayZipper/actions)|

# What's this?

PowerPlay Zipper is an implementation of `Lightning-Fast` Zip file compression/decompression library on .NET.

* Double faster unzipping execution than `SharpZipLib.FastZip`.

Yes, we can easy replacing PowerPlay Zipper from another zip manipulation library.

## Simpler unzip (Decompression)

```csharp
// Install NuGet "PowerPlayZipper" package.
using PowerPlayZipper;

public async Task YourUnzipTaskWithVeryLargeZipFile(
    string zipFilePath, string storeDirectoryPath)
{
    Unzipper unzipper = new Unzipper();
    ProcessedResults result = await unzipper.UnzipAsync(
        zipFilePath, storeDirectoryPath);

    Console.WriteLine(result);
}
```

## Simpler zip (Compression)

```csharp
// Install NuGet "PowerPlayZipper" package.
using PowerPlayZipper;

public async Task YourZipTaskWithManyFiles(
    string zipFilePath, string targetDirectoryPath)
{
    Zipper zipper = new Zipper();
    ProcessedResults result = await zipper.ZipAsync(
        zipFilePath, targetDirectoryPath);

    Console.WriteLine(result);
}
```

# Performance

## Unzipping (Decompress) performance

Using [GitHub zipped dotnet/docs repo](https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip) on Benchmark .NET.

### PowerPlayZipper 0.0.11 Unzipper

``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
Intel Core i9-9980XE CPU 3.00GHz, 1 CPU, 36 logical and 18 physical cores
.NET SDK=5.0.104
  [Host]   : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  .NET 5.0 : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  InvocationCount=1  
UnrollFactor=1  

```
| Method |    Mean |   Error |  StdDev |  Median |
|------- |--------:|--------:|--------:|--------:|
|    Run | 12.12 s | 0.963 s | 2.762 s | 11.17 s |

### SharpZipLib 1.3.2 FastZip.ExtractZip()

``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
Intel Core i9-9980XE CPU 3.00GHz, 1 CPU, 36 logical and 18 physical cores
.NET SDK=5.0.104
  [Host]   : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  .NET 5.0 : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  InvocationCount=1  
UnrollFactor=1  

```
| Method |    Mean |   Error |  StdDev |
|------- |--------:|--------:|--------:|
|    Run | 19.27 s | 0.264 s | 0.234 s |

# Supported platforms

PowerPlay Zipper is made with neutral/independent reference.

* .NET 5.0 or higher.
* .NET Core 3.1, 3.0, 2.1, 2.0 and 1.0.
* .NET Standard 2.1, 2.0 and 1.3.
  * netstandard13 and netstandard16 refered "System.Threading.Thread 4.0.0."
* .NET Framework 4.8, 4.6.2, 4.6.1, 4.5, 4.0, 3.5 and 2.0.
  * net40 refered "Microsoft.Bcl.Async 1.0.168."

TODO: Still under construction...

# License

Under Apache v2.

# Histroy
