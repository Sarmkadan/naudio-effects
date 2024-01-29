#nullable enable

using NAudio.Wave;
using System;

namespace NAudioEffects
{
    /// <summary>
    /// Provides extension methods for <see cref="CompressorSampleProvider"/> that enable fluent configuration
    /// and runtime control of compressor parameters.
    /// </summary>
    public static class CompressorSampleProviderExtensions
    {
        /// <summary>
        /// Sets the threshold in decibels.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="thresholdDb">The threshold in decibels.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider WithThreshold(this CompressorSampleProvider compressor, float thresholdDb)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.ThresholdDb = thresholdDb;
            return compressor;
        }

        /// <summary>
        /// Sets the ratio and updates the internal processing.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="ratio">The compression ratio (e.g., 4 for 4:1 compression).</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider WithRatio(this CompressorSampleProvider compressor, float ratio)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.Ratio = ratio;
            return compressor;
        }

        /// <summary>
        /// Sets the attack and release times in milliseconds.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="attackMs">The attack time in milliseconds.</param>
        /// <param name="releaseMs">The release time in milliseconds.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider WithEnvelopeSettings(this CompressorSampleProvider compressor, float attackMs, float releaseMs)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.AttackMs = attackMs;
            compressor.ReleaseMs = releaseMs;
            return compressor;
        }

        /// <summary>
        /// Sets the knee width in decibels.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="kneeDb">The knee width in decibels.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider WithKnee(this CompressorSampleProvider compressor, float kneeDb)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.KneeDb = kneeDb;
            return compressor;
        }

        /// <summary>
        /// Sets the makeup gain in decibels.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="makeupGainDb">The makeup gain in decibels.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider WithMakeupGain(this CompressorSampleProvider compressor, float makeupGainDb)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.MakeupGainDb = makeupGainDb;
            return compressor;
        }

        /// <summary>
        /// Enables or disables the compressor processing.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="enabled">Whether the compressor should be enabled.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        /// <remarks>
        /// Disabling the compressor sets <see cref="EffectSampleProviderBase.Bypass"/> so that samples pass
        /// through unmodified; enabling it clears the bypass so compression resumes.
        /// </remarks>
        public static CompressorSampleProvider WithEnabled(this CompressorSampleProvider compressor, bool enabled)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.Bypass = !enabled;
            return compressor;
        }

        /// <summary>
        /// Creates a new compressor instance with the specified parameters.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        /// <param name="thresholdDb">The threshold in decibels.</param>
        /// <param name="ratio">The compression ratio.</param>
        /// <param name="attackMs">The attack time in milliseconds.</param>
        /// <param name="releaseMs">The release time in milliseconds.</param>
        /// <param name="kneeDb">The knee width in decibels.</param>
        /// <param name="makeupGainDb">The makeup gain in decibels.</param>
        /// <returns>A new compressor instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static CompressorSampleProvider CreateCompressor(
            this ISampleProvider source,
            float thresholdDb = -20f,
            float ratio = 4f,
            float attackMs = 10f,
            float releaseMs = 100f,
            float kneeDb = 6f,
            float makeupGainDb = 0f)
        {
            ArgumentNullException.ThrowIfNull(source);

            return new CompressorSampleProvider(source)
            {
                ThresholdDb = thresholdDb,
                Ratio = ratio,
                AttackMs = attackMs,
                ReleaseMs = releaseMs,
                KneeDb = kneeDb,
                MakeupGainDb = makeupGainDb
            };
        }

        /// <summary>
        /// Resets the compressor to its default state.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider Reset(this CompressorSampleProvider compressor)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.ThresholdDb = -20;
            compressor.Ratio = 4;
            compressor.AttackMs = 10;
            compressor.ReleaseMs = 100;
            compressor.KneeDb = 6;
            compressor.MakeupGainDb = 0;
            return compressor;
        }

        /// <summary>
        /// Gets the current gain reduction as a linear factor (0-1).
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <returns>The gain reduction factor (1.0 = no reduction, 0.5 = -6dB reduction).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static float GetGainReductionFactor(this CompressorSampleProvider compressor)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            return 1f / (1f + MathF.Exp(-compressor.CurrentGainReductionDb * 0.2302585093f));
        }

        /// <summary>
        /// Gets a value indicating whether the compressor is currently applying gain reduction.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <returns>True if gain reduction is being applied; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static bool IsCompressing(this CompressorSampleProvider compressor)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            return compressor.CurrentGainReductionDb < -0.1f;
        }

        /// <summary>
        /// Sets all compressor parameters at once for quick configuration.
        /// </summary>
        /// <param name="compressor">The compressor instance.</param>
        /// <param name="thresholdDb">The threshold in decibels.</param>
        /// <param name="ratio">The compression ratio.</param>
        /// <param name="attackMs">The attack time in milliseconds.</param>
        /// <param name="releaseMs">The release time in milliseconds.</param>
        /// <param name="kneeDb">The knee width in decibels.</param>
        /// <param name="makeupGainDb">The makeup gain in decibels.</param>
        /// <returns>The compressor instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressor"/> is null.</exception>
        public static CompressorSampleProvider Configure(
            this CompressorSampleProvider compressor,
            float thresholdDb = -20f,
            float ratio = 4f,
            float attackMs = 10f,
            float releaseMs = 100f,
            float kneeDb = 6f,
            float makeupGainDb = 0f)
        {
            ArgumentNullException.ThrowIfNull(compressor);

            compressor.ThresholdDb = thresholdDb;
            compressor.Ratio = ratio;
            compressor.AttackMs = attackMs;
            compressor.ReleaseMs = releaseMs;
            compressor.KneeDb = kneeDb;
            compressor.MakeupGainDb = makeupGainDb;

            return compressor;
        }
    }
}