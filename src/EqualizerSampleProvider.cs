using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NAudioEffects;

/// <summary>
/// Equalizer effect sample provider that applies multiple peaking filters across frequency bands.
/// </summary>
public class EqualizerSampleProvider : EffectSampleProviderBase
{
    private readonly BiQuadFilter[] _filters;
    private readonly EqualizerBand[] _bands;

    // When any band changes we defer rebuilding the BiQuadFilter objects until the next Read.
    private bool _filtersDirty = true;

    /// <summary>
    /// Gets the number of frequency bands in the equalizer.
    /// </summary>
    public int BandCount => _bands.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualizerSampleProvider"/> with logarithmically spaced
    /// peaking bands at 0 dB.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="bandCount">Number of frequency bands (default: 5).</param>
    /// <exception cref="ArgumentOutOfRangeException">The band count is less than 1, or a default band is invalid for the source sample rate.</exception>
    public EqualizerSampleProvider(ISampleProvider source, int bandCount = 5)
        : this(source, CreateDefaultBands(bandCount))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualizerSampleProvider"/> with the given bands.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    /// <param name="bands">The bands to apply. Must be non-empty and every band must be valid for the source sample rate.</param>
    /// <exception cref="ArgumentNullException">The source or the band list is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The band list is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A band has an invalid frequency, gain or Q.</exception>
    public EqualizerSampleProvider(ISampleProvider source, IReadOnlyList<EqualizerBand> bands)
        : base(source)
    {
        ValidateBands(bands, source.WaveFormat.SampleRate);

        _bands = bands.ToArray();
        _filters = new BiQuadFilter[_bands.Length];
    }

    /// <summary>
    /// Replaces the settings of one band.
    /// </summary>
    /// <remarks>
    /// The new settings are validated before anything is changed. A rejected update throws and leaves the
    /// previous settings and filter coefficients in effect.
    /// </remarks>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="settings">The new settings for the band.</param>
    /// <exception cref="ArgumentOutOfRangeException">The band index or one of the settings is invalid.</exception>
    public void UpdateBand(int band, EqualizerBand settings)
    {
        CheckBandIndex(band);
        settings.Validate(WaveFormat.SampleRate);

        _bands[band] = settings;
        // Defer rebuilding the filter until the next processing pass.
        _filtersDirty = true;
    }

    /// <summary>
    /// Gets the current settings for a specific frequency band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <returns>The settings of the band.</returns>
    public EqualizerBand GetBand(int band)
    {
        CheckBandIndex(band);
        return _bands[band];
    }

    /// <summary>
    /// Sets the gain for a specific frequency band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="gainDb">The gain in decibels.</param>
    /// <param name="filterType">The type of filter to apply.</param>
    /// <param name="q">The Q factor (resonance) of the filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">The band index, gain or Q is invalid. The band is left unchanged.</exception>
    public void SetBandGain(int band, float gainDb, EqualizerFilterType filterType = EqualizerFilterType.Peaking, float q = 1.0f)
    {
        CheckBandIndex(band);
        UpdateBand(band, new EqualizerBand(_bands[band].Frequency, gainDb, q, filterType));
    }

    /// <summary>
    /// Sets the center frequency for a specific band.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <param name="frequency">The center frequency in Hz.</param>
    /// <exception cref="ArgumentOutOfRangeException">The band index or frequency is invalid. The band is left unchanged.</exception>
    public void SetBandFrequency(int band, float frequency)
    {
        CheckBandIndex(band);
        UpdateBand(band, _bands[band] with { Frequency = frequency });
    }

    /// <summary>
    /// Gets the default center frequency for a specific band, as used by the band-count constructor.
    /// </summary>
    /// <param name="band">The band index (0-based).</param>
    /// <returns>The default center frequency in Hz.</returns>
    public float GetBandFrequency(int band)
    {
        CheckBandIndex(band);
        return DefaultBandFrequency(band, BandCount);
    }

    private void CheckBandIndex(int band)
    {
        if (band < 0 || band >= _bands.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(band), $"Band must be between 0 and {_bands.Length - 1}");
        }
    }

    private static EqualizerBand[] CreateDefaultBands(int bandCount)
    {
        if (bandCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bandCount), "Band count must be at least 1");
        }

        var bands = new EqualizerBand[bandCount];
        for (int i = 0; i < bandCount; i++)
        {
            bands[i] = new EqualizerBand(DefaultBandFrequency(i, bandCount), 0.0f, 1.0f, EqualizerFilterType.Peaking);
        }
        return bands;
    }

    private static float DefaultBandFrequency(int band, int bandCount)
    {
        // Handle single band case to avoid division by zero
        if (bandCount == 1)
        {
            return 60.0f; // Default frequency for single band
        }

        // Logarithmically spaced bands from 60Hz to 12kHz
        const float minFreq = 60.0f;
        const float maxFreq = 12000.0f;
        float logMin = MathF.Log(minFreq);
        float logMax = MathF.Log(maxFreq);
        float logFreq = logMin + (logMax - logMin) * band / (bandCount - 1);
        return MathF.Exp(logFreq);
    }

    private static void ValidateBands(IReadOnlyList<EqualizerBand> bands, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(bands);

        if (bands.Count == 0)
        {
            throw new ArgumentException("At least one band is required.", nameof(bands));
        }

        foreach (EqualizerBand band in bands)
        {
            band.Validate(sampleRate);
        }
    }

    /// <summary>
    /// Rebuilds all <see cref="BiQuadFilter"/> instances based on the current band settings.
    /// Called lazily before processing a block when <see cref="_filtersDirty"/> is true.
    /// </summary>
    private void UpdateAllFilters()
    {
        UpdateAllFilters(CancellationToken.None);
    }

    /// <summary>
    /// Rebuilds all <see cref="BiQuadFilter"/> instances based on the current band settings, checking for cancellation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to check.</param>
    private void UpdateAllFilters(CancellationToken cancellationToken)
    {
        int sampleRate = WaveFormat.SampleRate;

        // Settings were validated when they were stored, so rebuilding cannot fail on bad input.
        for (int i = 0; i < _bands.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EqualizerBand band = _bands[i];

            _filters[i] = band.FilterType switch
            {
                EqualizerFilterType.LowShelf => BiQuadFilter.LowShelf(sampleRate, band.Frequency, band.Q, band.GainDb),
                EqualizerFilterType.HighShelf => BiQuadFilter.HighShelf(sampleRate, band.Frequency, band.Q, band.GainDb),
                _ => BiQuadFilter.PeakingEQ(sampleRate, band.Frequency, band.Q, band.GainDb)
            };
        }
    }

    /// <summary>
    /// Processes a block of samples, applying the equalizer effect.
    /// </summary>
    protected override void ProcessBlock(float[] buffer, int offset, int count)
    {
        // Rebuild filters only when a band has changed.
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

            for (int band = 0; band < _bands.Length; band++)
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

        // The provider validates every band against the source sample rate.
        var bands = _bands
            .Select(b => new EqualizerBand(b.Frequency, b.GainDb, b.Q, b.Type))
            .ToList();
        var eq = new EqualizerSampleProvider(source, bands);

        // Clear bands to allow the builder to be reused
        _bands.Clear();
        return eq;
    }
}
