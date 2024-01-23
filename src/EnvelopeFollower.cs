using NAudio.Dsp;

namespace NAudioEffects;

/// <summary>
/// Simple envelope follower for tracking signal level.
/// </summary>
public class EnvelopeFollower
{
    private float _envelope = 0f;
    private float _attackCoefficient = 0f;
    private float _releaseCoefficient = 0f;

    /// <summary>
    /// Gets the current envelope value (0 to 1).
    /// </summary>
    public float Envelope => _envelope;

    /// <summary>
    /// Initializes a new instance of the EnvelopeFollower.
    /// </summary>
    /// <param name="attackMs">Attack time in milliseconds.</param>
    /// <param name="releaseMs">Release time in milliseconds.</param>
    /// <param name="sampleRate">Audio sample rate.</param>
    public EnvelopeFollower(float attackMs = 1f, float releaseMs = 200f, int sampleRate = 44100)
    {
        SetParameters(attackMs, releaseMs, sampleRate);
    }

    /// <summary>
    /// Sets the time constants for the envelope follower.
    /// </summary>
    /// <param name="attackMs">Attack time in milliseconds.</param>
    /// <param name="releaseMs">Release time in milliseconds.</param>
    /// <param name="sampleRate">Audio sample rate.</param>
    public void SetParameters(float attackMs, float releaseMs, int sampleRate)
    {
        _attackCoefficient = CalculateCoefficient(attackMs, sampleRate);
        _releaseCoefficient = CalculateCoefficient(releaseMs, sampleRate);
    }

    /// <summary>
    /// Processes a block of audio samples and updates the envelope.
    /// </summary>
    /// <param name="buffer">The sample buffer.</param>
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
        }
    }

    /// <summary>
    /// Calculates the smoothing coefficient for a given time constant and sample rate.
    /// </summary>
    /// <param name="timeConstantMs">The time constant in milliseconds.</param>
    /// <param name="sampleRate">The audio sample rate.</param>
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