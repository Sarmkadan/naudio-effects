#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Flanger effect using LFO-modulated short delay (0.1-10ms) with feedback.
    /// Creates a sweeping, whooshing sound by mixing the input with a delayed,
    /// phase-modulated copy of itself. The delay time is modulated by a low-frequency
    /// oscillator (LFO) to produce the characteristic flanging effect.
    /// </summary>
    public class FlangerSampleProvider : EffectSampleProviderBase
    {
        private CircularBuffer _delayBuffer;
        private readonly float _sampleRate;
        private float _lfoPhase;
        private float _lfoValue;

        // Delay line state
        private int _delaySamples;
        private int _writeIndex;

        // Default parameters
        private const float DefaultRateHz = 0.1f;      // Slow modulation for flanging
        private const float DefaultDepthMs = 2.0f;       // 2ms depth typical for flanging
        private const float DefaultFeedback = 0.5f;       // 50% feedback typical
        private const float DefaultDelayMs = 1.0f;       // Base delay of 1ms
        private const float DefaultMix = 0.5f;           // Equal mix of dry and wet

        /// <summary>
        /// Gets or sets the LFO rate in Hz (0.01-2).
        /// </summary>
        public float RateHz { get; set; } = DefaultRateHz;

        /// <summary>
        /// Gets or sets the modulation depth in milliseconds (0-10).
        /// </summary>
        public float DepthMs { get; set; } = DefaultDepthMs;

        /// <summary>
        /// Gets or sets the feedback amount (0 = no feedback, 1 = full feedback).
        /// </summary>
        public float Feedback { get; set; } = DefaultFeedback;

        /// <summary>
        /// Gets or sets the base delay time in milliseconds (0.1-10).
        /// </summary>
        public float DelayMs { get; set; } = DefaultDelayMs;

        /// <summary>
        /// Gets or sets the mix level (0 = dry only, 1 = wet only).
        /// </summary>
        public float Mix { get; set; } = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlangerSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public FlangerSampleProvider(ISampleProvider source)
        : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            _delayBuffer = new CircularBuffer((int)(_sampleRate * 0.02f)); // 20ms buffer
            UpdateDelayParameters();
        }

        /// <summary>
        /// Updates internal delay parameters based on current property values.
        /// </summary>
        private void UpdateDelayParameters()
        {
            // Calculate delay time in samples
            float totalDelayMs = DelayMs + DepthMs;
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

            // Update LFO phase
            float lfoIncrement = (float)(2.0 * Math.PI * RateHz / _sampleRate);

            // Process each channel separately
            for (int ch = 0; ch < channels; ch++)
            {
                ProcessChannel(buffer, offset, samplesRead, ch, lfoIncrement);
            }

            // Update LFO phase for next block
            _lfoPhase += lfoIncrement * samplesRead;
            if (_lfoPhase > Math.PI * 2.0f)
            {
                _lfoPhase -= (float)(Math.PI * 2.0f);
            }
        }

        private void ProcessChannel(float[] buffer, int offset, int samplesRead, int channelIndex, float lfoIncrement)
        {
            float wetMix = Math.Clamp(Mix, 0.0f, 1.0f);
            float dryMix = 1.0f - wetMix;

            // Update LFO value for this channel
            _lfoValue = (float)Math.Sin(_lfoPhase + (channelIndex * 0.5f)); // Slight phase shift per channel for stereo effect

            // Process samples
            for (int s = 0; s < samplesRead; s++)
            {
                int sampleIndex = offset + s;
                float inputSample = buffer[sampleIndex + channelIndex];

                // Write to delay buffer
                _delayBuffer.Write(inputSample);
                _writeIndex = (_writeIndex + 1) % _delayBuffer.Capacity;

                // Calculate modulated delay time using LFO
                // LFO ranges from -1 to 1, we scale by depth and add base delay
                float modulation = _lfoValue * DepthMs;
                float currentDelayMs = DelayMs + modulation;
                int currentDelaySamples = (int)((currentDelayMs / 1000.0f) * _sampleRate);

                // Clamp delay to valid range (1 sample to buffer capacity - 1)
                currentDelaySamples = Math.Clamp(currentDelaySamples, 1, _delayBuffer.Capacity - 1);

                // Read from delay buffer with linear interpolation
                float delayedSample = ReadWithLinearInterpolation(currentDelaySamples);

                // Apply feedback to create the characteristic flanging sound
                delayedSample += delayedSample * Feedback;

                // Mix dry and wet signals
                buffer[sampleIndex + channelIndex] = (inputSample * dryMix) + (delayedSample * wetMix);
            }
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
            float totalDelayMs = DelayMs + DepthMs;
            int requiredCapacity = (int)((totalDelayMs / 1000.0f) * _sampleRate * 2); // 2x safety margin

            if (requiredCapacity > _delayBuffer.Capacity)
            {
                _delayBuffer = new CircularBuffer(requiredCapacity);
                _writeIndex = 0;
                _lfoPhase = 0;
            }
        }

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