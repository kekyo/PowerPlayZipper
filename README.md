# PowerPlay Zipper

![PowerPlay Zipper](Images/PowerPlayZipper.120.png)

[![Project Status: Concept – Minimal or no implementation has been done yet, or the repository is only intended to be a limited example, demo, or proof-of-concept.](https://www.repostatus.org/badges/latest/concept.svg)](https://www.repostatus.org/#concept)

|Status|Badge|
|:---|:---|
|NuGet (Library)|[![NuGet PowerPlayZipper](https://img.shields.io/nuget/v/PowerPlayZipper.svg?style=flat)](https://www.nuget.org/packages/PowerPlayZipper)|
|NuGet (.NET CLI tool)|[![NuGet ppzip](https://img.shields.io/nuget/v/ppzip.svg?style=flat)](https://www.nuget.org/packages/ppzip)|
|CI|[![PowerPlayZipper CI build (main)](https://github.com/kekyo/PowerPlayZipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/PowerPlayZipper/actions)|

# What's this?

PowerPlay Zipper is an implementation of `Lightning-Fast` Zip file compression/decompression library on .NET.
Simple word for strategy: **Maximize multi-core parallel file compression/decompression**.

* 3x and over faster unzipping execution than `SharpZipLib.FastZip` / `System.IO.Compression.ZipFile`.

Yes, we can easy replace PowerPlay Zipper from another zip manipulation library.

## Sample unzipping (Decompression)

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

## TODO: Sample zipping (Compression)

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

## .NET CLI tool

```sh
# Install ppzip command from NuGet.
> dotnet tool install --global ppzip

# Unzip a zip file.
> ppzip -u MixedRealityToolkit-master.zip
MixedRealityToolkit-master.zip: Unzipping ... Done.
  Elapsed    : 00:00:00.9144408
  Files      : 2491 [2724.07files/sec]
  Compressed : 164.35MiB [179.73MiB/sec]
  Expanded   : 261.63MiB [286.11MiB/sec]
  Ratio      : 159.19%
  Parallel   : 36 [36]
```

# Unzipping performance

Using these zip files on Benchmark .NET:

* [GitHub zipped dotnet/docs repo ~300MB (Contains many standard size text files)](https://github.com/dotnet/docs/archive/7814398e1e1b5bd7262f1932b743e9a30caef2c5.zip)
* [GitHub zipped Mixed Reality Toolkit repo ~160MB (Contains large files)](https://github.com/microsoft/MixedRealityToolkit/archive/b63b40b9a4bd4e350f35986d450dd5393c6e58a0.zip)
* PowerPlayZipper 0.0.47 `Unzipper.UnzipAsync()`
* SharpZipLib 1.3.2 `FastZip.ExtractZip()`
* System.IO.Compression 

Windows 10 on Core i9-9980XE (36 cores):

```
BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
Intel Core i9-9980XE CPU 3.00GHz, 1 CPU, 36 logical and 18 physical cores
.NET SDK=5.0.104
  [Host]   : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  .NET 5.0 : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  InvocationCount=1  
UnrollFactor=1  
```

Ubuntu 20.04 on Core i9-10900K (20 cores):

```
BenchmarkDotNet=v0.13.0, OS=ubuntu 20.04
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=5.0.301
  [Host]   : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
  .NET 5.0 : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  InvocationCount=1  
UnrollFactor=1  
```

## dotnet/docs repo

* Ubuntu 20.04: 3.1x faster than `System.IO.Compression`.
* Windows 10 19043: 2.2x faster than `SharpZipLib`.

| Ubuntu 20.04 | Mean | Error | StdDev |
|:-----|-------:|---------:|---------:|
| PowerPlayZipper | 679.6 ms | 13.10 ms | 14.56 ms |
| SharpZipLib | 4.105 s | 0.0129 s | 0.0101 s |
| System.IO.Compression | 2.117 s | 0.0099 s | 0.0093 s |

| Windows 10 19043 | Mean | Error | StdDev |
|:-----|-------:|---------:|---------:|
| PowerPlayZipper | 7.936 s | 0.1811 s | 0.5225 s |
| SharpZipLib | 17.80 s | 0.122 s | 0.108 s |
| System.IO.Compression | 18.35 s | 0.092 s | 0.086 s |

## Mixed Reality Toolkit repo

* Ubuntu 20.04: 2.3x faster than `System.IO.Compression`.
* Windows 10 19043: 2.5x faster than `System.IO.Compression`.

| Ubuntu 20.04 | Mean | Error | StdDev |
|:-----|-------:|---------:|---------:|
| PowerPlayZipper | 333.5 ms | 6.65 ms | 19.38 ms |
| SharpZipLib | 1.998 s | 0.0053 s | 0.0042 s |
| System.IO.Compression | 788.0 ms | 2.25 ms | 1.99 ms |

| Windows 10 19043 | Mean | Error | StdDev |
|:-----|-------:|---------:|---------:|
| PowerPlayZipper | 965.6 ms | 48.51 ms | 140.7 ms |
| SharpZipLib | 4.294 s | 0.0824 s | 0.1072 s |
| System.IO.Compression | 2.381 s | 0.0460 s | 0.0615 s |

# Supported platforms

PowerPlay Zipper is made with neutral/independent any reference. See [NuGet dependency list.](https://www.nuget.org/packages/PowerPlayZipper)

* .NET 5.0 or higher.
* .NET Core 3.1, 3.0, 2.1, 2.0 and 1.0.
* .NET Standard 2.1, 2.0, 1.6 and 1.3.
  * `netstandard13` and `netstandard16` referred `System.Threading.Thread 4.0.0`.
* .NET Framework 4.8, 4.6.2, 4.5, 4.0, 3.5 and 2.0.
  * `net40` referred `Microsoft.Bcl.Async 1.0.168`.

# Migration layer

If you use `ICSharpCode.SharpZipLib.FastZip` or `System.IO.Compression.ZipFile`,
PowerPlay Zipper produces similar interface for `FastZip` and `ZipFile` classes.

```csharp
// use PowerPlayZipper migration layer.
using PowerPlayZipper.Migration;
//using ICSharpCode.SharpZipLib.Zip;

var fastZip = new FastZip();
fastZip.ExtractZip("zipfile.zip", "C:\output", "");
```

```csharp
// use PowerPlayZipper migration layer.
using PowerPlayZipper.Migration;
//using System.IO.Compression;

ZipFile.ExtractToDirectory("zipfile.zip", "C:\output");
```

# Limitation

* Will not support these zip file features:
  * Only supported `deflate` compression algorithm, will not support any others.
  * Any encryption and decryption feature.
  * Continuous streaming unzip. PowerPlayZipper depends seekable file accessing.
  * Supported only synchronized interface between `net20` and `net35` platforms.

# License

Under Apache v2.

# Histroy

* 0.0.32: Rewrite new generation 3 unzip code made faster to 3x (dotnet/docs).
* 0.0.29: Added .NET CLI tool package named `ppzip`.
* 0.0.27: Minor bug fixed.
* 0.0.12: First NuGet package released.
