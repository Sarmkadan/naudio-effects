#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Bit crusher effect that reduces bit depth (2-16 bits) and decimates sample rate (hold factor).
    /// Creates a lo-fi, distorted sound by quantizing samples to fewer bits and reducing the effective
    /// sample rate. The wet/dry mix allows blending the processed signal with the original.
    /// </summary>
    public class BitCrusherSampleProvider : EffectSampleProviderBase
    {
        private readonly float _sampleRate;
        private int _holdCounter;
        private int _holdPeriod;

        // Default parameters
        private const int DefaultBitDepth = 8;
        private const int DefaultHoldFactor = 1;
        private const float DefaultMix = 0.5f;

        /// <summary>
        /// Gets or sets the bit depth (2-16 bits). Default is 8 bits.
        /// </summary>
        public int BitDepth { get; set; } = DefaultBitDepth;

        /// <summary>
        /// Gets or sets the hold factor for sample rate decimation (1-16).
        /// A value of 1 means no decimation, 2 means every other sample is kept, etc.
        /// Default is 1 (no decimation).
        /// </summary>
        public int HoldFactor { get; set; } = DefaultHoldFactor;

        /// <summary>
        /// Gets or sets the mix level (0 = dry only, 1 = wet only). Default is 0.5.
        /// </summary>
        public float Mix { get; set; } = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitCrusherSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public BitCrusherSampleProvider(ISampleProvider source)
            : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            UpdateHoldPeriod();
        }

        /// <summary>
        /// Updates the hold period based on current HoldFactor.
        /// </summary>
        private void UpdateHoldPeriod()
        {
            _holdPeriod = Math.Max(1, HoldFactor);
            _holdCounter = 0;
        }

        /// <summary>
        /// Processes a block of samples, applying bit crushing and sample rate decimation.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset into the buffer where processing should start.</param>
        /// <param name="samplesRead">The number of samples (per channel) to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            // Update hold period if HoldFactor changed
            if (HoldFactor != _holdPeriod)
            {
                UpdateHoldPeriod();
            }

            int channels = WaveFormat.Channels;
            float wetMix = Math.Clamp(Mix, 0.0f, 1.0f);
            float dryMix = 1.0f - wetMix;

            // Process each channel separately
            for (int ch = 0; ch < channels; ch++)
            {
                ProcessChannel(buffer, offset, samplesRead, ch, wetMix, dryMix);
            }
        }

        private void ProcessChannel(float[] buffer, int offset, int samplesRead, int channelIndex, float wetMix, float dryMix)
        {
            int bitDepthClamped = Math.Clamp(BitDepth, 2, 16);

            // Process samples
            for (int s = 0; s < samplesRead; s++)
            {
                int sampleIndex = offset + s;
                float inputSample = buffer[sampleIndex + channelIndex];

                // Apply sample rate decimation (hold factor)
                _holdCounter++;
                if (_holdCounter >= _holdPeriod)
                {
                    _holdCounter = 0;

                    // Apply bit depth reduction
                    float crushedSample = ApplyBitCrushing(inputSample, bitDepthClamped);

                    // Mix dry and wet signals
                    buffer[sampleIndex + channelIndex] = (inputSample * dryMix) + (crushedSample * wetMix);
                }
                else
                {
                    // Skip this sample (decimation)
                    buffer[sampleIndex + channelIndex] = inputSample * dryMix;
                }
            }
        }

        /// <summary>
        /// Applies bit depth reduction to a sample.
        /// </summary>
        /// <param name="sample">The input sample.</param>
        /// <param name="bitDepth">The bit depth to reduce to (2-16).</param>
        /// <returns>The quantized sample.</returns>
        private static float ApplyBitCrushing(float sample, int bitDepth)
        {
            // Calculate the maximum value for the given bit depth
            float maxValue = (float)Math.Pow(2, bitDepth - 1) - 1;
            float scale = 1.0f / maxValue;

            // Quantize the sample to the specified bit depth
            // First, scale to the bit depth range
            float quantized = sample * maxValue;

            // Round to nearest integer
            quantized = (float)Math.Round(quantized);

            // Clamp to valid range
            quantized = Math.Clamp(quantized, -maxValue, maxValue);

            // Scale back to [-1, 1] range
            return quantized * scale;
        }
    }
}
