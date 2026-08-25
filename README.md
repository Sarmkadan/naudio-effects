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

## ChorusSampleProvider
The ChorusSampleProvider class applies a chorus effect to an audio stream. It creates a shimmering, thickening effect by duplicating the input signal with a delayed, pitch-modulated copy. You can adjust the RateHz, DepthMs, Mix, and BaseDelayMs properties to fine-tune the chorus effect.

**Usage example**
```csharp
var chorus = new ChorusSampleProvider(new SampleProvider());
chorus.RateHz = 0.5f;
chorus.DepthMs = 2.0f;
chorus.Mix = 0.2f;
chorus.BaseDelayMs = 15.0f;
```

## EnvelopeFollowerTests
The EnvelopeFollowerTests class contains the unit tests for the EnvelopeFollower component, which implements a classic attack/release envelope detector used by dynamics effects such as compressors and gates. The tests verify correct initialization with default and custom parameters, that SetParameters recomputes the smoothing coefficients, and that the envelope rises on positive input, rectifies negative samples, decays toward zero on silence, and tracks mixed signals according to the configured attack and release times.

**Usage example**

```csharp
// Instantiate the test suite and exercise its checks directly
var tests = new EnvelopeFollowerTests();

// Constructor and parameter handling
tests.Constructor_WithDefaultParameters_InitializesCorrectly();
tests.Constructor_WithCustomParameters_InitializesCorrectly();
tests.SetParameters_UpdatesCoefficients();

// Envelope tracking behavior
tests.Process_WithZeroSamples_EnvelopeDecaysToZero();
tests.Process_WithPositiveSamples_EnvelopeRises();
tests.Process_WithNegativeSamples_EnvelopeRectifiesToPositive();
tests.Process_WithRisingInput_EnvelopeIncreasesPerAttack();
tests.Process_WithFallingInput_EnvelopeDecaysPerRelease();
tests.Process_WithMixedSamples_TracksEnvelopeCorrectly();

// Coefficient calculation
tests.CalculateCoefficient_WithZeroTimeConstant_ReturnsZero();
tests.CalculateCoefficient_WithPositiveTimeConstant_ReturnsValidCoefficient();
```
