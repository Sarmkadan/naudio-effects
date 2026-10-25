#nullable enable

using System;

namespace NAudioEffects
{
    /// <summary>
    /// Conversions between decibel values and linear amplitudes (dBFS-relative, 1 = 0 dB).
    /// </summary>
    public static class DbConversion
    {
        /// <summary>
        /// Converts a decibel value to a linear amplitude.
        /// </summary>
        /// <param name="db">The decibel value (dB), typically relative to digital full scale (dBFS).</param>
        /// <returns>The linear amplitude, where 1 corresponds to 0 dB.</returns>
        public static float DbToLinear(float db)
        {
            return MathF.Pow(10.0f, db / 20.0f);
        }

        /// <summary>
        /// Converts a linear amplitude to decibels.
        /// </summary>
        /// <param name="linear">The linear amplitude, where 1 corresponds to 0 dB.</param>
        /// <returns>The decibel value (dB); returns <see cref="float.NegativeInfinity"/> for non-positive input.</returns>
        public static float LinearToDb(float linear)
        {
            if (linear <= 0f)
            {
                // Avoid log of zero or negative values.
                return float.NegativeInfinity;
            }
            return 20.0f * MathF.Log10(linear);
        }
    }
}
