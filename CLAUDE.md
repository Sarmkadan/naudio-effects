# CLAUDE.md

NAudioEffects: a .NET 10 class library of DSP effects (compressor, limiter, EQ, gate, chorus, flanger, reverb, etc.) implemented as NAudio `ISampleProvider` wrappers, with xunit tests in the same project.

## Build

```bash
dotnet build            # single project: naudio-effects.csproj -> bin/Debug/net10.0/NAudioEffects.dll
dotnet build -c Release
```

No solution file; the csproj at repo root is the only project. `GenerateDocumentationFile` is on, so every public member needs an XML doc comment or the build emits CS1591 warnings.

## Test

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~GainSampleProvider"
```

Framework: xunit 2.9 (`[Fact]`, `[Theory]`). Tests live in the library project itself (no separate test csproj).

Known issue: `dotnet test` currently aborts with "NAudio.dll not found" because the project has no `Microsoft.NET.Test.Sdk` reference and package assemblies are not copied to the output folder. Fixing it means adding `Microsoft.NET.Test.Sdk` (or `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>`) to the csproj. `dotnet build` works.

## Lint / format

No analyzers, `.editorconfig`, or CI configured. `dotnet format` is the only available option. Note: source files mix 4-space indentation (most files) and tabs (`*JsonExtensions.cs`); match the file you are editing.

## Layout

```
naudio-effects.csproj   single project, net10.0, Nullable + ImplicitUsings enabled
src/                    all effects and helpers, namespace NAudioEffects (flat, no sub-namespaces)
test/                   xunit tests, namespace NAudioEffects.Tests
docs/                   per-class markdown docs (ChorusSampleProvider.md, SilenceDetector.md, ...)
README.md               per-class summaries with usage examples
```

Key entry points:
- `src/EffectSampleProviderBase.cs` - abstract base for effects: wraps a source `ISampleProvider`, exposes `WaveFormat`, `Bypass`, and calls the abstract `ProcessBlock(float[] buffer, int offset, int samplesRead)` from `Read`. Derive new effects from it.
- `src/EffectChainPresets.cs` - static factory methods composing several effects into preset chains.
- `src/SilenceDetector.cs`, `src/EnvelopeFollower.cs`, `src/MidSideProcessor.cs` - standalone analysis/processing classes not derived from the base.

Repo root also contains a few stray tracked files with code-fragment names (`}`, `compressor.Process(...)`, `Build & commit:** ...`). They are artifacts of a past tool run, not source; ignore them and do not add more.

## Conventions

- One public type per file, file name equals type name: `<Name>SampleProvider.cs`.
- Companion files per effect follow fixed suffixes:
  - `<Name>Extensions.cs` - static extension methods (fluent configuration, queries).
  - `<Name>Validation.cs` - `EnsureValid()` / `IsValid()` helpers throwing `ArgumentException`.
  - `<Name>JsonExtensions.cs` - `ToJson()` / `FromJson()` via `System.Text.Json`, camelCase naming.
- Tests: `test/<Name>Tests.cs`, class `<Name>Tests`, method names describe behavior (`Read_WithGainZero_ReturnsSilence` style). Tests define tiny private `ISampleProvider` stubs (e.g. `ConstantSampleProvider`) instead of using mocks.
- Effects take the source `ISampleProvider` in the constructor and throw `ArgumentNullException` on null; parameters are public get/set properties with validation in setters.
- Audio buffers are `float[]` with `(buffer, offset, count)` signatures matching NAudio.
- About half the files start with `#nullable enable`; most have explicit `using` lines even though ImplicitUsings is on; block-scoped `namespace NAudioEffects { ... }` is the dominant style (a few newer files use file-scoped namespaces).
- Public API is fully XML-documented (`<summary>`, `<param>`, `<returns>`).
- When adding an effect, also add: a test file, a `docs/<Name>.md`, and a README section.
