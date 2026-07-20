#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Tremolo effect using a sine‑wave LFO to modulate the amplitude of the input signal.
    /// </summary>
    public class TremoloSampleProvider : EffectSampleProviderBase
    {
        private readonly float _sampleRate;
        private float _lfoPhase;

        /// <summary>
        /// Gets or sets the LFO rate in Hz. Valid range is 0.1‑20. Default is 5 Hz.
        /// </summary>
        public float RateHz { get; set; } = 5.0f;

        /// <summary>
        /// Gets or sets the modulation depth (0 = no effect, 1 = full silence at troughs). Default is 0.5.
        /// </summary>
        public float Depth { get; set; } = 0.5f;

        /// <summary>
        /// Initializes a new instance of the <see cref="TremoloSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public TremoloSampleProvider(ISampleProvider source)
            : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            _lfoPhase = 0.0f;
        }

        /// <summary>
        /// Processes a block of samples, applying amplitude modulation.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset into the buffer where processing should start.</param>
        /// <param name="samplesRead">The number of samples (per channel) to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            int channels = WaveFormat.Channels;
            float lfoIncrement = (float)(2.0 * Math.PI * RateHz / _sampleRate);
            float depthClamped = Math.Clamp(Depth, 0.0f, 1.0f);

            for (int i = 0; i < samplesRead; i++)
            {
                // Current LFO value in the range [-1, 1]
                float lfoValue = (float)Math.Sin(_lfoPhase);

                // Convert LFO value to an amplitude factor.
                // (1 + lfoValue) / 2 gives a range [0, 1].
                // Depth controls how much the amplitude is reduced at the trough.
                float amplitude = 1.0f - depthClamped * (0.5f * (1.0f + lfoValue));

                // Apply the same amplitude to all channels for this sample frame.
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = offset + i * channels + ch;
                    buffer[idx] *= amplitude;
                }

                // Advance LFO phase for the next sample frame.
                _lfoPhase += lfoIncrement;
                if (_lfoPhase > Math.PI * 2.0f)
                {
                    _lfoPhase -= (float)(Math.PI * 2.0);
                }
            }
        }
    }
}
