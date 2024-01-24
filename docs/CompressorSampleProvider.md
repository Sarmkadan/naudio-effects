# CompressorSampleProvider

`CompressorSampleProvider` is a digital signal processing component for the NAudio framework that provides dynamic range compression to audio streams, reducing the volume of loud sounds or amplifying quiet sounds by attenuating audio signals that exceed a specified threshold to achieve a consistent output level.

## API

### `CompressorSampleProvider`
Initializes a new instance of the `CompressorSampleProvider` class.

### `ThresholdDb`
Gets or sets the decibel level at which compression begins. Signal levels above this threshold are attenuated.

### `Ratio`
Gets or sets the compression ratio applied to signals exceeding the `ThresholdDb`. For example, a ratio of 4.0 indicates that for every 4 dB by which the input signal exceeds the threshold, the output signal increases by only 1 dB.

### `AttackMs`
Gets or sets the attack time in milliseconds, which defines how quickly the compressor reaches full gain reduction once the input signal exceeds the threshold.

### `ReleaseMs`
Gets or sets the release time in milliseconds, which defines how quickly the compressor returns to unity gain once the input signal falls below the threshold.

### `KneeDb`
Gets or sets the width of the compression knee in decibels, controlling the smoothness of the transition into the compression region around the `ThresholdDb`.

### `MakeupGainDb`
Gets or sets the amount of gain in decibels applied to the output signal after compression to compensate for the overall level reduction.

### `CurrentGainReductionDb`
Gets the current amount of gain reduction being applied by the compressor in decibels. This is a read-only property.

## Usage

### Basic Configuration
```csharp
var reader = new AudioFileReader("audio.wav");
var compressor = new CompressorSampleProvider(reader)
{
    ThresholdDb = -20.0f,
    Ratio = 4.0f,
    AttackMs = 10.0f,
    ReleaseMs = 100.0f,
    KneeDb = 3.0f,
    MakeupGainDb = 2.0f
};
```

### Updating Parameters Dynamically
```csharp
// Adjusting makeup gain based on detected signal level
if (compressor.CurrentGainReductionDb > 10.0f)
{
    compressor.MakeupGainDb += 1.0f;
}
```

## Notes

### Thread Safety
The `CompressorSampleProvider` class is not thread-safe. Modifying properties, such as `ThresholdDb` or `Ratio`, while the compressor is actively processing audio data on a different thread may result in unexpected behavior or audio artifacts. Synchronization should be managed externally if dynamic parameter updates are required.

### Edge Cases
- **Extreme Values:** Setting `AttackMs` or `ReleaseMs` to values near zero may introduce significant harmonic distortion or audible clicking sounds due to instantaneous gain changes.
- **Ratio:** Very high compression ratios (e.g., above 20:1) will act as a hard limiter.
- **Makeup Gain:** Excessive `MakeupGainDb` can lead to digital clipping if the resulting output level exceeds 0 dBFS.
