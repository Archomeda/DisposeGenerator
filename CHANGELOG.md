# DisposeGenerator History

## 0.2.1 (17 December 2021)
- Fix members with explicit `IDisposable` or `IAsyncDisposable` type from not being disposed

## 0.2.0 (16 November 2021)
- Add support to explicitly dispose properties with `IncludeDisposeAttribute`
- Move all attributes from the `DisposeGenerator.Attributes` to the `DisposeGenerator` namespace

---

## 0.1.3 (15 November 2021)
- Downgrade Microsoft.CodeAnalysis.CSharp to 3.9.0 to support Visual Studio 2019 16.9 and up

## 0.1.2 (15 November 2021)
- Fix packing as development dependency

## 0.1.1 (15 November 2021)
- Fix packing the source generator as analyzer

## 0.1.0 (15 November 2021)
This is the initial release.
