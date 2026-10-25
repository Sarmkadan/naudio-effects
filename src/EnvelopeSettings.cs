namespace NAudioEffects;

/// <summary>
/// Attack, release and sample rate settings used to configure an <see cref="EnvelopeFollower"/>.
/// </summary>
public readonly struct EnvelopeSettings
{
    /// <summary>
    /// Gets the attack time in milliseconds (ms); controls how quickly the envelope rises toward louder signals.
    /// </summary>
    public float AttackMs { get; }

    /// <summary>
    /// Gets the release time in milliseconds (ms); controls how quickly the envelope falls toward quieter signals.
    /// </summary>
    public float ReleaseMs { get; }

    /// <summary>
    /// Gets the audio sample rate in hertz (Hz).
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvelopeSettings"/> struct.
    /// </summary>
    /// <param name="attackMs">Attack time in milliseconds (ms). Must be finite and non-negative.</param>
    /// <param name="releaseMs">Release time in milliseconds (ms). Must be finite and non-negative.</param>
    /// <param name="sampleRate">Audio sample rate in hertz (Hz). Must be greater than zero.</param>
    public EnvelopeSettings(float attackMs = 1f, float releaseMs = 200f, int sampleRate = 44100)
    {
        AttackMs = attackMs;
        ReleaseMs = releaseMs;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// Validates the settings and throws if any value is out of range.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a time is negative or not finite, or the sample rate is not positive.</exception>
    public void EnsureValid()
    {
        if (!float.IsFinite(AttackMs) || AttackMs < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(AttackMs), AttackMs, "Attack time must be a finite, non-negative value in milliseconds.");
        }

        if (!float.IsFinite(ReleaseMs) || ReleaseMs < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(ReleaseMs), ReleaseMs, "Release time must be a finite, non-negative value in milliseconds.");
        }

        if (SampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRate), SampleRate, "Sample rate must be greater than zero.");
        }
    }
}
