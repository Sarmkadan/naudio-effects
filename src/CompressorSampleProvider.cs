#nullable enable

using NAudio.Wave;
using NAudio.Dsp;

namespace NAudioEffects
{
    /// <summary>
    /// A sample provider that applies compression to the input audio.
    /// </summary>
    public class CompressorSampleProvider : EffectSampleProviderBase
    {
        private readonly EnvelopeFollower _envelopeFollower;
        private readonly int _sampleRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressorSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public CompressorSampleProvider(ISampleProvider source)
            : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            _envelopeFollower = new EnvelopeFollower(AttackMs, ReleaseMs, _sampleRate);
        }

        /// <summary>
        /// Gets or sets the threshold in decibels.
        /// </summary>
        /// <value>The threshold in decibels.</value>
        public float ThresholdDb { get; set; } = -20;

        /// <summary>
        /// Gets or sets the compression ratio.
        /// </summary>
        /// <value>The compression ratio.</value>
        public float Ratio { get; set; } = 4;

        /// <summary>
        /// Gets or sets the attack time in milliseconds.
        /// </summary>
        /// <value>The attack time in milliseconds.</value>
        public float AttackMs { get; set; } = 10;

        /// <summary>
        /// Gets or sets the release time in milliseconds.
        /// </summary>
        /// <value>The release time in milliseconds.</value>
        public float ReleaseMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets the knee in decibels.
        /// </summary>
        /// <value>The knee in decibels.</value>
        public float KneeDb { get; set; } = 6;

        /// <summary>
        /// Gets or sets the makeup gain in decibels.
        /// </summary>
        /// <value>The makeup gain in decibels.</value>
        public float MakeupGainDb { get; set; } = 0;

        /// <summary>
        /// Gets the current gain reduction in decibels.
        /// </summary>
        /// <value>The current gain reduction in decibels.</value>
        public float CurrentGainReductionDb { get; private set; }

        /// <summary>
        /// Processes the audio block.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset in the buffer.</param>
        /// <param name="count">The number of samples to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int count)
        {
            // Re-apply envelope timing every block so that changes to AttackMs/ReleaseMs
            // made after construction actually take effect.
            _envelopeFollower.SetParameters(AttackMs, ReleaseMs, _sampleRate);
            _envelopeFollower.Process(buffer, offset, count);

            float envelope = _envelopeFollower.Envelope;
            float envelopeDb = LinearToDb(envelope);

            float threshold = ThresholdDb;
            float knee = Math.Max(KneeDb, 0f);
            float ratio = Math.Max(Ratio, 1f);
            float makeupGainLinear = DbToLinear(MakeupGainDb);

            float overshoot = envelopeDb - threshold;
            float gainReductionDb;
            if (knee <= 0f)
            {
                gainReductionDb = overshoot > 0f ? (1f / ratio - 1f) * overshoot : 0f;
            }
            else if (2f * overshoot < -knee)
            {
                gainReductionDb = 0f;
            }
            else if (2f * Math.Abs(overshoot) <= knee)
            {
                float x = overshoot + (knee / 2f);
                gainReductionDb = (1f / ratio - 1f) * (x * x) / (2f * knee);
            }
            else
            {
                gainReductionDb = (1f / ratio - 1f) * overshoot;
            }

            CurrentGainReductionDb = gainReductionDb;
            float gain = DbToLinear(gainReductionDb) * makeupGainLinear;

            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= gain;
            }
        }
    }
}
