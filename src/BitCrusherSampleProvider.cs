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

        // Backing fields
        private int _bitDepth;
        private int _holdFactor;

        // Default parameters
        private const int DefaultBitDepth = 8;
        private const int DefaultHoldFactor = 1;
        private const float DefaultMix = 0.5f;

        /// <summary>
        /// Gets or sets the bit depth (1-32 bits). Default is 8 bits.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Value must be between 1 and 32.</exception>
        public int BitDepth
        {
            get => _bitDepth;
            set
            {
                if (value < 1 || value > 32)
                    throw new ArgumentOutOfRangeException(nameof(value), "Bit depth must be between 1 and 32.");
                _bitDepth = value;
            }
        }

        /// <summary>
        /// Gets or sets the hold factor for sample rate decimation (>=1).
        /// A value of 1 means no decimation, 2 means every other sample is kept, etc.
        /// Default is 1 (no decimation).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Value must be greater than or equal to 1.</exception>
        public int HoldFactor
        {
            get => _holdFactor;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Hold factor must be greater than or equal to 1.");
                _holdFactor = value;
                UpdateHoldPeriod();
            }
        }

        /// <summary>
        /// Gets or sets the mix level (0 = dry only, 1 = wet only). Default is 0.5.
        /// </summary>
        public float Mix { get; set; } = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitCrusherSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        /// <exception cref="ArgumentNullException">If source is null.</exception>
        public BitCrusherSampleProvider(ISampleProvider source)
            : base(source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Initialize backing fields with default values
            _bitDepth = DefaultBitDepth;
            _holdFactor = DefaultHoldFactor;

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
        /// Reads samples from the source and processes them.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset in the buffer to start writing.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        /// <exception cref="ArgumentNullException">If buffer is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If offset or count is negative.</exception>
        public override int Read(float[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("Offset and count exceed buffer length.");

            return base.Read(buffer, offset, count);
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
                    float crushedSample = ApplyBitCrushing(inputSample, BitDepth);

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
        /// <param name="bitDepth">The bit depth to reduce to (1-32).</param>
        /// <returns>The quantized sample.</returns>
        private static float ApplyBitCrushing(float sample, int bitDepth)
        {
            // Handle 1 bit as a special case (two levels: -1 and 1)
            if (bitDepth == 1)
            {
                return sample >= 0 ? 1f : -1f;
            }

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

        /// <summary>
        /// Returns a concise, informative representation of this provider.
        /// </summary>
        public override string ToString() => $"BitCrusherSampleProvider {{ BitDepth = {BitDepth}, HoldFactor = {HoldFactor}, Mix = {Mix} }}";
    }
}