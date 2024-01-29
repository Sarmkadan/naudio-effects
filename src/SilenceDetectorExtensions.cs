using System;
using System.Collections.Generic;

namespace NAudioEffects
{
    /// <summary>
    /// Extension methods for <see cref="SilenceDetector"/> that provide additional functionality
    /// for working with silence detection results.
    /// </summary>
    public static class SilenceDetectorExtensions
    {
        /// <summary>
        /// Determines whether the silence detector has detected any silence regions.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>True if at least one silence region was detected; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static bool HasSilence(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);
            return detector.Regions.Count > 0;
        }

        /// <summary>
        /// Gets the total duration of all detected silence regions.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The total duration in milliseconds of all silence regions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static double GetTotalSilenceDurationMilliseconds(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            double totalMilliseconds = 0;
            foreach (var region in detector.Regions)
            {
                totalMilliseconds += region.Duration.TotalMilliseconds;
            }

            return totalMilliseconds;
        }

        /// <summary>
        /// Gets the total duration of all detected silence regions.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The total duration of all silence regions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static TimeSpan GetTotalSilenceDuration(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            TimeSpan total = TimeSpan.Zero;
            foreach (var region in detector.Regions)
            {
                total += region.Duration;
            }

            return total;
        }

        /// <summary>
        /// Gets the average duration of detected silence regions.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The average duration of silence regions, or TimeSpan.Zero if no regions detected.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static TimeSpan GetAverageSilenceDuration(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            if (detector.Regions.Count == 0)
            {
                return TimeSpan.Zero;
            }

            TimeSpan total = detector.GetTotalSilenceDuration();
            return TimeSpan.FromTicks(total.Ticks / detector.Regions.Count);
        }

        /// <summary>
        /// Gets the longest silence region detected.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The longest silence region, or null if no regions detected.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static SilenceDetector.SilenceRegion GetLongestSilence(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            SilenceDetector.SilenceRegion longest = default;
            bool found = false;
            foreach (var region in detector.Regions)
            {
                if (!found || region.Duration > longest.Duration)
                {
                    longest = region;
                    found = true;
                }
            }

            return found ? longest : default;
        }

        /// <summary>
        /// Gets the shortest silence region detected.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The shortest silence region, or default if no regions detected.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static SilenceDetector.SilenceRegion GetShortestSilence(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            SilenceDetector.SilenceRegion shortest = default;
            bool found = false;
            foreach (var region in detector.Regions)
            {
                if (!found || region.Duration < shortest.Duration)
                {
                    shortest = region;
                    found = true;
                }
            }

            return found ? shortest : default;
        }

        /// <summary>
        /// Gets the number of silence regions detected.
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>The count of detected silence regions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static int GetSilenceRegionCount(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);
            return detector.Regions.Count;
        }

        /// <summary>
        /// Determines whether the audio contains only silence (no non-silence regions).
        /// </summary>
        /// <param name="detector">The silence detector instance.</param>
        /// <returns>True if the entire audio is silence; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static bool IsEntirelySilence(this SilenceDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            if (detector.Regions.Count != 1)
            {
                return false;
            }

            var region = detector.Regions[0];
            return region.Start == TimeSpan.Zero && region.End >= detector.TotalDuration;
        }
    }
}
