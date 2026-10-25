using NAudio.Dsp;

namespace NAudioEffects;

/// <summary>
/// Simple envelope follower for tracking signal level.
/// </summary>
public class EnvelopeFollower
{
    // Values below this magnitude are flushed to zero (float subnormals start near 1.2e-38)
    private const float DenormalThreshold = 1e-30f;

    private float _envelope = 0f;
    private float _attackCoefficient = 0f;
    private float _releaseCoefficient = 0f;

    /// <summary>
    /// Gets the current envelope value as a linear amplitude in the range 0 to 1,
    /// where 1 corresponds to digital full scale (0 dBFS).
    /// </summary>
    public float Envelope => _envelope;

    /// <summary>
    /// Initializes a new instance of the EnvelopeFollower.
    /// </summary>
    /// <param name="attackMs">Attack time in milliseconds (ms); controls how quickly the envelope rises toward louder signals.</param>
    /// <param name="releaseMs">Release time in milliseconds (ms); controls how quickly the envelope falls toward quieter signals.</param>
    /// <param name="sampleRate">Audio sample rate in hertz (Hz).</param>
    public EnvelopeFollower(float attackMs = 1f, float releaseMs = 200f, int sampleRate = 44100)
    {
        SetParameters(attackMs, releaseMs, sampleRate);
    }

    /// <summary>
    /// Initializes a new instance of the EnvelopeFollower from a validated settings struct.
    /// </summary>
    /// <param name="settings">The attack, release and sample rate settings.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the settings are invalid.</exception>
    public EnvelopeFollower(EnvelopeSettings settings)
    {
        settings.EnsureValid();
        SetParameters(settings.AttackMs, settings.ReleaseMs, settings.SampleRate);
    }

    /// <summary>
    /// Sets the time constants for the envelope follower.
    /// </summary>
    /// <param name="attackMs">Attack time in milliseconds (ms).</param>
    /// <param name="releaseMs">Release time in milliseconds (ms).</param>
    /// <param name="sampleRate">Audio sample rate in hertz (Hz).</param>
    public void SetParameters(float attackMs, float releaseMs, int sampleRate)
    {
        _attackCoefficient = CalculateCoefficient(attackMs, sampleRate);
        _releaseCoefficient = CalculateCoefficient(releaseMs, sampleRate);
    }

    /// <summary>
    /// Processes a block of audio samples and updates the envelope.
    /// </summary>
    /// <param name="buffer">The sample buffer containing interleaved samples.</param>
    /// <param name="offset">The offset into the buffer.</param>
    /// <param name="count">The number of samples to process.</param>
    public void Process(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float sample = Math.Abs(buffer[offset + i]);

            // Update envelope based on sample magnitude
            if (sample > _envelope)
            {
                _envelope = _attackCoefficient * _envelope + (1f - _attackCoefficient) * sample;
            }
            else
            {
                _envelope = _releaseCoefficient * _envelope + (1f - _releaseCoefficient) * sample;
            }

            // Release decays toward zero forever on silence; flush the tail so it never goes subnormal
            if (MathF.Abs(_envelope) < DenormalThreshold)
            {
                _envelope = 0f;
            }
        }
    }

    /// <summary>
    /// Calculates the smoothing coefficient for a given time constant and sample rate.
    /// </summary>
    /// <param name="timeConstantMs">The time constant in milliseconds (ms).</param>
    /// <param name="sampleRate">The audio sample rate in hertz (Hz).</param>
    /// <returns>The smoothing coefficient.</returns>
    private float CalculateCoefficient(float timeConstantMs, int sampleRate)
    {
        if (timeConstantMs <= 0f)
        {
            return 0f;
        }

        float timeConstantSeconds = timeConstantMs / 1000f;
        return (float)Math.Exp(-1f / (timeConstantSeconds * sampleRate));
    }
}
