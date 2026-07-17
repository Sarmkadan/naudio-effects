# ChorusSampleProvider

The `ChorusSampleProvider` class implements a stereo chorus effect by modulating a delayed signal with a low‑frequency oscillator. It exposes configurable parameters for the LFO rate, depth, wet/dry mix, and base delay, and provides a circular buffer for storing and retrieving audio samples during processing.

## API

### RateHz
```csharp
public float RateHz { get; set; }
```
Gets or sets the frequency of the low‑frequency oscillator in hertz. Typical values range from 0.1 Hz to 10 Hz. Setting a value less than or equal to zero throws an `ArgumentOutOfRangeException`.

### DepthMs
```csharp
public float DepthMs { get; set; }
```
Gets or sets the maximum delay variation introduced by the chorus, expressed in milliseconds. Values must be non‑negative; a negative value throws an `ArgumentOutOfRangeException`.

### Mix
```csharp
public float Mix { get; set; }
```
Gets or sets the wet/dry mix ratio, where 0 represents 100 % dry (unprocessed) signal and 1 represents 100 % wet (fully processed) signal. Values outside the range `[0, 1]` cause an `ArgumentOutOfRangeException`.

### BaseDelayMs
```csharp
public float BaseDelayMs { get; set; }
```
Gets or sets the base delay time in milliseconds around which the LFO modulates. Must be non‑negative; setting a negative value throws an `ArgumentOutOfRangeException`.

### ChorusSampleProvider (constructor)
```csharp
public ChorusSampleProvider()
```
Initializes a new instance of the `ChorusSampleProvider` with default parameter values (RateHz = 1 Hz, DepthMs = 20 ms, Mix = 0.5, BaseDelayMs = 10 ms) and creates an internal circular buffer ready for sample processing.

### CircularBuffer
```csharp
public CircularBuffer CircularBuffer { get; }
```
Provides read‑only access to the internal circular buffer used to store delayed samples. The buffer’s capacity and current fill level can be inspected through this property, but the buffer itself should not be modified directly.

### EnsureCapacity
```csharp
public void EnsureCapacity(int minimumCapacity)
```
Ensures that the internal circular buffer can hold at least `minimumCapacity` samples without resizing. If `minimumCapacity` is less than zero, an `ArgumentOutOfRangeException` is thrown. If the current capacity is already sufficient, the method returns immediately; otherwise, the buffer is resized preserving existing data.

### Write
```csharp
public void Write(float sample)
```
Writes a single mono audio sample into the circular buffer at the current write position. The sample is stored for later retrieval by the chorus processing algorithm. If the buffer is full and cannot accommodate the new sample without overwriting unread data, an `InvalidOperationException` is thrown.

### Read
```csharp
public float Read()
```
Reads and returns the next processed sample from the chorus effect. The method applies the LFO‑modulated delay, mixes the delayed signal with the dry input according to the `Mix` property, and advances the read position. If no samples are available to read (i.e., the buffer is empty), an `InvalidOperationException` is thrown.

## Usage

### Basic processing loop
```csharp
var chorus = new ChorusSampleProvider();
chorus.RateHz = 1.5f;
chorus.DepthMs = 30f;
chorus.Mix = 0.4f;
chorus.BaseDelayMs = 12f;

// Ensure the buffer can hold at least 1024 samples.
chorus.EnsureCapacity(1024);

// Simulate an audio source providing samples.
float[] input = GetInputSamples(); // Assume this returns a float[].
foreach (float s in input)
{
    chorus.Write(s);          // Feed the dry sample into the buffer.
    float processed = chorus.Read(); // Obtain the chorused output.
    OutputSample(processed); // Send to playback or further processing.
}
```

### Integration with NAudio pipeline
```csharp
using NAudio.Wave;

// Assume `reader` is an AudioFileReader or similar ISampleProvider.
var chorus = new ChorusSampleProvider { RateHz = 2f, DepthMs = 25f, Mix = 0.3f, BaseDelayMs = 15f };
var chorusProvider = new SampleToWaveProvider(chorus); // Adapter if needed.

using (var waveOut = new WaveOutEvent())
{
    waveOut.Init(new SampleProviderToWaveProvider(chorus)); // Connect chorus as source.
    waveOut.Play();
    while (waveOut.PlaybackState == PlaybackState.Playing)
    {
        // Feed samples from the source into the chorus.
        float sample = reader.ReadNextSample(); // Hypothetical helper.
        chorus.Write(sample);
        // The chorus provider internally supplies processed samples to the waveOut.
    }
}
```

## Notes
- Setting any of the configurable properties (`RateHz`, `DepthMs`, `Mix`, `BaseDelayMs`) to an invalid value will raise an `ArgumentOutOfRangeException` before the change takes effect.
- The `Write` and `Read` methods are not thread‑safe; concurrent calls from multiple threads may corrupt the buffer state or produce undefined results. If multi‑threaded access is required, external synchronization must be applied.
- The internal buffer size grows only when `EnsureCapacity` is invoked with a larger value than the current capacity; it never shrinks automatically. Calling `EnsureCapacity` with a value smaller than the current capacity has no effect.
- When the buffer is empty, `Read` throws an `InvalidOperationException`. Likewise, attempting to `Write` when the buffer is full throws an `InvalidOperationException`. Properly balancing write and read rates is essential to avoid these exceptions.
- The `CircularBuffer` property exposes the underlying buffer for diagnostic purposes; modifying the buffer directly bypasses the chorus algorithm and may lead to audio artifacts. It is recommended to treat this property as read‑only in production code.
