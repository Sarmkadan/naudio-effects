#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Auto-panning effect using an LFO to modulate the stereo position of mono sources.
    /// Mono input is rejected with an exception; stereo sources are panned between left and right channels.
    /// Supports sine and triangle waveforms.
    /// </summary>
    public class AutoPanSampleProvider : EffectSampleProviderBase
    {
        private readonly float _sampleRate;
        private float _lfoPhase;

        /// <summary>
        /// Gets or sets the LFO rate in Hz. Valid range is 0.1‑20. Default is 5 Hz.
        /// </summary>
        public float RateHz { get; set; } = 5.0f;

        /// <summary>
        /// Gets or sets the modulation depth (0 = no panning, 1 = full left/right alternation). Default is 0.5.
        /// </summary>
        public float Depth { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets the LFO waveform shape. Default is Sine.
        /// </summary>
        public Waveform Waveform { get; set; } = Waveform.Sine;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPanSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        /// <exception cref="ArgumentException">Thrown if the source is mono.</exception>
        public AutoPanSampleProvider(ISampleProvider source)
            : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            _lfoPhase = 0.0f;

            // Reject mono sources - auto-panning only makes sense for stereo
            if (source.WaveFormat.Channels < 2)
            {
                throw new ArgumentException(
                    "AutoPanSampleProvider requires a stereo source. Mono sources are not supported.",
                    nameof(source));
            }
        }

        /// <summary>
        /// Processes a block of samples, applying stereo panning modulation.
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
                // Get LFO value based on selected waveform
                float lfoValue = GetLfoValue(_lfoPhase);

                // Convert LFO value to stereo panning position [-1, 1]
                // where -1 = full left, 0 = center, 1 = full right
                float panPosition = lfoValue;

                // Apply depth scaling
                panPosition *= depthClamped;

                // Calculate left/right gains based on pan position
                // Using constant power panning law for smooth transitions
                float leftGain = (float)Math.Cos((panPosition + 1.0f) * Math.PI / 4.0f);
                float rightGain = (float)Math.Cos((1.0f - panPosition) * Math.PI / 4.0f);

                // Apply panning to each channel
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = offset + i * channels + ch;

                    if (ch == 0) // Left channel
                    {
                        buffer[idx] *= leftGain;
                    }
                    else if (ch == 1) // Right channel
                    {
                        buffer[idx] *= rightGain;
                    }
                    // Additional channels (if any) are not panned
                }

                // Advance LFO phase for the next sample frame
                _lfoPhase += lfoIncrement;
                if (_lfoPhase > Math.PI * 2.0f)
                {
                    _lfoPhase -= (float)(Math.PI * 2.0);
                }
            }
        }

        private float GetLfoValue(float phase)
        {
            switch (Waveform)
            {
                case Waveform.Sine:
                    return (float)Math.Sin(phase);

                case Waveform.Triangle:
                    // Triangle wave: -1 to 1 with linear ramps
                    float value = (float)(2.0 * (phase / (Math.PI * 2.0)));
                    if (value < 0.5f)
                    {
                        return 4.0f * value - 1.0f; // -1 to 0
                    }
                    else
                    {
                        return 1.0f - 4.0f * (value - 0.5f); // 0 to 1 to 0
                    }

                default:
                    return (float)Math.Sin(phase); // Default to sine
            }
        }
    }

    /// <summary>
    /// Waveform types for LFO modulation.
    /// </summary>
    public enum Waveform
    {
        /// <summary>Sine wave.</summary>
        Sine,

        /// <summary>Triangle wave.</summary>
        Triangle
    }
}