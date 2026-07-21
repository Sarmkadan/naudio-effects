# FlangerSampleProvider

The `FlangerSampleProvider` is a real-time audio effect that applies a flanging modulation to an input audio stream. Flanging is achieved by mixing a delayed, modulated copy of the signal with the original, creating a sweeping comb-filter effect. This implementation uses a circular buffer to manage the delay line and supports adjustable rate, depth, feedback, delay, and mix parameters.

## API

### `public float RateHz`
Gets or sets the modulation rate of the flanger effect in Hertz. This determines how quickly the delay time oscillates.
- **Range**: Typically between `0.01f` and `20.0f`. Values outside this range may produce unstable or inaudible results.
- **Default**: `0.1f`.

### `public float DepthMs`
Gets or sets the modulation depth of the flanger effect in milliseconds. This defines the maximum deviation of the delay time from the base `DelayMs` value.
- **Range**: Typically between `0.0f` and `10.0f`. Values beyond this range may cause artifacts or excessive phase cancellation.
- **Default**: `1.0f`.

### `public float Feedback`
Gets or sets the feedback amount, which determines how much of the output signal is fed back into the delay line.
- **Range**: Between `-1.0f` and `1.0f`. Negative values invert the phase of the feedback signal.
- **Default**: `0.0f`.
- **Edge Case**: Values near `±1.0f` may cause self-oscillation or excessive gain, leading to clipping or instability.

### `public float DelayMs`
Gets or sets the base delay time in milliseconds. This is the average delay applied to the input signal before modulation.
- **Range**: Typically between `0.1f` and `20.0f`. Very small values may reduce the flanging effect, while large values introduce pronounced comb filtering.
- **Default**: `2.0f`.

### `public float Mix`
Gets or sets the wet/dry mix ratio. A value of `0.0f` outputs only the dry (original) signal, while `1.0f` outputs only the wet (effected) signal.
- **Range**: Between `0.0f` and `1.0f`.
- **Default**: `0.5f`.

### `public FlangerSampleProvider(ISampleProvider source)`
Constructs a new `FlangerSampleProvider` instance.
- **Parameters**:
  - `source`: The input audio stream to process. Must not be `null`.
- **Throws**: `ArgumentNullException` if `source` is `null`.

### `public CircularBuffer`
Exposes the internal `CircularBuffer` used for the delay line. This is primarily for advanced use cases (e.g., manual buffer manipulation) and is not typically required for standard operation.
- **Note**: Direct modification of the buffer may disrupt the flanger effect or cause artifacts.

### `public void EnsureCapacity(int samples)`
Ensures the circular buffer has sufficient capacity to hold the specified number of samples.
- **Parameters**:
  - `samples`: The required capacity in samples.
- **Note**: This method is automatically called during processing if the buffer is undersized. Manual invocation is rarely needed.

### `public void Write(float value)`
Writes a single sample into the circular buffer.
- **Parameters**:
  - `value`: The sample value to write.
- **Note**: This method is used internally during processing and is not typically called manually.

### `public float Read(float delaySamples)`
Reads a single sample from the circular buffer at the specified delay offset.
- **Parameters**:
  - `delaySamples`: The delay in samples relative to the current write position.
- **Returns**: The sample value at the requested delay offset.
- **Note**: This method is used internally to retrieve delayed samples for modulation.

## Usage

### Example 1: Applying Flanger to an Audio File
