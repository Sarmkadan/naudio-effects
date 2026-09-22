using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NAudioEffects;

/// <summary>
/// Equalizer effect sample provider that applies multiple peaking filters across frequency bands.
/// </summary>
public class EqualizerSampleProvider : EffectSampleProviderBase
{
    private readonly BiQuadFilter[] _filters;
    private readonly float[] _gainsDb;
    private readonly float[] _frequencies;
    private readonly float[] _qValues;
    private readonly EqualizerFilterType[] _filterTypes;
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
        _frequencies = new float[bandCount];
        _qValues = new float[bandCount];
        _filterTypes = new EqualizerFilterType[bandCount];

        // Initialise gains to zero; filters will be created lazily on first processing pass.
        for (int i = 0; i < bandCount; i++)
        {
            _gainsDb[i] = 0.0f;
            _filters[i] = null!; // suppressed null warning – will be replaced before use
            _filterTypes[i] = EqualizerFilterType.Peaking;
            _qValues[i] = 1.0f;
            _frequencies[i] = GetBandFrequency(i); // Default to log spacing
        }
    }

    /// <summary>
    /// Sets the gain for a specific frequency band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="gainDb">The gain in decibels.</param>
    /// <param name="filterType">The type of filter to apply.</param>
    /// <param name="q">The Q factor (resonance) of the filter.</param>
    public void SetBandGain(int band, float gainDb, EqualizerFilterType filterType = EqualizerFilterType.Peaking, float q = 1.0f)
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
        _filterTypes[band] = filterType;
        _qValues[band] = q;
        // Defer rebuilding the filter until the next processing pass.
        _filtersDirty = true;
    }

    /// <summary>
    /// Sets the center frequency for a specific band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="frequency">The center frequency in Hz.</param>
    public void SetBandFrequency(int band, float frequency)
    {
        if (band < 0 || band >= _bandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(band), $"Band must be between 0 and {_bandCount - 1}");
        }

        _frequencies[band] = frequency;
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
            float frequency = _frequencies[i];
            float gainDb = _gainsDb[i];
            EqualizerFilterType filterType = _filterTypes[i];
            float q = _qValues[i];

            // Validate frequency is in the valid range (0, sampleRate/2)
            if (frequency <= 0f || frequency >= WaveFormat.SampleRate / 2f)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency),
                    $"Frequency {frequency}Hz is not in the valid range (0, {WaveFormat.SampleRate/2}Hz) for band {i}.");
            }

            // Validate Q factor
            if (q <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(q), $"Q factor must be greater than zero. Actual value: {q}");
            }

            _filters[i] = filterType switch
            {
                EqualizerFilterType.LowShelf => BiQuadFilter.LowShelf(WaveFormat.SampleRate, frequency, q, gainDb),
                EqualizerFilterType.HighShelf => BiQuadFilter.HighShelf(WaveFormat.SampleRate, frequency, q, gainDb),
                _ => BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, frequency, q, gainDb)
            };
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

/// <summary>
/// Specifies the type of filter to apply in the equalizer.
/// </summary>
public enum EqualizerFilterType
{
    /// <summary>
    /// Peaking EQ filter.
    /// </summary>
    Peaking,
    /// <summary>
    /// Low shelf filter.
    /// </summary>
    LowShelf,
    /// <summary>
    /// High shelf filter.
    /// </summary>
    HighShelf
}

/// <summary>
/// Fluent builder for constructing <see cref="EqualizerSampleProvider"/> instances.
/// </summary>
public class EqualizerBuilder
{
    private readonly List<(float Frequency, float GainDb, float Q, EqualizerFilterType Type)> _bands = new();

    /// <summary>
    /// Adds a peaking band to the equalizer.
    /// </summary>
    public EqualizerBuilder AddBand(float frequency, float gainDb, float q)
    {
        _bands.Add((frequency, gainDb, q, EqualizerFilterType.Peaking));
        return this;
    }

    /// <summary>
    /// Adds a low shelf band to the equalizer.
    /// </summary>
    public EqualizerBuilder AddLowShelf(float frequency, float gainDb, float q)
    {
        _bands.Add((frequency, gainDb, q, EqualizerFilterType.LowShelf));
        return this;
    }

    /// <summary>
    /// Adds a high shelf band to the equalizer.
    /// </summary>
    public EqualizerBuilder AddHighShelf(float frequency, float gainDb, float q)
    {
        _bands.Add((frequency, gainDb, q, EqualizerFilterType.HighShelf));
        return this;
    }

    /// <summary>
    /// Adds a peak band to the equalizer.
    /// </summary>
    public EqualizerBuilder AddPeak(float frequency, float gainDb, float q)
    {
        _bands.Add((frequency, gainDb, q, EqualizerFilterType.Peaking));
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="EqualizerSampleProvider"/>.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <returns>A configured <see cref="EqualizerSampleProvider"/>.</returns>
    public EqualizerSampleProvider Build(ISampleProvider source)
    {
        if (_bands.Count == 0)
        {
            throw new InvalidOperationException("At least one band must be added before building.");
        }

        // Validate bands at build time
        foreach (var (freq, gain, q, type) in _bands)
        {
            if (freq <= 0f || freq >= source.WaveFormat.SampleRate / 2f)
            {
                throw new ArgumentOutOfRangeException(nameof(freq),
                    $"Frequency {freq}Hz is not in the valid range (0, {source.WaveFormat.SampleRate / 2}Hz).");
            }
            if (gain < -24f || gain > 24f)
            {
                throw new ArgumentOutOfRangeException(nameof(gain),
                    $"Gain must be between -24 dB and +24 dB. Actual value: {gain} dB");
            }
            if (q <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(q), $"Q factor must be greater than zero. Actual value: {q}");
            }
        }

        var eq = new EqualizerSampleProvider(source, _bands.Count);
        for (int i = 0; i < _bands.Count; i++)
        {
            var (freq, gain, q, type) = _bands[i];
            eq.SetBandFrequency(i, freq);
            eq.SetBandGain(i, gain, type, q);
        }

        // Clear bands to allow the builder to be reused
        _bands.Clear();
        return eq;
    }
}
