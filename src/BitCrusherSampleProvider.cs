#nullable enable

using System;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Provides a bit-crusher effect for an NAudio sample stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bit-depth reduction quantizes each floating-point sample to one of a smaller number of amplitude
    /// levels, introducing the characteristic distortion and noise associated with low-resolution audio.
    /// Sample-rate reduction is simulated by processing only every <see cref="HoldFactor"/>th sample;
    /// increasing the hold factor therefore produces progressively stronger decimation artifacts.
    /// <see cref="Mix"/> blends the processed signal with the original signal.
    /// </para>
    /// <para>
    /// <see cref="BitDepth"/> accepts values from 1 through 32, <see cref="HoldFactor"/> accepts values
    /// greater than or equal to 1, and the effective range of <see cref="Mix"/> is 0 through 1. Mix values
    /// outside that range are clamped when audio is processed.
    /// </para>
    /// <example>
    /// The provider can be inserted between an audio source and an NAudio playback device:
    /// <code>
    /// using var reader = new AudioFileReader("input.wav");
    /// var crusher = new BitCrusherSampleProvider(reader)
    /// {
    ///     BitDepth = 8,
    ///     HoldFactor = 4,
    ///     Mix = 0.75f
    /// };
    ///
    /// using var output = new WaveOutEvent();
    /// output.Init(crusher);
    /// output.Play();
    /// </code>
    /// </example>
    /// </remarks>
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
        /// Gets or sets the number of bits used to quantize each processed sample.
        /// </summary>
        /// <value>An integer from 1 through 32. The default is 8.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is less than 1 or greater than 32.
        /// </exception>
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
        /// Gets or sets the factor used to reduce the effective sample rate.
        /// </summary>
        /// <value>
        /// An integer greater than or equal to 1. A value of 1 disables sample-rate reduction, a value of
        /// 2 processes every second sample, and so on. The default is 1.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than 1.</exception>
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
        /// Gets or sets the balance between the original and processed signals.
        /// </summary>
        /// <value>
        /// A value from 0 to 1, where 0 is fully dry and 1 is fully wet. The default is 0.5.
        /// Values outside this range are clamped during processing.
        /// </value>
        public float Mix { get; set; } = DefaultMix;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitCrusherSampleProvider"/> class with default
        /// effect settings.
        /// </summary>
        /// <param name="source">The sample provider from which audio is read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
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
        /// Reads samples from the source and applies bit-depth and sample-rate reduction.
        /// </summary>
        /// <param name="buffer">The buffer that receives the processed samples.</param>
        /// <param name="offset">The zero-based index in <paramref name="buffer"/> at which writing begins.</param>
        /// <param name="count">The maximum number of samples to write.</param>
        /// <returns>The number of samples written to <paramref name="buffer"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The region described by <paramref name="offset"/> and <paramref name="count"/> extends beyond
        /// the end of <paramref name="buffer"/>.
        /// </exception>
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
        /// Returns a string that contains the current bit depth, hold factor, and mix settings.
        /// </summary>
        /// <returns>A string representation of the provider's current configuration.</returns>
        public override string ToString() => $"BitCrusherSampleProvider {{ BitDepth = {BitDepth}, HoldFactor = {HoldFactor}, Mix = {Mix} }}";
    }
}
