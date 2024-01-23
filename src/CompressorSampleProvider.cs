#nullable enable

using NAudio.Wave;
using NAudio.Dsp;

namespace NAudioEffects
{
    public class CompressorSampleProvider : EffectSampleProviderBase
    {
        private readonly EnvelopeFollower _envelopeFollower;
        private float _thresholdLinear;
        private float _kneeLinear;
        private float _makeupGainLinear;

        public CompressorSampleProvider(ISampleProvider source) 
            : base(source)
        {
            _envelopeFollower = new EnvelopeFollower(AttackMs, ReleaseMs, source.WaveFormat.SampleRate);
            UpdateThreshold();
            UpdateKnee();
            UpdateMakeupGain();
        }

        public float ThresholdDb { get; set; } = -20;
        public float Ratio { get; set; } = 4;
        public float AttackMs { get; set; } = 10;
        public float ReleaseMs { get; set; } = 100;
        public float KneeDb { get; set; } = 6;
        public float MakeupGainDb { get; set; } = 0;
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
