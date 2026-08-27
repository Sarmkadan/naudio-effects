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

## FlangerSampleProvider
The FlangerSampleProvider class applies a flanger effect to an audio stream by mixing the input with a delayed, phase-modulated copy of itself. The delay time is modulated by a low-frequency oscillator (LFO) to create the characteristic sweeping sound. You can adjust the LFO rate (RateHz), modulation depth (DepthMs), feedback amount (Feedback), base delay time (DelayMs), and wet/dry mix (Mix) to fine-tune the effect.

**Usage example**
```csharp
var flanger = new FlangerSampleProvider(new SampleProvider());
flanger.RateHz = 0.2f;
flanger.DepthMs = 1.5f;
flanger.Feedback = 0.3f;
flanger.DelayMs = 0.5f;
flanger.Mix = 0.4f;
```

## NoiseGateSampleProvider
The NoiseGateSampleProvider class implements a noise gate that attenuates audio below a specified threshold with smooth attack, release, and hold times. It can be used to silence quiet parts of an audio signal while preserving louder sections.

**Usage example**
```csharp
var noiseGate = new NoiseGateSampleProvider(new SampleProvider());
noiseGate.ThresholdDb = -20.0f;
noiseGate.AttackMs = 10.0f;
noiseGate.ReleaseMs = 100.0f;
noiseGate.HoldMs = 50.0f;
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

## SilenceDetectorValidation
The `SilenceDetectorValidation` class provides static extension methods that help you verify a `SilenceDetector` (and its nested `SilenceRegion` objects) is correctly configured. You can retrieve a list of validation problems, check validity with a boolean, or have the method throw an exception when the detector is invalid.

**Usage example**

## MidSideProcessorTests
The `MidSideProcessorTests` class provides a comprehensive test suite for the `MidSideProcessor` effect, validating mid/side channel separation, independent gain control, and smoothing behavior. It verifies correct handling of mono and stereo inputs, bypass functionality, and various gain configurations to ensure accurate audio processing.

**Usage example**
```csharp
// Instantiate the test suite and exercise its checks directly
var tests = new MidSideProcessorTests();

// Constructor validation
tests.Constructor_WithMonoSource_ThrowsArgumentException();
tests.Constructor_WithStereoSource_Succeeds();

// Property updates
tests.MidGainDb_WhenSet_UpdatesCurrentGain();
tests.SideGainDb_WhenSet_UpdatesCurrentGain();
tests.MidSmoothingMs_WhenSet_UpdatesInterval();
tests.SideSmoothingMs_WhenSet_UpdatesInterval();

// Bypass and signal processing
tests.Bypass_WhenTrue_DoesNotProcessSamples();
tests.MidOnlySignal_WithMidGain_AmplifiesCenter();
tests.SideOnlySignal_WithSideGain_AmplifiesStereoImage();
tests.StereoSignal_WithDifferentMidSideGains_ProcessesIndependently();
tests.ZeroGains_ProducesOriginalSignal();
tests.NegativeMidGain_ReducesCenterContent();
tests.LargeSideGain_CreatesWideStereoImage();
tests.Smoothing_AppliesGradualGainChange();

// WaveFormat and provider access
var format = tests.WaveFormat;
var provider = tests.TestSampleProvider;
var readCount = provider.Read(new float[1024], 0, 1024);
```