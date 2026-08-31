#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Classic feedback delay effect that creates repeating echoes of the source signal.
    /// Each channel is processed with an independent circular delay line.
    /// </summary>
    public class DelaySampleProvider : EffectSampleProviderBase
    {
        private const float DefaultFeedback = 0.35f;
        private const float DefaultMix = 0.5f;

        private readonly float[][] _delayLines;
        private readonly int[] _writePositions;
        private readonly float _maxDelayMs;
        private readonly float _samplesPerMillisecond;
        private float _delayMs;
        private float _feedback = DefaultFeedback;
        private float _mix = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelaySampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        /// <param name="maxDelayMs">The maximum supported delay time in milliseconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxDelayMs"/> is negative, not finite, or too large
        /// to allocate a delay line for the source sample rate.
        /// </exception>
        public DelaySampleProvider(ISampleProvider source, float maxDelayMs = 2000f)
            : base(source)
        {
            if (maxDelayMs < 0f || float.IsNaN(maxDelayMs) || float.IsInfinity(maxDelayMs))
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelayMs), "Maximum delay must be a finite, non-negative value.");
            }

            int sampleRate = source.WaveFormat.SampleRate;
            int channels = source.WaveFormat.Channels;
            if (sampleRate <= 0)
            {
                throw new ArgumentException("The source sample rate must be greater than zero.", nameof(source));
            }

            if (channels <= 0)
            {
                throw new ArgumentException("The source channel count must be greater than zero.", nameof(source));
            }

            double maximumDelaySamples = Math.Ceiling(maxDelayMs * sampleRate / 1000.0);
            if (maximumDelaySamples >= int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelayMs), "Maximum delay is too large for the source sample rate.");
            }

            _maxDelayMs = maxDelayMs;
            _samplesPerMillisecond = sampleRate / 1000f;
            int delayLineLength = Math.Max(1, (int)maximumDelaySamples + 1);
            _delayLines = new float[channels][];
            _writePositions = new int[channels];

            for (int channel = 0; channel < channels; channel++)
            {
                _delayLines[channel] = new float[delayLineLength];
            }
        }

        /// <summary>
        /// Gets or sets the delay time in milliseconds, clamped between zero and the
        /// maximum delay specified when the effect was constructed.
        /// </summary>
        public float DelayMs
        {
            get => _delayMs;
            set => _delayMs = Math.Clamp(value, 0f, _maxDelayMs);
        }

        /// <summary>
        /// Gets or sets the amount of the delayed signal fed back into the delay line.
        /// The value is clamped between 0 and 0.95. The default is 0.35.
        /// </summary>
        public float Feedback
        {
            get => _feedback;
            set => _feedback = Math.Clamp(value, 0f, 0.95f);
        }

        /// <summary>
        /// Gets or sets the dry/wet mix, where 0 is dry only and 1 is wet only.
        /// The value is clamped between 0 and 1. The default is 0.5.
        /// </summary>
        public float Mix
        {
            get => _mix;
            set => _mix = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Processes a block of interleaved audio samples through the feedback delay.
        /// </summary>
        /// <param name="buffer">The audio buffer containing the samples to process.</param>
        /// <param name="offset">The offset in <paramref name="buffer"/> where processing starts.</param>
        /// <param name="samplesRead">The number of interleaved samples to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            int channels = WaveFormat.Channels;
            int delaySamples = Math.Min(
                (int)Math.Round(_delayMs * _samplesPerMillisecond),
                _delayLines[0].Length - 1);
            float dryMix = 1f - _mix;

            for (int sample = 0; sample < samplesRead; sample++)
            {
                int channel = sample % channels;
                float[] delayLine = _delayLines[channel];
                int writePosition = _writePositions[channel];
                float input = buffer[offset + sample];
                float delayed;

                if (delaySamples == 0)
                {
                    delayed = input;
                }
                else
                {
                    int readPosition = writePosition - delaySamples;
                    if (readPosition < 0)
                    {
                        readPosition += delayLine.Length;
                    }

                    delayed = delayLine[readPosition];
                }

                delayLine[writePosition] = input + (delayed * _feedback);
                _writePositions[channel] = (writePosition + 1) % delayLine.Length;
                buffer[offset + sample] = (input * dryMix) + (delayed * _mix);
            }
        }
    }
}
