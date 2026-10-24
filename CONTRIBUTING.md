# Contributing to NAudioEffects

Thanks for helping out. This is a single-project .NET library, so setup is short.

## Prerequisites

- .NET SDK 10.0 (the project targets `net10.0`; check with `dotnet --version`)
- Git

## Build

```bash
dotnet build                 # Debug build
dotnet build -c Release      # Release build
```

`GenerateDocumentationFile` is enabled, so every public type and member needs an XML doc comment. Missing ones produce CS1591 warnings.

## Test

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~GainSampleProvider"   # single class
```

Tests use xunit and live in `test/` inside the library project.

## Code style

- No `.editorconfig` or analyzer configuration exists yet. Match the file you are editing: most files use 4-space indentation, `*JsonExtensions.cs` uses tabs.
- One public type per file, named after the type (`<Name>SampleProvider.cs`).
- Public API has `<summary>`, `<param>`, and `<returns>` XML docs.
- Effects take the source `ISampleProvider` in the constructor and throw `ArgumentNullException` on null.
- Warnings are not treated as errors yet. Before opening a PR, check that your change adds no warnings:

  ```bash
  dotnet build -c Release -warnaserror
  ```

  (This fails on any existing warning too, so compare against `main` if the baseline is not clean.)

## Branch naming

Create a branch from `main` with a short prefix and a kebab-case description:

- `feature/<short-description>` for new effects or features
- `fix/<short-description>` for bug fixes
- `docs/<short-description>` for documentation-only changes

## Pull request checklist

Before requesting review, confirm:

- [ ] `dotnet build -c Release` succeeds
- [ ] `dotnet test` passes
- [ ] Your change adds no new compiler warnings
- [ ] New public members have XML doc comments
- [ ] New effects include a test file, a `docs/<Name>.md` page, and a README section
- [ ] The PR description explains what changed and why, and how you tested it

CI runs build and test on pull requests targeting `main`.
