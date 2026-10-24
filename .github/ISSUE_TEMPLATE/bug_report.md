---
name: Bug report
about: Report a defect in an effect, helper, or the build
title: "[Bug] "
labels: bug
assignees: ""
---

## Description

<!-- A clear and concise description of the bug. Which effect or helper is affected? -->

## Steps to reproduce

1. 
2. 
3. 

<!-- A minimal code snippet is ideal. Include the effect configuration and how audio was fed in (buffer size, number of calls to Read, etc.). -->

```csharp

```

## Expected behavior

<!-- What you expected to happen. -->

## Actual behavior

<!-- What happened instead. Include exception messages and stack traces, or describe the audible artifact (clicks, silence, clipping, distortion, DC offset, etc.). -->

## Audio format and sample rate

- Sample rate (Hz):
- Channels:
- Sample format (`WaveFormat` type, e.g. IEEE float / PCM bit depth):
- Source provider (e.g. `WaveFileReader`, `MediaFoundationReader`, custom `ISampleProvider`):
- Buffer size per `Read` call (samples):

## Environment

- NAudioEffects version (or commit SHA):
- NAudio version:
- .NET version (output of `dotnet --version` and target framework):
- OS and version (e.g. Windows 11 23H2, Ubuntu 24.04, macOS 15.1):
- Architecture (x64 / arm64):

## Additional context

<!-- Logs, screenshots, sample audio files, or anything else that helps. Do not attach copyrighted audio. -->
