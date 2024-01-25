namespace NAudioEffects;

using NAudio.Wave;

/// <summary>
/// A sample provider that removes DC offset using a single-pole high-pass filter.
/// The filter uses the formula: y[n] = x[n] - x[n-1] + R * y[n-1]
/// where R is the feedback coefficient (typically 0.995).
/// </summary>
public class DcOffsetRemoverSampleProvider : EffectSampleProviderBase
{
    private readonly float[] _previousInput;
    private readonly float[] _previousOutput;
    private float _r = 0.995f;

    /// <summary>
    /// Initializes a new instance of the <see cref="DcOffsetRemoverSampleProvider"/> class.
    /// </summary>
    /// <param name="source">The source sample provider.</param>
    public DcOffsetRemoverSampleProvider(ISampleProvider source) : base(source)
    {
        _previousInput = new float[WaveFormat.Channels];
        _previousOutput = new float[WaveFormat.Channels];
    }

    /// <summary>
    /// Gets or sets the feedback coefficient R (default: 0.995).
    /// Higher values result in a slower high-pass filter.
    /// </summary>
    public float R
    {
        get => _r;
        set => _r = Math.Clamp(value, 0.0f, 0.9999f);
    }

    /// <summary>
    /// Processes a block of samples.
    /// </summary>
    /// <param name="buffer">The audio buffer.</param>
    /// <param name="offset">The offset in the buffer.</param>
    /// <param name="samplesRead">The number of samples to process.</param>
    protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
    {
        int channels = WaveFormat.Channels;

        for (int ch = 0; ch < channels; ch++)
        {
            ProcessChannel(buffer, offset, samplesRead, ch);
        }
    }

    private void ProcessChannel(float[] buffer, int offset, int samplesRead, int channelIndex)
    {
        for (int s = 0; s < samplesRead; s++)
        {
            int sampleIndex = offset + s;
            float input = buffer[sampleIndex + channelIndex];
            float output = input - _previousInput[channelIndex] + _r * _previousOutput[channelIndex];

            _previousInput[channelIndex] = input;
            _previousOutput[channelIndex] = output;

            buffer[sampleIndex + channelIndex] = output;
        }
    }
}