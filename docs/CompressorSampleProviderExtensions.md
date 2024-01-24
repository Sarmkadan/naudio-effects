# CompressorSampleProviderExtensions
The `CompressorSampleProviderExtensions` class provides a set of extension methods for the `CompressorSampleProvider` class, allowing for the configuration and manipulation of compressor effects in audio processing. These methods enable the creation, customization, and querying of compressor instances, facilitating the implementation of dynamic compression algorithms in audio applications.

## API
* `WithThreshold`: Configures the compressor threshold. Returns a `CompressorSampleProvider` instance with the specified threshold.
* `WithRatio`: Configures the compressor ratio. Returns a `CompressorSampleProvider` instance with the specified ratio.
* `WithEnvelopeSettings`: Configures the compressor envelope settings. Returns a `CompressorSampleProvider` instance with the specified envelope settings.
* `WithKnee`: Configures the compressor knee. Returns a `CompressorSampleProvider` instance with the specified knee.
* `WithMakeupGain`: Configures the compressor makeup gain. Returns a `CompressorSampleProvider` instance with the specified makeup gain.
* `WithEnabled`: Enables or disables the compressor. Returns a `CompressorSampleProvider` instance with the specified enabled state.
* `CreateCompressor`: Creates a new compressor instance. Returns a `CompressorSampleProvider` instance.
* `Reset`: Resets the compressor to its default state. Returns a `CompressorSampleProvider` instance.
* `GetGainReductionFactor`: Retrieves the current gain reduction factor. Returns a `float` value representing the gain reduction factor.
* `IsCompressing`: Checks if the compressor is currently compressing. Returns a `bool` value indicating whether compression is active.
* `Configure`: Configures the compressor with the specified settings. Returns a `CompressorSampleProvider` instance with the applied settings.

## Usage
```csharp
// Example 1: Creating a compressor with custom settings
var compressor = CompressorSampleProvider.CreateCompressor()
    .WithThreshold(-20.0f)
    .WithRatio(4.0f)
    .WithMakeupGain(6.0f);

// Example 2: Enabling and disabling compression
var compressor2 = CompressorSampleProvider.CreateCompressor()
    .WithEnabled(true);
compressor2 = compressor2.WithEnabled(false);
```

## Notes
When using the `CompressorSampleProviderExtensions` class, note that the `CreateCompressor` method creates a new instance, while the other methods modify and return the existing instance. The `Reset` method resets the compressor to its default state, which may not be suitable for all use cases. Additionally, the `GetGainReductionFactor` and `IsCompressing` methods provide insight into the compressor's current state, but may not be thread-safe if accessed concurrently from multiple threads. It is recommended to synchronize access to these methods to ensure accurate results.
