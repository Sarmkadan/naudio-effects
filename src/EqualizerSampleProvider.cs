using NAudio.Dsp;
using NAudio.Wave;
using System.Threading;

namespace NAudioEffects;

/// <summary>
/// Equalizer effect sample provider that applies multiple peaking filters across frequency bands.
/// </summary>
public class EqualizerSampleProvider : EffectSampleProviderBase
{
    private readonly BiQuadFilter[] _filters;
    private readonly float[] _gainsDb;
    private readonly int _bandCount;

    // When any band gain changes we defer rebuilding the BiQuadFilter objects until the next Read.
    private bool _filtersDirty = true;

    /// <summary>
    /// Gets the number of frequency bands in the equalizer.
    /// </summary>
    public int BandCount => _bandCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualizerSampleProvider"/>.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="bandCount">Number of frequency bands (default: 5).</param>
    public EqualizerSampleProvider(ISampleProvider source, int bandCount = 5)
        : base(source)
    {
        if (bandCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bandCount), "Band count must be at least 1");
        }

        _bandCount = bandCount;
        _gainsDb = new float[bandCount];
        _filters = new BiQuadFilter[bandCount];

        // Initialise gains to zero; filters will be created lazily on first processing pass.
        for (int i = 0; i < bandCount; i++)
        {
            _gainsDb[i] = 0.0f;
            _filters[i] = null!; // suppressed null warning – will be replaced before use
        }
    }

    /// <summary>
    /// Sets the gain for a specific frequency band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="gainDb">The gain in decibels.</param>
    public void SetBandGain(int band, float gainDb)
    {
        if (band < 0 || band >= _bandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(band), $"Band must be between 0 and {_bandCount - 1}");
        }

        // Validate gain is within a sane range (-24 dB to +24 dB)
        if (gainDb < -24f || gainDb > 24f)
        {
            throw new ArgumentOutOfRangeException(nameof(gainDb), $"Gain must be between -24 dB and +24 dB. Actual value: {gainDb} dB");
        }

        _gainsDb[band] = gainDb;
        // Defer rebuilding the filter until the next processing pass.
        _filtersDirty = true;
    }

    /// <summary>
    /// Gets the center frequency for a specific band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <returns>The center frequency in Hz.</returns>
    public float GetBandFrequency(int band)
    {
        if (band < 0 || band >= _bandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(band), $"Band must be between 0 and {_bandCount - 1}");
        }

        // Handle single band case to avoid division by zero
        if (_bandCount == 1)
        {
            return 60.0f; // Default frequency for single band
        }

        // Logarithmically spaced bands from 60Hz to 12kHz
        const float minFreq = 60.0f;
        const float maxFreq = 12000.0f;
        float logMin = MathF.Log(minFreq);
        float logMax = MathF.Log(maxFreq);
        float logFreq = logMin + (logMax - logMin) * band / (_bandCount - 1);
        return MathF.Exp(logFreq);
    }

    /// <summary>
    /// Rebuilds all <see cref="BiQuadFilter"/> instances based on the current gains.
    /// Called lazily before processing a block when <see cref="_filtersDirty"/> is true.
    /// </summary>
    private void UpdateAllFilters()
    {
        UpdateAllFilters(CancellationToken.None);
    }

    /// <summary>
    /// Rebuilds all <see cref="BiQuadFilter"/> instances based on the current gains, checking for cancellation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to check.</param>
    private void UpdateAllFilters(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _bandCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float frequency = GetBandFrequency(i);
            float gainDb = _gainsDb[i];

            // Validate frequency is in the valid range (0, sampleRate/2)
            if (frequency <= 0f || frequency >= WaveFormat.SampleRate / 2f)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency),
                    $"Frequency {frequency}Hz is not in the valid range (0, {WaveFormat.SampleRate/2}Hz) for band {i}.");
            }

            // Validate Q factor (fixed at 1.0f, but validate for completeness)
            const float q = 1.0f;
            if (q <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(q), $"Q factor must be greater than zero. Actual value: {q}");
            }

            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, frequency, q, gainDb);
        }
    }

    /// <summary>
    /// Processes a block of samples, applying the equalizer effect.
    /// </summary>
    protected override void ProcessBlock(float[] buffer, int offset, int count)
    {
        // Rebuild filters only when a gain has changed.
        if (_filtersDirty)
        {
            UpdateAllFilters();
            _filtersDirty = false;
        }

        // Cache the filter array locally to avoid repeated field accesses inside the inner loop.
        BiQuadFilter[] filters = _filters;

        // Apply each filter in sequence to every sample.
        // The loop works on interleaved audio (multiple channels) because the effect is
        // channel‑agnostic – each sample is processed independently.
        for (int n = 0; n < count; n++)
        {
            float sample = buffer[offset + n];

            for (int band = 0; band < _bandCount; band++)
            {
                sample = filters[band].Transform(sample);
            }

            buffer[offset + n] = sample;
        }
    }

    /// <summary>
    /// Reads a block of samples, supporting cancellation.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset into the buffer.</param>
    /// <param name="count">The number of samples to read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of samples read.</returns>
    public int Read(float[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        const int blockSize = 256;
        int processed = 0;
        while (processed < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int blockSizeToProcess = Math.Min(blockSize, count - processed);
            int samplesRead = _source.Read(buffer, offset + processed, blockSizeToProcess);
            if (samplesRead == 0)
                break;

            if (!Bypass && samplesRead > 0)
            {
                // Update filters if needed, with cancellation
                if (_filtersDirty)
                {
                    UpdateAllFilters(cancellationToken);
                    _filtersDirty = false;
                }
                ProcessBlock(buffer, offset + processed, samplesRead);
            }
            processed += samplesRead;
        }
        return processed;
    }

    /// <inheritdoc />
    public override int Read(float[] buffer, int offset, int count)
    {
        return Read(buffer, offset, count, CancellationToken.None);
    }
}
