using System;

namespace NAudioEffects;

/// <summary>
/// Settings for one band of an <see cref="EqualizerSampleProvider"/>.
/// </summary>
/// <param name="Frequency">The center frequency in Hz. Must satisfy 0 &lt; Frequency &lt; sampleRate / 2.</param>
/// <param name="GainDb">The gain in decibels. Must satisfy |GainDb| &lt;= 24.</param>
/// <param name="Q">The Q factor (resonance). Must be greater than zero.</param>
/// <param name="FilterType">The type of filter applied to this band.</param>
public readonly record struct EqualizerBand(
    float Frequency,
    float GainDb,
    float Q = 1.0f,
    EqualizerFilterType FilterType = EqualizerFilterType.Peaking)
{
    /// <summary>
    /// The largest absolute gain accepted for a band, in decibels.
    /// </summary>
    public const float MaxAbsGainDb = 24f;

    /// <summary>
    /// Validates this band against the sample rate of the audio it will process.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <exception cref="ArgumentOutOfRangeException">The frequency, gain or Q is outside its valid range.</exception>
    public void Validate(int sampleRate)
    {
        float nyquist = sampleRate / 2f;

        // Negated comparisons so that NaN is rejected as well.
        if (!(Frequency > 0f && Frequency < nyquist))
        {
            throw new ArgumentOutOfRangeException(nameof(Frequency),
                $"Frequency {Frequency}Hz is not in the valid range (0, {nyquist}Hz).");
        }

        if (!(Math.Abs(GainDb) <= MaxAbsGainDb))
        {
            throw new ArgumentOutOfRangeException(nameof(GainDb),
                $"Gain must be between -24 dB and +24 dB. Actual value: {GainDb} dB");
        }

        if (!(Q > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(Q), $"Q factor must be greater than zero. Actual value: {Q}");
        }
    }
}
