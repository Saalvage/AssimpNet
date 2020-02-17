![alt text](https://bitbucket.org/Starnick/assimpnet/raw/64485416c27d84b2928ba375d7ae51c8ab24bdb7/logo.png "AssimpNet Logo")

**The latest release can be downloaded via [NuGet](https://www.nuget.org/packages/AssimpNet/).**

[![Build Status](https://dev.azure.com/nicholaswoodfield/OSS-Libraries/_apis/build/status/bitbucket/starnick/AssimpNet?branchName=master)](https://dev.azure.com/nicholaswoodfield/OSS-Libraries/_build/latest?definitionId=6&branchName=master)

## Introduction ##
This is the official repository for **AssimpNet**, the cross-platform .NET wrapper for the Open Asset Import Library (otherwise known as [Assimp](https://github.com/assimp/assimp)), which is a 3D model import-export library. The primary motivation is for this library to power (offline) content pipelines to import and process 3D models into your game engine's internal format, although the wrapper can be used at runtime to enable your users to import custom content. Please see the Assimp website for a full list of supported formats and features. Each version of the managed wrapper tries to maintain parity with the features of the native version.

P/Invoke is used to communicate with the C-API of the native library. The managed assembly is compiled as **AnyCpu** and the native binaries are loaded dynamically for either 32 or 64 bit applications.

The library is split between two parts, a low level and a high level. The intent is to give as much freedom as possible to the developer to work with the native library from managed code.

### Low level ###

* Native methods are exposed via the AssimpLibrary singleton.
* Structures corresponding to unmanaged structures are prefixed with the name **Ai** and generally contain IntPtrs to the unmanaged data.
* Located in the *Assimp.Unmanaged* namespace.

### High level ###

* Replicates the native library's C++ API, but in a way that is more familiar to C# developers.
* Marshaling to and from managed memory handled automatically, all you need to worry about is processing your data.
* Located in the *Assimp* namespace.

## Supported Frameworks ##

The library runs on both **.NET Core** and **.NET Framework**, targeting specifically:

* **.NET Standard 1.3**
* **.NET Framework 4.0**
* **.NET Framework 3.5**

This means the NuGet package is compatible with a **wide range** of applications. When targeting .NET Framework, the package uses a MSBuild targets file to copy native binaries to your application output folder. For .NET Core applications, the native binaries are resolved by the *deps.json* dependency graph automatically.

The library can be compiled on any platform that supports  the DotNet CLI build tools or Visual Studio 2017. There is a single **build-time only** dependency, an IL Patcher also distributed as a cross-platform NuGet package. The patcher requires .NET Core 2.0+ or .NET Framework 4.7+ to be installed on your machine to build.

## Supported Platforms ##

The NuGet package supports the following Operating Systems and Architectures out of the box (located in the *runtimes* folder, under [RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)-specific folders):

* **Windows** 
	* x86, x64 (Tested on Windows 10)
* **Linux**
	* x64 (Tested on Ubuntu 18.04 Bionic Beaver)
* **MacOS**
	* x64 (Tested on MacOS 10.13 High Sierra)

You may have to build and provide your own native binaries for a target platform that is not listed. If the library does not support a platform you are targeting, please let us know or contribute an implementation! The logic to dynamically load the native library is abstracted, so new platform implementations can easily be added.

## Unity Users ##

With the release of version 4.1.0, a Unity plugin replicating the NuGet package is outputted to the build folder. You can simply drag and drop the contents into your Unity project. The plugin utilizes a
runtime initiliazation script to ensure the native binaries are loaded when running in editor or standalone.

## Licensing ##

The library is licensed under the [MIT](https://opensource.org/licenses/MIT) license. This means you're free to modify the source and use the library in whatever way you want, as long as you attribute the original authors. The native library is licensed under the [3-Clause BSD](https://opensource.org/licenses/BSD-3-Clause) license. Please be kind enough to include the licensing text file (it contains both licenses).

## Contact ##

Follow project updates and more on [Twitter](https://twitter.com/Tesla3D/).

In addition, check out these other projects from the same author:

[TeximpNet](https://bitbucket.org/Starnick/teximpnet) - A wrapper for the Nvidia Texture Tools and FreeImage libraries, which is a sister library to this one.

[MemoryInterop.ILPatcher](https://bitbucket.org/Starnick/memoryinterop.ilpatcher) - This is the ILPatcher that is required at build time, it uses Mono.Cecil to inject IL code to improve native interop. The ILPatcher is cross-platform, which enables building of AssimpNet on non-windows platforms.

[Tesla Graphics Engine](https://bitbucket.org/Starnick/tesla3d) - A 3D rendering engine written in C# and the primary driver for developing AssimpNet.