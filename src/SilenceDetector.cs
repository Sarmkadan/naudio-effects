// Copyright (c) 2023-present NAudioEffects Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudioEffects
{
    /// <summary>
    /// Detects silence regions in audio sample streams
    /// </summary>
    public class SilenceDetector
    {
        private readonly float sampleRate;
        private readonly int channels;
        private readonly float thresholdDb;
        private readonly double minDurationSeconds;
        private readonly double minSamples;
        private List<SilenceRegion> regions = new List<SilenceRegion>();
        
        private long currentSilenceStartSample = -1;
        private long totalSamplesProcessed = 0;

        /// <summary>
        /// Creates a new silence detector
        /// </summary>
        /// <param name="sampleRate">Audio sample rate in Hz</param>
        /// <param name="channels">Number of audio channels</param>
        /// <param name="thresholdDb">Threshold in dB below which audio is considered silent (-50dB default)</param>
        /// <param name="minDurationSeconds">Minimum duration in seconds for a silence region to be reported (0.5s default)</param>
        public SilenceDetector(int sampleRate, int channels, float thresholdDb = -50f, double minDurationSeconds = 0.5)
        {
            this.sampleRate = sampleRate;
            this.channels = channels;
            this.thresholdDb = thresholdDb;
            this.minDurationSeconds = minDurationSeconds;
            this.minSamples = minDurationSeconds * sampleRate;
        }

        /// <summary>
        /// Processes a buffer of audio samples
        /// </summary>
        /// <param name="buffer">Audio samples buffer</param>
        /// <param name="offset">Offset in buffer to start processing</param>
        /// <param name="count">Number of samples to process</param>
        public void Process(float[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentException("Offset and count exceed buffer length");

            for (int i = 0; i < count; i += channels)
            {
                float maxSample = 0f;
                for (int c = 0; c < channels; c++)
                {
                    float sample = Math.Abs(buffer[offset + i + c]);
                    if (sample > maxSample) maxSample = sample;
                }

                bool isSilent = 20 * Math.Log10(maxSample + 1e-9f) < thresholdDb;

                if (isSilent)
                {
                    if (currentSilenceStartSample == -1)
                    {
                        currentSilenceStartSample = totalSamplesProcessed;
                    }
                }
                else
                {
                    if (currentSilenceStartSample != -1)
                    {
                        long silenceEndSample = totalSamplesProcessed;
                        long silenceDurationSamples = silenceEndSample - currentSilenceStartSample;
                        
                        if (silenceDurationSamples >= minSamples)
                        {
                            var start = TimeSpan.FromSeconds(currentSilenceStartSample / (double)sampleRate);
                            var end = TimeSpan.FromSeconds(silenceEndSample / (double)sampleRate);
                            regions.Add(new SilenceRegion(start, end));
                        }
                        
                        currentSilenceStartSample = -1;
                    }
                }

                totalSamplesProcessed++;
            }
        }

        /// <summary>
        /// Finalizes processing and closes any open silence regions
        /// </summary>
        public void Complete()
        {
            if (currentSilenceStartSample != -1)
            {
                long silenceEndSample = totalSamplesProcessed;
                long silenceDurationSamples = silenceEndSample - currentSilenceStartSample;
                
                if (silenceDurationSamples >= minSamples)
                {
                    var start = TimeSpan.FromSeconds(currentSilenceStartSample / (double)sampleRate);
                    var end = TimeSpan.FromSeconds(silenceEndSample / (double)sampleRate);
                    regions.Add(new SilenceRegion(start, end));
                }
            }
        }

        /// <summary>
        /// Gets the detected silence regions
        /// </summary>
        public IReadOnlyList<SilenceRegion> Regions => regions.AsReadOnly();

        /// <summary>
        /// Gets the total duration of audio processed so far.
        /// </summary>
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(totalSamplesProcessed / (double)sampleRate);

        /// <summary>
        /// Represents a region of silence in audio
        /// </summary>
        public class SilenceRegion
        {
            /// <summary>
            /// Start time of silence region
            /// </summary>
            public TimeSpan Start { get; }

            /// <summary>
            /// End time of silence region
            /// </summary>
            public TimeSpan End { get; }

            /// <summary>
            /// Duration of silence region
            /// </summary>
            public TimeSpan Duration => End - Start;

            /// <summary>
            /// Creates a new silence region
            /// </summary>
            public SilenceRegion(TimeSpan start, TimeSpan end)
            {
                Start = start;
                End = end;
            }
        }
    }
}
