using NAudio.Dsp;
using NAudio.Wave;

namespace NAudioEffects;

/// <summary>
/// Equalizer effect sample provider that applies multiple peaking filters across frequency bands.
/// </summary>
public class EqualizerSampleProvider : EffectSampleProviderBase
{
    private readonly BiQuadFilter[] _filters;
    private readonly float[] _gainsDb;
    private readonly int _bandCount;

    /// <summary>
    /// Gets the number of frequency bands in the equalizer.
    /// </summary>
    public int BandCount => _bandCount;

    /// <summary>
    /// Initializes a new instance of the EqualizerSampleProvider.
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

        // Create logarithmically spaced bands from 60Hz to 12kHz
        for (int i = 0; i < bandCount; i++)
        {
            float frequency = GetBandFrequency(i);
            _filters[i] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, frequency, 1.0f, 0.0f);
            _gainsDb[i] = 0.0f; // Default gain
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

        _gainsDb[band] = gainDb;
        UpdateFilterForBand(band);
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

        // Logarithmically spaced bands from 60Hz to 12kHz
        float minFreq = 60.0f;
        float maxFreq = 12000.0f;
        float logMin = MathF.Log(minFreq);
        float logMax = MathF.Log(maxFreq);
        float logFreq = logMin + (logMax - logMin) * band / (_bandCount - 1);
        return MathF.Exp(logFreq);
    }

    /// <summary>
    /// Updates the filter coefficients for a specific band based on current gain.
    /// </summary>
    private void UpdateFilterForBand(int band)
    {
        float frequency = GetBandFrequency(band);
        float gainDb = _gainsDb[band];
        _filters[band] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, frequency, 1.0f, gainDb);
    }

    /// <summary>
    /// Processes a block of samples, applying the equalizer effect.
    /// </summary>
    protected override void ProcessBlock(float[] buffer, int offset, int count)
    {
        for (int n = 0; n < count; n++)
        {
            float sample = buffer[offset + n];

            // Apply each band's filter to the sample
            for (int band = 0; band < _bandCount; band++)
            {
                sample = _filters[band].Transform(sample);
            }

            buffer[offset + n] = sample;
        }
    }
}