using NAudio.Dsp;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// A noise gate that attenuates audio below a specified threshold with smooth transitions.
    /// </summary>
    public class NoiseGateSampleProvider : EffectSampleProviderBase
    {
        private readonly EnvelopeFollower _envelopeFollower;
        private readonly int _sampleRate;
        private float _thresholdDb = -30.0f;
        private float _attackMs = 5.0f;
        private float _releaseMs = 50.0f;
        private float _holdMs = 50.0f;
        private float _currentGain = 1.0f;
        private float _targetGain = 1.0f;
        private int _holdCounter = 0;

        /// <summary>
        /// Gets or sets the threshold in decibels. The value must not be <see cref="float.NaN"/>.
        /// </summary>
        public float ThresholdDb
        {
            get => _thresholdDb;
            set
            {
                if (float.IsNaN(value))
                {
                    throw new ArgumentException("Threshold must not be NaN.", nameof(ThresholdDb));
                }

                _thresholdDb = value;
            }
        }

        /// <summary>
        /// Gets or sets the attack time in milliseconds. The value must be finite and greater than or equal to zero.
        /// </summary>
        public float AttackMs
        {
            get => _attackMs;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(AttackMs), "Attack time must be finite and greater than or equal to zero.");
                }

                _attackMs = value;
            }
        }

        /// <summary>
        /// Gets or sets the release time in milliseconds. The value must be finite and greater than or equal to zero.
        /// </summary>
        public float ReleaseMs
        {
            get => _releaseMs;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(ReleaseMs), "Release time must be finite and greater than or equal to zero.");
                }

                _releaseMs = value;
            }
        }

        /// <summary>
        /// Gets or sets the hold time in milliseconds. The value must be finite and greater than or equal to zero.
        /// </summary>
        public float HoldMs
        {
            get => _holdMs;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(HoldMs), "Hold time must be finite and greater than or equal to zero.");
                }

                _holdMs = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the NoiseGateSampleProvider.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public NoiseGateSampleProvider(ISampleProvider source) : base(source)
        {
            _sampleRate = source.WaveFormat.SampleRate;
            if (_sampleRate <= 0)
            {
                throw new ArgumentException("Source sample rate must be greater than zero.", nameof(source));
            }

            _envelopeFollower = new EnvelopeFollower();
        }

        /// <summary>
        /// Processes the audio block.
        /// </summary>
        /// <param name="buffer">The audio buffer.</param>
        /// <param name="offset">The offset in the buffer.</param>
        /// <param name="count">The number of samples to process.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int count)
        {
            // Re-apply envelope timing every block so that changes to AttackMs/ReleaseMs/HoldMs
            // made after construction actually take effect.
            _envelopeFollower.SetParameters(AttackMs, ReleaseMs, _sampleRate);
            _envelopeFollower.Process(buffer, offset, count);

            float envelope = _envelopeFollower.Envelope;
            float thresholdLinear = DbToLinear(ThresholdDb);

            // Check if we should open or close the gate
            bool aboveThreshold = envelope > thresholdLinear;

            if (aboveThreshold)
            {
                _holdCounter = 0;
                _targetGain = 1.0f;
            }
            else
            {
                _holdCounter++;

                // Check if we're still in hold time
                if (_holdCounter * 1000.0f / _sampleRate < HoldMs)
                {
                    _targetGain = 1.0f;
                }
                else
                {
                    _targetGain = 0.0f;
                }
            }

            // Smooth gain transition
            float attackCoeff = CalculateCoefficient(AttackMs, _sampleRate);
            float releaseCoeff = CalculateCoefficient(ReleaseMs, _sampleRate);

            if (_targetGain > _currentGain)
            {
                // Opening the gate - use attack
                _currentGain = _targetGain - (_targetGain - _currentGain) * attackCoeff;
            }
            else
            {
                // Closing the gate - use release
                _currentGain = _targetGain + (_currentGain - _targetGain) * releaseCoeff;
            }

            // Apply gain to samples
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= _currentGain;
            }
        }

        private static float CalculateCoefficient(float timeMs, int sampleRate)
        {
            if (timeMs <= 0f)
            {
                return 0f;
            }

            float timeConstantSeconds = timeMs / 1000f;
            return (float)Math.Exp(-1f / (timeConstantSeconds * sampleRate));
        }
    }
}
