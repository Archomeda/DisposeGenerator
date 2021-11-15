# DisposeGenerator
[![GitHub CI](https://img.shields.io/github/workflow/status/Archomeda/DisposeGenerator/CI/master?label=CI&logo=GitHub)](https://github.com/Archomeda/DisposeGenerator/actions?workflow=CI)
[![NuGet](https://img.shields.io/nuget/v/DisposeGenerator.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/DisposeGenerator)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DisposeGenerator.svg?label=Downloads&logo=nuget)](https://www.nuget.org/packages/DisposeGenerator)

DisposeGenerator is a C# source generator that automatically implements IDisposable and IAsyncDisposable for you.

Manually implementing proper [IDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) and [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) patterns tends to bring a lot of boilerplate code.
This source generator aims to alleviate that problem so that you don't have to think about this pattern anymore.

Keep in mind that this project is in an early stage, and that bugs may occur.

## Limitations
- Other source generators that add additional members to a partial class, are not detected
- Nested IDisposable/IAsyncDisposable classes are not supported
- Inheritance is done on best effort, which means that the generator expects the base class to properly implement Microsoft's [IDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) and [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) and/or [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) pattern.
- Probably more that I have overlooked...

## Installing
You can find the library on [NuGet](https://www.nuget.org/packages/DisposeGenerator/). Or, alternatively, you can install it by running `dotnet add package DisposeGenerator` in a console, or `Install-Package DisposeGenerator` in the package manager console.

## Usage
TODO

## Compiling
This project has been developed and tested in Visual Studio 2019 (16.10).

## Contributing
Because a proper IDisposable or IAsyncDisposable pattern is quite complicated, any contribution that adds features or fixes bugs are very much welcome.
