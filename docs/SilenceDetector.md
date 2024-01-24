# SilenceDetector

The `SilenceDetector` class is designed for identifying and tracking periods of silence within an audio stream. It analyzes incoming audio data against a defined amplitude threshold to detect segments where the signal is deemed silent. This component is useful for applications such as voice activity detection, automatic audio trimming, and event logging based on signal level.

## API

### SilenceDetector()
Initializes a new instance of the `SilenceDetector` class.

### void Process(float[] buffer, int offset, int count)
Processes a segment of audio data to detect silence.
- `buffer`: The array containing the audio samples.
- `offset`: The zero-based offset in the buffer where audio data begins.
- `count`: The number of samples to process.

### void Complete()
Finalizes the audio processing and signals that no further audio data will be provided. This method ensures any pending silence detection state is closed and the final silence region is calculated.

### TimeSpan Start
Gets the starting timestamp of the detected silence region.

### TimeSpan End
Gets the ending timestamp of the detected silence region.

### SilenceRegion SilenceRegion
Gets the details of the detected silence region.

## Usage

### Basic Usage
```csharp
var detector = new SilenceDetector();
// Assuming audioData is a float array of PCM samples
detector.Process(audioData, 0, audioData.Length);
detector.Complete();
Console.WriteLine($"Silence from {detector.Start} to {detector.End}");
```

### Integration in a Stream
```csharp
var detector = new SilenceDetector();
while (reader.Position < reader.Length)
{
    int read = reader.Read(buffer, 0, buffer.Length);
    detector.Process(buffer, 0, read);
}
detector.Complete();
// Access the detected region
var region = detector.SilenceRegion;
```

## Notes

- **Edge Cases**: The `SilenceDetector` behavior is dependent on the signal amplitude. Very low-level background noise may interfere with detection if the threshold is not appropriately configured. If `Complete` is called without sufficient audio data, the resulting `SilenceRegion` may represent an empty interval.
- **Thread Safety**: This class is not thread-safe. Multiple threads should not call `Process` or `Complete` on the same instance simultaneously. It is designed for use in a single-threaded audio processing pipeline.
