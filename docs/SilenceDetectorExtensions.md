# SilenceDetectorExtensions

Provides extension methods for analyzing silence regions detected by a `SilenceDetector` instance. These methods offer convenient ways to query silence statistics, durations, and regions without directly accessing the detector's internal state.

## API

### `HasSilence(SilenceDetector detector)`
Determines whether any silence regions were detected.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to check.
- **Return value**
  - `true` if at least one silence region exists; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.

---

### `GetTotalSilenceDurationMilliseconds(SilenceDetector detector)`
Calculates the total duration of all silence regions in milliseconds.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - The total silence duration in milliseconds.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.

---

### `GetTotalSilenceDuration(SilenceDetector detector)`
Calculates the total duration of all silence regions as a `TimeSpan`.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - The total silence duration as a `TimeSpan`.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.

---
### `GetAverageSilenceDuration(SilenceDetector detector)`
Computes the average duration of silence regions.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - The average silence duration as a `TimeSpan`.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.
  - Throws `InvalidOperationException` if no silence regions exist.

---
### `GetLongestSilence(SilenceDetector detector)`
Retrieves the longest silence region detected.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - A `SilenceRegion` representing the longest silence period.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.
  - Throws `InvalidOperationException` if no silence regions exist.

---
### `GetShortestSilence(SilenceDetector detector)`
Retrieves the shortest silence region detected.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - A `SilenceRegion` representing the shortest silence period.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.
  - Throws `InvalidOperationException` if no silence regions exist.

---
### `GetSilenceRegionCount(SilenceDetector detector)`
Returns the number of silence regions detected.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to analyze.
- **Return value**
  - The count of silence regions.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.

---
### `IsEntirelySilence(SilenceDetector detector)`
Determines whether the entire analyzed audio consists of silence.

- **Parameters**
  - `detector`: The `SilenceDetector` instance to check.
- **Return value**
  - `true` if the entire audio is silence; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `detector` is `null`.

## Usage

### Example 1: Basic Silence Analysis
```csharp
using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;

var silenceDetector = new SilenceDetector(
    threshold: -40.0,
    minimumSilenceDuration: TimeSpan.FromMilliseconds(500));

// Process audio and detect silence regions...
silenceDetector.ProcessSamples(someAudioSamples);

// Query silence statistics
if (silenceDetector.HasSilence())
{
    Console.WriteLine($"Total silence duration: {silenceDetector.GetTotalSilenceDuration():hh\\:mm\\:ss}");
    Console.WriteLine($"Average silence duration: {silenceDetector.GetAverageSilenceDuration():hh\\:mm\\:ss}");
    Console.WriteLine($"Silence regions detected: {silenceDetector.GetSilenceRegionCount()}");

    var longestSilence = silenceDetector.GetLongestSilence();
    Console.WriteLine($"Longest silence: {longestSilence.Duration:hh\\:mm\\:ss} at {longestSilence.StartPosition}");
}
```

### Example 2: Checking Entire Silence
```csharp
var silenceDetector = new SilenceDetector(
    threshold: -30.0,
    minimumSilenceDuration: TimeSpan.FromSeconds(1));

silenceDetector.ProcessSamples(entireAudioBuffer);

if (silenceDetector.IsEntirelySilence())
{
    Console.WriteLine("The entire audio is silence.");
}
else
{
    Console.WriteLine("The audio contains non-silent segments.");
}
```

## Notes

- All methods are thread-safe for concurrent calls on the same `SilenceDetector` instance, assuming the instance itself is not modified during concurrent access.
- Methods that return `SilenceRegion` return a struct with immutable properties (`StartPosition` and `Duration`), ensuring safe concurrent reads.
- Methods that require silence regions (e.g., `GetAverageSilenceDuration`) will throw `InvalidOperationException` if no regions exist, rather than returning a default value.
- The `SilenceDetector` instance must be fully processed (i.e., all samples fed via `ProcessSamples`) before querying silence statistics. Partial processing may yield incomplete or incorrect results.
