#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Base class for sample provider effects.
    /// </summary>
    public abstract class EffectSampleProviderBase : ISampleProvider
    {
        private readonly ISampleProvider _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectSampleProviderBase"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        protected EffectSampleProviderBase(ISampleProvider source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Gets the wave format of the underlying source.
        /// </summary>
        public WaveFormat WaveFormat => _source.WaveFormat;

        /// <summary>
        /// Gets or sets a value indicating whether the effect should bypass processing.
        /// </summary>
        public bool Bypass { get; set; }

        /// <summary>
        /// Reads samples from the source and optionally processes them.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            if (!Bypass && samplesRead > 0)
            {
                ProcessBlock(buffer, offset, samplesRead);
            }
            return samplesRead;
        }

        /// <summary>
        /// Processes a block of samples. Derived classes must implement this.
        /// </summary>
        /// <param name="buffer">The buffer containing the samples.</param>
        /// <param name="offset">The offset in the buffer where the block starts.</param>
        /// <param name="samplesRead">The number of samples read into the buffer.</param>
        protected abstract void ProcessBlock(float[] buffer, int offset, int samplesRead);

        /// <summary>
        /// Converts a decibel value to a linear amplitude.
        /// </summary>
        /// <param name="db">The decibel value.</param>
        /// <returns>The linear amplitude.</returns>
        protected static float DbToLinear(float db)
        {
            return (float)Math.Pow(10.0, db / 20.0);
        }

        /// <summary>
        /// Converts a linear amplitude to decibels.
        /// </summary>
        /// <param name="linear">The linear amplitude.</param>
        /// <returns>The decibel value.</returns>
        protected static float LinearToDb(float linear)
        {
            if (linear <= 0f)
            {
                // Avoid log of zero or negative values.
                return float.NegativeInfinity;
            }
            return 20.0f * (float)Math.Log10(linear);
        }
    }
}
