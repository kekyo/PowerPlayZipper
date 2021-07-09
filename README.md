# PowerPlay Zipper

[![Project Status: Concept – Minimal or no implementation has been done yet, or the repository is only intended to be a limited example, demo, or proof-of-concept.](https://www.repostatus.org/badges/latest/concept.svg)](https://www.repostatus.org/#concept)

|Status|Badge|
|:---|:---|
|NuGet|[![NuGet PowerPlayZipper](https://img.shields.io/nuget/v/PowerPlayZipper.svg?style=flat)](https://www.nuget.org/packages/PowerPlayZipper)|
|CI|[![PowerPlayZipper CI build (main)](https://github.com/kekyo/PowerPlayZipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/PowerPlayZipper/actions)|

# What's this?

PowerPlay Zipper is an implementation of `Lightning-Fast` Zip file compression/decompression library on .NET.

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

TODO: Still under construction...

## Many supported platforms

PowerPlayZipper is made with neutralize/independent other libs.

* .NET 5.0 or higher.
* .NET Core 3.1, 3.0, 2.1, 2.0 and 1.0.
* .NET Standard 2.1, 2.0 and 1.3.
* .NET Framework 4.0, 4.5, 4.6.2 and 4.8.

TODO: Still under construction...

# License

Under Apache v2.

# Histroy
