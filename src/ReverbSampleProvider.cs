#nullable enable

using NAudio.Wave;
using NAudio.Dsp;

namespace NAudioEffects
{
    /// <summary>
    /// Simple Schroeder reverb effect using 4 comb filters and 2 allpass filters per channel.
    /// Based on the classic reverb design by Manfred Schroeder.
    /// </summary>
    public class ReverbSampleProvider : EffectSampleProviderBase
    {
        // Comb filter delays in samples (at 44.1kHz)
        private static readonly int[] CombDelays = { 1116, 1356, 1691, 1916 };

        // Allpass filter delays in samples (at 44.1kHz)
        private static readonly int[] AllpassDelays = { 225, 556 };

        // Allpass feedback coefficients
        private const float AllpassFeedback = 0.5f;

        private readonly float[][] _combBuffers;  // One per comb filter per channel
        private readonly float[][] _allpassBuffers; // One per allpass filter per channel
        private readonly int[] _combDelayLengths;
        private readonly int[] _allpassDelayLengths;
        private readonly int[] _combIndices;      // Current write position for each comb
        private readonly int[] _allpassIndices;    // Current write position for each allpass

        // Pre-delay buffers (one per channel)
        private readonly float[][] _preDelayBuffers;
        private readonly int[] _preDelayLengths;
        private readonly int[] _preDelayIndices;

        private float _roomSize = 0.5f;
        private float _damping = 0.5f;
        private float _wetLevel = 0.33f;
        private float _dryLevel = 0.67f;
        private float _preDelaySeconds = 0.0f;

        /// <summary>
        /// Gets or sets the room size (0-1)
        /// </summary>
        public float RoomSize
        {
            get => _roomSize;
            set => _roomSize = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets or sets the damping (high-frequency absorption) (0-1)
        /// </summary>
        public float Damping
        {
            get => _damping;
            set => _damping = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets or sets the wet level (reverb output) (0-1)
        /// </summary>
        public float WetLevel
        {
            get => _wetLevel;
            set => _wetLevel = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets or sets the dry level (dry signal) (0-1)
        /// </summary>
        public float DryLevel
        {
            get => _dryLevel;
            set => _dryLevel = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets or sets the pre-delay in seconds (0-2)
        /// </summary>
        public float PreDelaySeconds
        {
            get => _preDelaySeconds;
            set
            {
                _preDelaySeconds = Math.Clamp(value, 0.0f, 2.0f);
                UpdatePreDelayBuffers();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverbSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public ReverbSampleProvider(ISampleProvider source)
            : base(source)
        {
            int sampleRate = source.WaveFormat.SampleRate;
            double sampleRateScale = sampleRate / 44100.0;

            int channels = WaveFormat.Channels;

            // Comb filters
            _combBuffers = new float[CombDelays.Length][];
            _combDelayLengths = new int[CombDelays.Length];
            _combIndices = new int[CombDelays.Length];

            for (int i = 0; i < CombDelays.Length; i++)
            {
                int scaledDelay = Math.Max(1, (int)Math.Round(CombDelays[i] * sampleRateScale));

                _combDelayLengths[i] = scaledDelay;
                _combBuffers[i] = new float[scaledDelay];
                _combIndices[i] = 0;
            }

            // Allpass filters
            _allpassBuffers = new float[AllpassDelays.Length][];
            _allpassDelayLengths = new int[AllpassDelays.Length];
            _allpassIndices = new int[AllpassDelays.Length];

            for (int i = 0; i < AllpassDelays.Length; i++)
            {
                int scaledDelay = Math.Max(1, (int)Math.Round(AllpassDelays[i] * sampleRateScale));

                _allpassDelayLengths[i] = scaledDelay;
                _allpassBuffers[i] = new float[scaledDelay];
                _allpassIndices[i] = 0;
            }

            // Pre-delay buffers (one per channel)
            _preDelayBuffers = new float[channels][];
            _preDelayLengths = new int[channels];
            _preDelayIndices = new int[channels];

            UpdatePreDelayBuffers();
        }

        private void UpdatePreDelayBuffers()
        {
            int sampleRate = WaveFormat.SampleRate;
            int channels = WaveFormat.Channels;

            for (int ch = 0; ch < channels; ch++)
            {
                int delayLength = Math.Max(1, (int)Math.Round(_preDelaySeconds * sampleRate));
                _preDelayLengths[ch] = delayLength;
                _preDelayBuffers[ch] = new float[delayLength];
                _preDelayIndices[ch] = 0;
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
            int channels = WaveFormat.Channels;

            // Process each channel separately
            for (int ch = 0; ch < channels; ch++)
            {
                ProcessChannel(buffer, offset, samplesRead, ch);
            }
        }

        private void ProcessChannel(float[] buffer, int offset, int samplesRead, int channelIndex)
        {
            float[] combOut = new float[samplesRead];

            // Apply pre-delay: delay the input for the wet path
            float[] delayedInput = new float[samplesRead];
            for (int s = 0; s < samplesRead; s++)
            {
                int sampleIndex = offset + s;
                float input = buffer[sampleIndex + channelIndex];

                // Write to pre-delay buffer
                _preDelayBuffers[channelIndex][_preDelayIndices[channelIndex]] = input;

                // Read from pre-delay buffer (with delay)
                int readIndex = (_preDelayIndices[channelIndex] - _preDelayLengths[channelIndex] + _preDelayBuffers[channelIndex].Length) % _preDelayBuffers[channelIndex].Length;
                delayedInput[s] = _preDelayBuffers[channelIndex][readIndex];

                // Advance write index
                _preDelayIndices[channelIndex] = (_preDelayIndices[channelIndex] + 1) % _preDelayBuffers[channelIndex].Length;
            }

            // Apply 4 parallel comb filters (using delayed input)
            for (int i = 0; i < _combBuffers.Length; i++)
            {
                float[] combBuffer = _combBuffers[i];
                int combIndex = _combIndices[i];
                float feedback = 0.5f + 0.4f * RoomSize;
                float dampingFactor = 1.0f - 0.8f * Damping;

                for (int s = 0; s < samplesRead; s++)
                {
                    int sampleIndex = offset + s;
                    int readIndex = (combIndex - _combDelayLengths[i] + combBuffer.Length) % combBuffer.Length;

                    // Read from circular buffer
                    float input = delayedInput[s] * 0.25f; // Normalize to avoid clipping
                    float output = combBuffer[readIndex];

                    // Apply comb filter with feedback
                    float newValue = input + (output * feedback * dampingFactor);
                    combBuffer[combIndex] = newValue;

                    combOut[s] += output;

                    combIndex = (combIndex + 1) % combBuffer.Length;
                }

                _combIndices[i] = combIndex;
            }

            // Apply 2 serial allpass filters
            float[] allpassIn = combOut;
            for (int i = 0; i < _allpassBuffers.Length; i++)
            {
                float[] allpassBuffer = _allpassBuffers[i];
                int allpassIndex = _allpassIndices[i];

                for (int s = 0; s < samplesRead; s++)
                {
                    int sampleIndex = offset + s;
                    int readIndex = (allpassIndex - _allpassDelayLengths[i] + allpassBuffer.Length) % allpassBuffer.Length;

                    // Read from circular buffer
                    float input = allpassIn[s];
                    float output = allpassBuffer[readIndex];

                    // Allpass filter: y[n] = -g*x[n] + x[n-d] + g*y[n-d]
                    float allpassOut = -AllpassFeedback * input + output + AllpassFeedback * allpassBuffer[allpassIndex];

                    // Store and advance
                    allpassBuffer[allpassIndex] = input + (AllpassFeedback * allpassOut);
                    allpassIn[s] = allpassOut;

                    allpassIndex = (allpassIndex + 1) % allpassBuffer.Length;
                }

                _allpassIndices[i] = allpassIndex;
            }

            // Mix dry and wet signals
            float wetGain = WetLevel;
            float dryGain = DryLevel;

            for (int s = 0; s < samplesRead; s++)
            {
                int sampleIndex = offset + s;
                float drySample = buffer[sampleIndex + channelIndex]; // Dry is undelayed
                float wetSample = allpassIn[s];

                // Apply to the appropriate channel
                buffer[sampleIndex + channelIndex] = (drySample * dryGain) + (wetSample * wetGain);
            }
        }

        /// <summary>
        /// Fluent builder for creating ReverbSampleProvider instances.
        /// </summary>
        public class ReverbBuilder
        {
            private float _roomSize = 0.5f;
            private float _damping = 0.5f;
            private float _wetLevel = 0.33f;
            private float _dryLevel = 0.67f;
            private float _preDelaySeconds = 0.0f;

            /// <summary>
            /// Sets the room size (0-1).
            /// </summary>
            public ReverbBuilder WithRoomSize(float roomSize)
            {
                _roomSize = Math.Clamp(roomSize, 0.0f, 1.0f);
                return this;
            }

            /// <summary>
            /// Sets the damping (high-frequency absorption) (0-1).
            /// </summary>
            public ReverbBuilder WithDamping(float damping)
            {
                _damping = Math.Clamp(damping, 0.0f, 1.0f);
                return this;
            }

            /// <summary>
            /// Sets the wet level (reverb output) (0-1).
            /// </summary>
            public ReverbBuilder WithWetDryMix(float wetLevel)
            {
                _wetLevel = Math.Clamp(wetLevel, 0.0f, 1.0f);
                // Adjust dry level to maintain constant total gain?
                // We'll let dry level be set independently, but note that the default dry level is 0.67 when wet is 0.33.
                // For simplicity, we'll just set wet level and leave dry level as set separately.
                return this;
            }

            /// <summary>
            /// Sets the pre-delay in seconds (0-2).
            /// </summary>
            public ReverbBuilder WithPreDelay(float preDelaySeconds)
            {
                _preDelaySeconds = Math.Clamp(preDelaySeconds, 0.0f, 2.0f);
                return this;
            }

            /// <summary>
            /// Builds the ReverbSampleProvider with the specified source.
            /// </summary>
            /// <param name="source">The source sample provider.</param>
            /// <returns>A configured ReverbSampleProvider instance.</returns>
            public ReverbSampleProvider Build(ISampleProvider source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                var reverb = new ReverbSampleProvider(source)
                {
                    RoomSize = _roomSize,
                    Damping = _damping,
                    WetLevel = _wetLevel,
                    DryLevel = _dryLevel,
                    PreDelaySeconds = _preDelaySeconds
                };

                return reverb;
            }
        }

        /// <summary>
        /// Creates a new ReverbBuilder for fluent configuration of a ReverbSampleProvider.
        /// </summary>
        /// <returns>A new ReverbBuilder instance.</returns>
        public static ReverbBuilder Create()
        {
            return new ReverbBuilder();
        }
    }
}