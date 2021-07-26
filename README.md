# DisposeGenerator
DisposeGenerator is a C# source generator that automatically implements IDisposable and IAsyncDisposable for you.

Manually implementing proper [IDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) and [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) patterns tends to bring a lot of boilerplate code.
This source generator aims to eleviate that problem so that you don't have to think about this pattern anymore.

Keep in mind that this project is in an early stage, and that bugs may occur.

## Limitations
- Other source generators that add additional members to a partial class, are not detected
- Nested IDisposable/IAsyncDisposable classes are not supported
- Inheritance is done on best effort, which means that the generator expects the base class to properly implement Microsoft's [IDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) and [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) and/or [IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) pattern.
- Probably more that I have overlooked...

## Examples
TODO

## Compiling
This project has been developed and tested in Visual Studio 2019 (16.10).

## Contributing
Because a proper IDisposable or IAsyncDisposable pattern is quite complicated, any contribution that adds features or fixes bugs are very much welcome.
