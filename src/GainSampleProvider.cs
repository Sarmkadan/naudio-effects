using NAudio.Wave;

namespace NAudioEffects;

/// <summary>
/// A sample provider that applies gain (volume adjustment) to audio samples with smooth smoothing.
/// </summary>
public class GainSampleProvider : EffectSampleProviderBase
{
    private const float DefaultSmoothingMs = 10.0f;
    private const float MillisecondsPerSecond = 1000.0f;

    private float _currentGainLinear = 1.0f;
    private float _targetGainLinear = 1.0f;
    private float _samplesPerMs;
    private int _samplesRemainingInRamp;

    /// <summary>
    /// Initializes a new instance of the GainSampleProvider.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    public GainSampleProvider(ISampleProvider source)
        : base(source)
    {
        _samplesPerMs = WaveFormat.SampleRate / MillisecondsPerSecond;
        SmoothingMs = DefaultSmoothingMs;
    }

    /// <summary>
    /// Gets or sets the gain in decibels.
    /// </summary>
    public float GainDb
    {
        get => DbConversion.LinearToDb(_currentGainLinear);
        set
        {
            var newGainLinear = DbConversion.DbToLinear(value);
            if (Math.Abs(_targetGainLinear - newGainLinear) > float.Epsilon)
            {
                _targetGainLinear = newGainLinear;
                _samplesRemainingInRamp = -1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the smoothing time in milliseconds.
    /// </summary>
    public float SmoothingMs { get; set; } = DefaultSmoothingMs;

    /// <summary>
    /// Processes a block of samples with gain adjustment.
    /// </summary>
    /// <param name="buffer">The buffer containing the samples.</param>
    /// <param name="offset">The offset in the buffer where the block starts.</param>
    /// <param name="samplesRead">The number of samples read into the buffer.</param>
    protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
    {
        // If smoothing is set to 0 (instant gain change), use a short default ramp (~10 ms)
        // to avoid clicks. This mirrors the default smoothing behavior.
        float rampMs = SmoothingMs > 0 ? SmoothingMs : DefaultSmoothingMs;
        int updateInterval = (int)(rampMs * _samplesPerMs);
        if (updateInterval < 1)
        {
            updateInterval = 1;
        }

        if (_samplesRemainingInRamp < 0)
        {
            _samplesRemainingInRamp = updateInterval;
        }

        float step = _samplesRemainingInRamp > 0
            ? (_targetGainLinear - _currentGainLinear) / _samplesRemainingInRamp
            : 0;
        for (int i = 0; i < samplesRead; i++)
        {
            if (step > 0)
            {
                _currentGainLinear = Math.Min(_currentGainLinear + step, _targetGainLinear);
            }
            else if (step < 0)
            {
                _currentGainLinear = Math.Max(_currentGainLinear + step, _targetGainLinear);
            }

            if (_samplesRemainingInRamp > 0)
            {
                _samplesRemainingInRamp--;
            }

            buffer[offset + i] *= _currentGainLinear;
        }
    }
}
