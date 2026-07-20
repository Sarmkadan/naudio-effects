using NAudio.Dsp;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// A sample provider that applies a brickwall limiter to the input audio.
    /// </summary>
    public class LimiterSampleProvider : EffectSampleProviderBase
    {
        private readonly EnvelopeFollower _envelopeFollower;
        private readonly int _sampleRate;

        /// <summary>
        /// Initializes a new instance of the LimiterSampleProvider class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public LimiterSampleProvider(ISampleProvider source)
            : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            _envelopeFollower = new EnvelopeFollower(AttackMs, ReleaseMs, _sampleRate);
        }

        /// <summary>
        /// Gets or sets the ceiling in decibels.
        /// </summary>
        /// <value>The ceiling in decibels.</value>
        public float CeilingDb { get; set; } = -1.0f;

        /// <summary>
        /// Gets or sets the release time in milliseconds.
        /// </summary>
        /// <value>The release time in milliseconds.</value>
        public float ReleaseMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets the attack time in milliseconds.
        /// </summary>
        /// <value>The attack time in milliseconds.</value>
        public float AttackMs { get; set; } = 10;

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

            float gainReductionDb = Math.Max(0, envelopeDb - CeilingDb);

            CurrentGainReductionDb = gainReductionDb;
            float gain = DbToLinear(-gainReductionDb);

            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= gain;
            }
        }
    }
}
