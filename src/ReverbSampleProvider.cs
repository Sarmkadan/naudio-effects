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

        // Allpass filter delays in samples
        private static readonly int[] AllpassDelays = { 225, 556 };

        // Allpass feedback coefficients
        private const float AllpassFeedback = 0.5f;

        private readonly float[][] _combBuffers;  // One per comb filter per channel
        private readonly float[][] _allpassBuffers; // One per allpass filter per channel
        private readonly int[] _combIndices;      // Current write position for each comb
        private readonly int[] _allpassIndices;    // Current write position for each allpass

        private float _roomSize = 0.5f;
        private float _damping = 0.5f;
        private float _wetLevel = 0.33f;
        private float _dryLevel = 0.67f;

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
        /// Initializes a new instance of the <see cref="ReverbSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public ReverbSampleProvider(ISampleProvider source)
            : base(source)
        {
            int channels = source.WaveFormat.Channels;
            int sampleRate = source.WaveFormat.SampleRate;

            // Calculate delay times based on room size and sample rate
            _combBuffers = new float[CombDelays.Length][];
            _combIndices = new int[CombDelays.Length];

            for (int i = 0; i < CombDelays.Length; i++)
            {
                // Scale delay times based on room size and sample rate
                int baseDelay = (int)(CombDelays[i] * (0.5f + 0.5f * RoomSize));
                int scaledDelay = (int)(baseDelay * (44100.0f / sampleRate));

                _combBuffers[i] = new float[scaledDelay];
                _combIndices[i] = 0;
            }

            // Allpass filters
            _allpassBuffers = new float[AllpassDelays.Length][];
            _allpassIndices = new int[AllpassDelays.Length];

            for (int i = 0; i < AllpassDelays.Length; i++)
            {
                int baseDelay = (int)(AllpassDelays[i] * (0.5f + 0.5f * RoomSize));
                int scaledDelay = (int)(baseDelay * (44100.0f / sampleRate));

                _allpassBuffers[i] = new float[scaledDelay];
                _allpassIndices[i] = 0;
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

            // Apply 4 parallel comb filters
            for (int i = 0; i < _combBuffers.Length; i++)
            {
                float[] combBuffer = _combBuffers[i];
                int combIndex = _combIndices[i];
                float feedback = 0.5f + 0.4f * RoomSize;
                float dampingFactor = 1.0f - 0.8f * Damping;

                for (int s = 0; s < samplesRead; s++)
                {
                    int sampleIndex = offset + s;
                    int readIndex = (combIndex - CombDelays[i] + combBuffer.Length) % combBuffer.Length;

                    // Read from circular buffer
                    float input = buffer[sampleIndex + channelIndex] * 0.25f; // Normalize to avoid clipping
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
                    int readIndex = (allpassIndex - AllpassDelays[i] + allpassBuffer.Length) % allpassBuffer.Length;

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
                float drySample = buffer[sampleIndex + channelIndex];
                float wetSample = allpassIn[s];

                // Apply to the appropriate channel
                buffer[sampleIndex + channelIndex] = (drySample * dryGain) + (wetSample * wetGain);
            }
        }
    }
}
