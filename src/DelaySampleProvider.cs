#nullable enable

using System;
using System.Threading;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Provides a classic feedback delay effect that creates repeating echoes of an
    /// <see cref="ISampleProvider"/> source.
    /// </summary>
    /// <remarks>Each channel is processed with an independent circular delay line.</remarks>
    public class DelaySampleProvider : EffectSampleProviderBase
    {
        private const float DefaultFeedback = 0.35f;
        private const float DefaultMix = 0.5f;

        private readonly float[][] _delayLines;
        private readonly int[] _writePositions;
        private readonly int[] _masks;
        private readonly float _maxDelayMs;
        private readonly float _samplesPerMillisecond;
        private float _delayMs;
        private float _feedback = DefaultFeedback;
        private float _mix = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelaySampleProvider"/> class.
        /// </summary>
        /// <param name="source">The non-<see langword="null"/> source sample provider to process.</param>
        /// <param name="maxDelayMs">
        /// The maximum supported delay time, in milliseconds. The value must be finite and
        /// greater than or equal to <c>0</c>. The default is <c>2000</c> milliseconds.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The wave format of <paramref name="source"/> has a sample rate or channel count
        /// that is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxDelayMs"/> is negative, is not finite, or is too large for
        /// the source sample rate.
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
            
            // Round up to next power of two for efficient bitwise masking
            int delayLineLength = Math.Max(1, (int)maximumDelaySamples + 1);
            int capacity = 1;
            while (capacity < delayLineLength) capacity <<= 1;

            _delayLines = new float[channels][];
            _writePositions = new int[channels];
            _masks = new int[channels];

            for (int channel = 0; channel < channels; channel++)
            {
                _delayLines[channel] = new float[capacity];
                _masks[channel] = capacity - 1;
            }
        }

        /// <summary>
        /// Gets or sets the delay time, in milliseconds.
        /// </summary>
        /// <value>
        /// A value from <c>0</c> through the maximum delay supplied to the constructor,
        /// inclusive. The default is <c>0</c> milliseconds.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is less than <c>0</c> or greater than the configured maximum delay.
        /// </exception>
        public float DelayMs
        {
            get => _delayMs;
            set
            {
                if (value < 0f || value > _maxDelayMs)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Delay time must be between zero and the maximum delay.");
                }
                _delayMs = value;
            }
        }

        /// <summary>
        /// Gets or sets the unitless proportion of the delayed signal fed back into the delay line.
        /// </summary>
        /// <value>A value from <c>0</c> through <c>0.95</c>, inclusive. The default is <c>0.35</c>.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is less than <c>0</c> or greater than <c>0.95</c>.
        /// </exception>
        public float Feedback
        {
            get => _feedback;
            set
            {
                if (value < 0f || value > 0.95f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Feedback must be between 0 and 0.95.");
                }
                _feedback = value;
            }
        }

        /// <summary>
        /// Gets or sets the unitless dry/wet mix.
        /// </summary>
        /// <value>
        /// A value from <c>0</c> through <c>1</c>, inclusive, where <c>0</c> is fully dry
        /// and <c>1</c> is fully wet. The default is <c>0.5</c>.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is less than <c>0</c> or greater than <c>1</c>.
        /// </exception>
        public float Mix
        {
            get => _mix;
            set
            {
                if (value < 0f || value > 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Mix must be between 0 and 1.");
                }
                _mix = value;
            }
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
                    int readPosition = (writePosition - delaySamples) & _masks[channel];
                    delayed = delayLine[readPosition];
                }

                delayLine[writePosition & _masks[channel]] = input + (delayed * _feedback);
                _writePositions[channel] = (writePosition + 1) & _masks[channel];
                buffer[offset + sample] = (input * dryMix) + (delayed * _mix);
            }
        }

        /// <summary>
        /// Reads samples from the source, applies the delay effect unless bypassed, and
        /// periodically observes a cancellation request.
        /// </summary>
        /// <param name="buffer">The buffer into which processed samples are written.</param>
        /// <param name="offset">The zero-based index in <paramref name="buffer"/> at which writing begins.</param>
        /// <param name="count">The number of interleaved samples to read.</param>
        /// <param name="cancellationToken">The token used to cancel the read operation.</param>
        /// <returns><paramref name="count"/> after all requested samples have been processed.</returns>
        /// <exception cref="OperationCanceledException">
        /// Cancellation was requested through <paramref name="cancellationToken"/>.
        /// </exception>
        public int Read(float[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            const int blockSize = 256;
            int processed = 0;
            while (processed < count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int blockSizeToProcess = Math.Min(blockSize, count - processed);
                base.Read(buffer, offset + processed, blockSizeToProcess);
                processed += blockSizeToProcess;
            }
            return count;
        }
    }
}
