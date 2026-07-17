# naudio-effects

DSP effects for NAudio (compressor, limiter, EQ, gate) as ISampleProvider.

> v0.1 in progress.

## SilenceDetector
The SilenceDetector class detects periods of silence in an audio stream. It can be used to identify sections of audio that are below a certain threshold. You can use it to process audio in real-time by calling Process repeatedly, and then call Complete to finalize the detection.

## CompressorSampleProvider
The CompressorSampleProvider class applies a compressor effect to an audio stream. It can be used to reduce the dynamic range of an audio signal. You can adjust the threshold, ratio, attack, release, knee, and makeup gain to fine‑tune the compression.

## CompressorSampleProviderExtensions
Extension methods for `CompressorSampleProvider` give a fluent, immutable‑style API for configuring and querying a compressor. They let you create a compressor, adjust its parameters in a chainable way, reset it to defaults, and inspect runtime state such as the current gain‑reduction factor or whether compression is active.

**Usage example**

