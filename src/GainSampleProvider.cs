using NAudio.Wave;

namespace NAudioEffects;

/// <summary>
/// A sample provider that applies gain (volume adjustment) to audio samples with smooth smoothing.
/// </summary>
public class GainSampleProvider : EffectSampleProviderBase
{
    private float _currentGainLinear = 1.0f;
    private float _targetGainLinear = 1.0f;
    private float _samplesPerMs;
    private int _samplesUntilNextUpdate;
    private int _updateInterval;

    /// <summary>
    /// Initializes a new instance of the GainSampleProvider.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    public GainSampleProvider(ISampleProvider source)
        : base(source)
    {
        _samplesPerMs = WaveFormat.SampleRate / 1000.0f;
        SmoothingMs = 10;
    }

    /// <summary>
    /// Gets or sets the gain in decibels.
    /// </summary>
    public float GainDb
    {
        get => LinearToDb(_currentGainLinear);
        set
        {
            var newGainLinear = DbToLinear(value);
            if (Math.Abs(_targetGainLinear - newGainLinear) > float.Epsilon)
            {
                _targetGainLinear = newGainLinear;
                // Ensure a smoothing period is scheduled even if SmoothingMs is zero.
                _samplesUntilNextUpdate = _updateInterval;
            }
        }
    }

    /// <summary>
    /// Gets or sets the smoothing time in milliseconds.
    /// </summary>
    public float SmoothingMs { get; set; } = 10;

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
        float rampMs = SmoothingMs > 0 ? SmoothingMs : 10f;
        _updateInterval = (int)(rampMs * _samplesPerMs);
        if (_updateInterval < 1)
        {
            _updateInterval = 1;
        }

        if (_samplesUntilNextUpdate > 0)
        {
            // Apply current gain during smoothing period
            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] *= _currentGainLinear;
            }
            _samplesUntilNextUpdate -= samplesRead;
        }
        else
        {
            // Update gain smoothly
            float step = (_targetGainLinear - _currentGainLinear) / _updateInterval;
            for (int i = 0; i < samplesRead; i++)
            {
                _currentGainLinear += step;
                buffer[offset + i] *= _currentGainLinear;
            }
            _samplesUntilNextUpdate = _updateInterval - (samplesRead % _updateInterval);
        }
    }
}
