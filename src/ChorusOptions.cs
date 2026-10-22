#nullable enable

using System;
using System.Globalization;

namespace NAudioEffects
{
    /// <summary>
    /// Configuration for <see cref="ChorusSampleProvider"/>.
    /// </summary>
    public class ChorusOptions
    {
        /// <summary>
        /// Exclusive lower bound for <see cref="RateHz"/>; zero is rejected.
        /// </summary>
        internal const float MinRateHzExclusive = 0f;

        /// <summary>
        /// Inclusive upper bound for <see cref="RateHz"/>.
        /// </summary>
        internal const float MaxRateHz = 20f;

        /// <summary>
        /// Exclusive lower bound for <see cref="DepthMs"/>; zero is rejected.
        /// </summary>
        internal const float MinDepthMsExclusive = 0f;

        /// <summary>
        /// Inclusive upper bound for <see cref="DepthMs"/>.
        /// </summary>
        internal const float MaxDepthMs = 50f;

        /// <summary>
        /// Inclusive lower bound for <see cref="Mix"/>.
        /// </summary>
        internal const float MinMix = 0f;

        /// <summary>
        /// Inclusive upper bound for <see cref="Mix"/>.
        /// </summary>
        internal const float MaxMix = 1f;

        internal const float DefaultRateHz = 0.3f;
        internal const float DefaultDepthMs = 5.0f;
        internal const float DefaultMix = 0.3f;

        /// <summary>
        /// Gets or sets the LFO rate in Hz. Must be greater than 0 and at most 20.
        /// </summary>
        public float RateHz { get; set; } = DefaultRateHz;

        /// <summary>
        /// Gets or sets the modulation depth in milliseconds. Must be greater than 0 and at most 50.
        /// </summary>
        public float DepthMs { get; set; } = DefaultDepthMs;

        /// <summary>
        /// Gets or sets the mix level (0 = dry only, 1 = wet only). Must be between 0 and 1 inclusive.
        /// </summary>
        public float Mix { get; set; } = DefaultMix;

        /// <summary>
        /// Gets a new <see cref="ChorusOptions"/> instance populated with the default chorus settings.
        /// A new instance is returned on every access because this type is mutable.
        /// </summary>
        public static ChorusOptions Default => new();

        /// <summary>
        /// Validates the current option values.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <see cref="RateHz"/> is not in (0, 20], <see cref="DepthMs"/> is not in (0, 50],
        /// or <see cref="Mix"/> is not in [0, 1]. The exception's <see cref="ArgumentException.ParamName"/>
        /// identifies the offending option.
        /// </exception>
        public void Validate()
        {
            ValidateRateHz(RateHz, nameof(RateHz));
            ValidateDepthMs(DepthMs, nameof(DepthMs));
            ValidateMix(Mix, nameof(Mix));
        }

        /// <summary>
        /// Validates a rate value against the (0, 20] range. NaN is rejected.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name reported in the thrown exception.</param>
        internal static void ValidateRateHz(float value, string paramName)
        {
            if (!(value > MinRateHzExclusive && value <= MaxRateHz))
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Create(CultureInfo.InvariantCulture, $"RateHz must be greater than 0 and at most {MaxRateHz}."));
            }
        }

        /// <summary>
        /// Validates a depth value against the (0, 50] range. NaN is rejected.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name reported in the thrown exception.</param>
        internal static void ValidateDepthMs(float value, string paramName)
        {
            if (!(value > MinDepthMsExclusive && value <= MaxDepthMs))
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Create(CultureInfo.InvariantCulture, $"DepthMs must be greater than 0 and at most {MaxDepthMs}."));
            }
        }

        /// <summary>
        /// Validates a mix value against the [0, 1] range. NaN is rejected.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name reported in the thrown exception.</param>
        internal static void ValidateMix(float value, string paramName)
        {
            if (!(value >= MinMix && value <= MaxMix))
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    string.Create(CultureInfo.InvariantCulture, $"Mix must be between {MinMix} and {MaxMix} inclusive."));
            }
        }
    }
}
