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
        private float _thresholdLinear;
        private float _kneeLinear;
        private float _makeupGainLinear;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressorSampleProvider"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public CompressorSampleProvider(ISampleProvider source) 
            : base(source)
        {
            _envelopeFollower = new EnvelopeFollower(AttackMs, ReleaseMs, source.WaveFormat.SampleRate);
            UpdateThreshold();
            UpdateKnee();
            UpdateMakeupGain();
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

        private void UpdateThreshold()
        {
            _thresholdLinear = DbToLinear(ThresholdDb);
        }

        private void UpdateKnee()
        {
            _kneeLinear = DbToLinear(KneeDb);
        }

        private void UpdateMakeupGain()
        {
            _makeupGainLinear = DbToLinear(MakeupGainDb);
        }

        /// <summary>
        /// Processes the audio block.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset in the buffer.</param>
        /// <param name="count">The number of samples to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int count)
        {
            _envelopeFollower.Process(buffer, offset, count);

            float envelope = _envelopeFollower.Envelope;

            float threshold = _thresholdLinear;
            float knee = _kneeLinear;
            float ratio = Ratio;
            float makeupGain = _makeupGainLinear;

            for (int i = 0; i < count; i++)
            {
                float sample = buffer[offset + i];

                // Calculate gain reduction
                float gainReduction = 0;
                if (envelope > threshold)
                {
                    float excess = envelope - threshold;
                    if (excess > knee)
                    {
                        gainReduction = (excess - knee) / (ratio - 1);
                    }
                    else
                    {
                        gainReduction = excess / (ratio * (knee / threshold));
                    }
                }

                // Apply gain reduction and makeup gain
                float gain = 1 / (1 + gainReduction);
                buffer[offset + i] = sample * gain * makeupGain;

                // Update current gain reduction
                CurrentGainReductionDb = LinearToDb(gain);
            }
        }
    }
}
