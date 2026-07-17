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

## SilenceDetectorExtensions
Extension methods for `SilenceDetector` that provide convenient querying and analysis of detected silence regions. These methods allow you to quickly determine if silence was detected, count regions, calculate durations (total, average, longest, shortest), and check if the entire audio is silence.

**Usage example**

```csharp
// Create a silence detector with a threshold of -40dB
var silenceDetector = new SilenceDetector(
    sampleRate: 44100,
    silenceThresholdDb: -40.0,
    minimumSilenceDuration: TimeSpan.FromMilliseconds(500)
);

// Process audio samples...
// silenceDetector.Process(sampleBuffer);

// Query silence statistics
if (silenceDetector.HasSilence())
{
    int regionCount = silenceDetector.GetSilenceRegionCount();
    double totalMs = silenceDetector.GetTotalSilenceDurationMilliseconds();
    TimeSpan totalDuration = silenceDetector.GetTotalSilenceDuration();
    TimeSpan averageDuration = silenceDetector.GetAverageSilenceDuration();
    
    var longestSilence = silenceDetector.GetLongestSilence();
    var shortestSilence = silenceDetector.GetShortestSilence();
    
    bool entirelySilence = silenceDetector.IsEntirelySilence();
}
```

