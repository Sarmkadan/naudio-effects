#nullable enable

using System;

namespace NAudioEffects
{
    /// <summary>
    /// Extension methods for <see cref="TremoloSampleProvider"/>.
    /// </summary>
    public static class TremoloSampleProviderExtensions
    {
        /// <summary>
        /// Calculates the tremolo rate (in Hz) based on BPM and musical note division.
        /// </summary>
        /// <param name="bpm">Beats per minute.</param>
        /// <param name="noteDivision">The note division (e.g., 4 for quarter, 8 for eighth).</param>
        /// <returns>The rate in Hz.</returns>
        public static float RateFromBpm(float bpm, int noteDivision)
        {
            if (bpm <= 0)
                throw new ArgumentException("BPM must be greater than zero.", nameof(bpm));
            
            if (noteDivision <= 0)
                throw new ArgumentException("Note division must be greater than zero.", nameof(noteDivision));

            // Rate (Hz) = (BPM / 60) * (NoteDivision / 4)
            // Example: 120 BPM, 4th note (quarter) = (120/60) * (4/4) = 2 Hz
            // Example: 120 BPM, 8th note = (120/60) * (8/4) = 4 Hz
            return (bpm / 60.0f) * (noteDivision / 4.0f);
        }
    }
}
