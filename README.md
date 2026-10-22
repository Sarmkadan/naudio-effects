# naudio-effects

DSP effects for NAudio (compressor, limiter, EQ, gate) as ISampleProvider.

> v0.1 in progress.

## Getting started

Install the package from NuGet:

```bash
dotnet add package NAudioEffects --version 0.1.0
```

The example below loads a WAV file, adds a feedback delay (300 ms, feedback 0.4, mix 0.3), feeds the result into a compressor, plays it with `WaveOutEvent`, and writes the processed audio to `output.wav` as 16-bit PCM. `WaveOutEvent` uses the Windows WinMM API, so playback requires Windows. Each chain is built from a fresh `AudioFileReader` because the effects are stateful and the stream can only be read once.

```csharp
using System.Threading;
using NAudio.Wave;
using NAudioEffects;

static class Program
{
    static CompressorSampleProvider BuildChain(ISampleProvider source) =>
        new CompressorSampleProvider(new DelaySampleProvider(source)
        {
            DelayMs = 300f,
            Feedback = 0.4f,
            Mix = 0.3f
        });

    static void Main()
    {
        // Write the processed audio to a 16-bit PCM file.
        using (var reader = new AudioFileReader("input.wav"))
        {
            WaveFileWriter.CreateWaveFile16("output.wav", BuildChain(reader));
        }

        // Play the processed audio.
        using (var reader = new AudioFileReader("input.wav"))
        using (var player = new WaveOutEvent())
        {
            player.Init(BuildChain(reader).ToWaveProvider());
            player.Play();
            while (player.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }
    }
}
```

### Parameters

`DelaySampleProvider` (constructor argument `maxDelayMs` sets the buffer size; the default is 2000 ms):

| Parameter | Unit | Default | Sensible range | Notes |
|---|---|---|---|---|
| `maxDelayMs` (constructor) | ms | 2000 | ≥ the largest `DelayMs` you will use | Sizes the delay line; must be finite and ≥ 0 |
| `DelayMs` | ms | 0 | 20–1000 | Must be between 0 and `maxDelayMs` |
| `Feedback` | unitless (0–0.95) | 0.35 | 0.2–0.6 | Share of the delayed signal fed back; above ~0.7 the echoes ring for a long time |
| `Mix` | unitless (0–1) | 0.5 | 0.1–0.5 | 0 = fully dry, 1 = fully wet |

`CompressorSampleProvider` (its setters do not validate values; the ranges below are guidance):

| Parameter | Unit | Default | Sensible range | Notes |
|---|---|---|---|---|
| `ThresholdDb` | dB (dBFS) | -20 | -30 to -10 | Level above which gain reduction starts |
| `Ratio` | n:1 | 4 | 2–8 | Values below 1 are treated as 1 |
| `AttackMs` | ms | 10 | 1–50 | How quickly the compressor reacts to a level increase |
| `ReleaseMs` | ms | 100 | 50–500 | How quickly gain returns after the level drops |
| `KneeDb` | dB | 6 | 0–12 | Width of the soft knee; 0 gives a hard knee |
| `MakeupGainDb` | dB | 0 | 0–12 | Gain applied after compression to restore loudness |

## Building a 3-band EQ

`EqualizerSampleProvider` runs one biquad filter per band. `EqualizerBuilder` creates the bands in the order you add them, so band 0 is the first `Add…` call. Gains must be between -24 and +24 dB, and each frequency must be above 0 and below half the sample rate.

The first example builds a 3-band EQ over a WAV file, plays it, and changes the peaking band's gain after three seconds. The shelf Q values are 0.707 (the usual Butterworth slope); the spec did not give one.

```csharp
using System.Threading;
using NAudio.Wave;
using NAudioEffects;

static class Program
{
    static void Main()
    {
        using var reader = new AudioFileReader("input.wav");

        var eq = new EqualizerBuilder()
            .AddLowShelf(100f, 3f, 0.707f)    // band 0: low shelf, +3 dB
            .AddPeak(1000f, -4f, 1.0f)        // band 1: peaking, -4 dB, Q 1.0
            .AddHighShelf(8000f, 2f, 0.707f)  // band 2: high shelf, +2 dB
            .Build(reader);

        using var player = new WaveOutEvent();
        player.Init(eq.ToWaveProvider());
        player.Play();

        Thread.Sleep(3000);

        // Change band 1 while audio plays. The new filter is built on the next block.
        // Pass the filter type and Q again: SetBandGain resets them to Peaking and 1.0 when omitted.
        eq.SetBandGain(1, -7f, EqualizerFilterType.Peaking, 1.0f);

        while (player.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(100);
        }
    }
}
```

`SetBandGain` does not lock, so call it from one thread at a time. The audio thread picks up the change on its next read, so a call that races with a block can take effect one block late.

The second example records from the default microphone and runs it through `ChorusSampleProvider`, then `EqualizerSampleProvider`. `WaveInEvent` defaults to 8 kHz, and `Build` rejects bands at or above half the sample rate, so the recording format is set to 44.1 kHz mono. `BufferedWaveProvider` delivers 16-bit PCM. `ToSampleProvider()` converts it to the 32-bit float format that the effects require.

Use headphones. Monitoring a microphone through speakers feeds the output back into the input.

```csharp
using System;
using NAudio.Wave;
using NAudioEffects;

class MicChain
{
    static void Main()
    {
        var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 16, 1)
        };

        var micBuffer = new BufferedWaveProvider(waveIn.WaveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };
        waveIn.DataAvailable += (_, e) => micBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

        ISampleProvider mic = micBuffer.ToSampleProvider();

        var chorus = new ChorusSampleProvider(mic)
        {
            RateHz = 0.5f,
            DepthMs = 2.0f,
            Mix = 0.2f,
            BaseDelayMs = 15.0f
        };

        var eq = new EqualizerBuilder()
            .AddLowShelf(100f, 3f, 0.707f)
            .AddPeak(1000f, -4f, 1.0f)
            .AddHighShelf(8000f, 2f, 0.707f)
            .Build(chorus);

        using var player = new WaveOutEvent();
        player.Init(eq.ToWaveProvider());

        waveIn.StartRecording();
        player.Play();

        Console.WriteLine("Monitoring the microphone. Press Enter to stop.");
        Console.ReadLine();

        waveIn.StopRecording();
        player.Stop();
        waveIn.Dispose();
    }
}
```

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

## LimiterSampleProvider
The LimiterSampleProvider class implements a limiter that prevents audio from exceeding a specified ceiling threshold with adjustable attack and release times. It can be used to prevent clipping and maintain consistent loudness.

**Usage example
```csharp
var limiter = new LimiterSampleProvider(new SampleProvider());
limiter.CeilingDb = -1.0f;
limiter.AttackMs = 5.0f;
limiter.ReleaseMs = 50.0f;
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