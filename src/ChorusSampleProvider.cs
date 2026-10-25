#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Chorus effect using LFO-modulated delay with linear interpolation.
    /// Creates a shimmering, thickening effect by duplicating the input signal
    /// with a delayed, pitch-modulated copy.
    /// </summary>
    public class ChorusSampleProvider : EffectSampleProviderBase
    {
        private CircularBuffer _delayBuffer;
        private readonly float _sampleRate;
        private float _lfoPhase;
        private float _lfoValue;

        // Delay line state
        private int _delaySamples;
        private int _writeIndex;

        // Default parameters
        private const float DefaultBaseDelayMs = 20.0f;

        private float _rateHz = ChorusOptions.DefaultRateHz;
        private float _depthMs = ChorusOptions.DefaultDepthMs;
        private float _mix = ChorusOptions.DefaultMix;

        /// <summary>
        /// Gets or sets the LFO rate in Hz (greater than 0, at most 20).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in (0, 20].</exception>
        public float RateHz
        {
            get => _rateHz;
            set
            {
                ChorusOptions.ValidateRateHz(value, nameof(RateHz));
                _rateHz = value;
            }
        }

        /// <summary>
        /// Gets or sets the modulation depth in milliseconds (greater than 0, at most 50).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in (0, 50].</exception>
        public float DepthMs
        {
            get => _depthMs;
            set
            {
                ChorusOptions.ValidateDepthMs(value, nameof(DepthMs));
                _depthMs = value;
            }
        }

        /// <summary>
        /// Gets or sets the mix level (0 = dry only, 1 = wet only).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not in [0, 1].</exception>
        public float Mix
        {
            get => _mix;
            set
            {
                ChorusOptions.ValidateMix(value, nameof(Mix));
                _mix = value;
            }
        }

        /// <summary>
        /// Gets or sets the base delay time in milliseconds (default 20ms).
        /// </summary>
        public float BaseDelayMs { get; set; } = DefaultBaseDelayMs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChorusSampleProvider"/> class
        /// with default parameters.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public ChorusSampleProvider(ISampleProvider source)
            : base(source)
        {
            // Validate the incoming WaveFormat before using it.
            ValidateWaveFormat(source.WaveFormat);

            _sampleRate = source.WaveFormat.SampleRate;
            _delayBuffer = new CircularBuffer((int)(_sampleRate * 0.05f)); // 50ms buffer
            UpdateDelayParameters();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChorusSampleProvider"/> class
        /// using the values in <paramref name="options"/>. The options are validated first
        /// and their values are copied, so later changes to <paramref name="options"/>
        /// do not affect this instance.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        /// <param name="options">The chorus configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A value in <paramref name="options"/> is out of range.</exception>
        public ChorusSampleProvider(ISampleProvider source, ChorusOptions options)
            : this(source)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();

            RateHz = options.RateHz;
            DepthMs = options.DepthMs;
            Mix = options.Mix;
        }

        /// <summary>
        /// Validates that the provided WaveFormat meets the requirements for effect processing.
        /// Throws an <see cref="ArgumentException"/> if the format is invalid, including the
        /// offending format details in the exception message.
        /// </summary>
        /// <param name="format">The WaveFormat to validate.</param>
        private static void ValidateWaveFormat(WaveFormat format)
        {
            if (format.SampleRate <= 0)
                throw new ArgumentException($"Invalid WaveFormat: SampleRate must be > 0, got {format.SampleRate}.");

            if (format.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException($"Invalid WaveFormat: Encoding must be IEEE float, got {format.Encoding}.");

            if (format.Channels <= 0)
                throw new ArgumentException($"Invalid WaveFormat: Channels must be > 0, got {format.Channels}.");
        }

        /// <summary>
        /// Updates internal delay parameters based on current property values.
        /// </>
        private void UpdateDelayParameters()
        {
            // Calculate delay time in samples
            float totalDelayMs = BaseDelayMs + DepthMs;
            _delaySamples = (int)((totalDelayMs / 1000.0f) * _sampleRate);

            // Ensure we have enough buffer space
            if (_delaySamples * 2 > _delayBuffer.Capacity)
            {
                _delayBuffer.EnsureCapacity(_delaySamples * 2);
            }
        }

        /// <summary>
        /// Processes a block of samples.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset in the buffer.</param>
        /// <param name="samplesRead">The number of samples to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            // Reinitialize buffer if parameters changed
            ReinitializeDelayBuffer();

            int channels = WaveFormat.Channels;

            // Update LFO phase increment per sample (not per block)
            float lfoIncrement = (float)(2.0 * Math.PI * RateHz / _sampleRate) / channels;

            // Process all samples interleaved
            for (int s = 0; s < samplesRead; s++)
            {
                int channel = s % channels;

                // Update LFO value for this sample's channel
                _lfoValue = (float)Math.Sin(_lfoPhase + (channel * 0.5f));

                // Process one sample
                ProcessSample(buffer, offset + s, channel, lfoIncrement);

                // Accumulate LFO phase for this sample
                _lfoPhase += lfoIncrement * channels;
                if (_lfoPhase > Math.PI * 2.0f)
                {
                    _lfoPhase -= (float)(Math.PI * 2.0f);
                }
            }
        }

        /// <summary>
        /// Processes a single interleaved sample through the chorus effect.
        /// </summary>
        /// <param name="buffer">The audio buffer containing interleaved samples.</param>
        /// <param name="sampleOffset">The offset of the sample in the buffer.</param>
        /// <param name="channel">The channel index for this sample.</param>
        /// <param name="lfoIncrement">The LFO increment per channel.</param>
        private void ProcessSample(float[] buffer, int sampleOffset, int channel, float lfoIncrement)
        {
            float wetMix = Math.Clamp(Mix, 0.0f, 1.0f);

            // If mix is 0 (dry only), bypass all processing for bit-identical output
            if (wetMix == 0.0f)
            {
                return;
            }

            // Update LFO value for this channel
            _lfoValue = (float)Math.Sin(_lfoPhase + (channel * 0.5f)); // Slight phase shift per channel for stereo effect

            // Read the interleaved sample
            float inputSample = buffer[sampleOffset];

            // Write to delay buffer
            _delayBuffer.Write(inputSample);
            _writeIndex = (_writeIndex + 1) % _delayBuffer.Capacity;

            // Calculate modulated delay time using LFO
            // LFO ranges from -1 to 1, we scale by depth and add base delay
            float modulation = _lfoValue * DepthMs;
            float currentDelayMs = BaseDelayMs + modulation;
            int currentDelaySamples = (int)((currentDelayMs / 1000.0f) * _sampleRate);

            // Clamp delay to valid range (1 sample to buffer capacity - 1)
            currentDelaySamples = Math.Clamp(currentDelaySamples, 1, _delayBuffer.Capacity - 1);

            // Read from delay buffer with linear interpolation
            float delayedSample = ReadWithLinearInterpolation(currentDelaySamples);

            // Mix dry and wet signals
            buffer[sampleOffset] = MixWetDry(inputSample, delayedSample, wetMix);
        }

        /// <summary>
        /// Reads from the delay buffer with linear interpolation.
        /// </summary>
        /// <param name="delaySamples">The delay time in samples.</param>
        /// <returns>The interpolated sample.</returns>
        private float ReadWithLinearInterpolation(int delaySamples)
        {
            // Calculate read position with fractional part for interpolation
            float readPosFloat = (_writeIndex - delaySamples + _delayBuffer.Capacity) % _delayBuffer.Capacity;
            int readPosInt = (int)readPosFloat;
            float fraction = readPosFloat - readPosInt;

            // Read two adjacent samples
            float sample1 = _delayBuffer.Read(readPosInt);
            float sample2 = _delayBuffer.Read((readPosInt + 1) % _delayBuffer.Capacity);

            // Linear interpolation: sample = sample1 + fraction * (sample2 - sample1)
            return sample1 + (fraction * (sample2 - sample1));
        }

        /// <summary>
        /// Re-initialize delay buffer when parameters change.
        /// </summary>
        private void ReinitializeDelayBuffer()
        {
            float totalDelayMs = BaseDelayMs + DepthMs;
            int requiredCapacity = (int)((totalDelayMs / 1000.0f) * _sampleRate * 2); // 2x safety margin

            if (requiredCapacity > _delayBuffer.Capacity)
            {
                // Only reset delay-line state when the buffer is actually reallocated;
                // resetting on every block would restart the LFO phase and write position
                // each call, producing audible glitches instead of continuous modulation.
                _delayBuffer = new CircularBuffer(requiredCapacity);
                _writeIndex = 0;
                _lfoPhase = 0;
            }
        }

        public override string ToString() => $"ChorusSampleProvider {{ RateHz = {RateHz}, DepthMs = {DepthMs}, Mix = {Mix}, BaseDelayMs = {BaseDelayMs} }}";

        /// <summary>
        /// Simple circular buffer for delay line implementation.
        /// </summary>
        private class CircularBuffer
        {
            private float[] _buffer;
            private int _writeIndex;

            public int Capacity => _buffer.Length;

            public CircularBuffer(int capacity)
            {
                _buffer = new float[capacity];
                _writeIndex = 0;
            }

            public void EnsureCapacity(int capacity)
            {
                if (capacity > _buffer.Length)
                {
                    var newBuffer = new float[capacity];
                    Array.Copy(_buffer, newBuffer, _buffer.Length);
                    _buffer = newBuffer;
                }
            }

            public void Write(float sample)
            {
                _buffer[_writeIndex] = sample;
                _writeIndex = (_writeIndex + 1) % _buffer.Length;
            }

            public float Read(int index)
            {
                return _buffer[index];
            }
        }
    }
}
