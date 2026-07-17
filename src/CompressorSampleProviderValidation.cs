#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NAudioEffects
{
    /// <summary>
    /// Provides validation helpers for <see cref="CompressorSampleProvider"/> instances.
    /// </summary>
    public static class CompressorSampleProviderValidation
    {
        private const float MinThresholdDb = -60f;
        private const float MaxThresholdDb = 0f;
        private const float MinRatio = 1f;
        private const float MaxRatio = 20f;
        private const float MinAttackMs = 1f;
        private const float MaxAttackMs = 1000f;
        private const float MinReleaseMs = 10f;
        private const float MaxReleaseMs = 5000f;
        private const float MinKneeDb = 0f;
        private const float MaxKneeDb = 24f;
        private const float MinMakeupGainDb = -30f;
        private const float MaxMakeupGainDb = 30f;

        /// <summary>
        /// Validates the specified <see cref="CompressorSampleProvider"/> instance.
        /// </summary>
        /// <param name="value">The compressor sample provider to validate.</param>
        /// <returns>A list of validation problems; empty if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this CompressorSampleProvider? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.ThresholdDb < MinThresholdDb || value.ThresholdDb > MaxThresholdDb)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"ThresholdDb ({value.ThresholdDb:F2} dB) is out of range [{MinThresholdDb:F2}, {MaxThresholdDb:F2}] dB"));
            }

            if (value.Ratio < MinRatio || value.Ratio > MaxRatio)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Ratio ({value.Ratio:F2}) is out of range [{MinRatio:F2}, {MaxRatio:F2}]"));
            }

            if (value.AttackMs < MinAttackMs || value.AttackMs > MaxAttackMs)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"AttackMs ({value.AttackMs:F2} ms) is out of range [{MinAttackMs:F2}, {MaxAttackMs:F2}] ms"));
            }

            if (value.ReleaseMs < MinReleaseMs || value.ReleaseMs > MaxReleaseMs)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"ReleaseMs ({value.ReleaseMs:F2} ms) is out of range [{MinReleaseMs:F2}, {MaxReleaseMs:F2}] ms"));
            }

            if (value.KneeDb < MinKneeDb || value.KneeDb > MaxKneeDb)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"KneeDb ({value.KneeDb:F2} dB) is out of range [{MinKneeDb:F2}, {MaxKneeDb:F2}] dB"));
            }

            if (value.MakeupGainDb < MinMakeupGainDb || value.MakeupGainDb > MaxMakeupGainDb)
            {
                problems.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"MakeupGainDb ({value.MakeupGainDb:F2} dB) is out of range [{MinMakeupGainDb:F2}, {MaxMakeupGainDb:F2}] dB"));
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the specified <see cref="CompressorSampleProvider"/> instance is valid.
        /// </summary>
        /// <param name="value">The compressor sample provider to check.</param>
        /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this CompressorSampleProvider? value) => value is not null && Validate(value).Count == 0;

        /// <summary>
        /// Ensures that the specified <see cref="CompressorSampleProvider"/> instance is valid.
        /// </summary>
        /// <param name="value">The compressor sample provider to validate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The instance is invalid. Contains a list of validation problems.</exception>
        public static void EnsureValid(this CompressorSampleProvider? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"CompressorSampleProvider is invalid. Problems:\n- {
                    string.Join("\n- ", problems)
                    }");
            }
        }
    }
}