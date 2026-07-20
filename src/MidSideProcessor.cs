using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Mid/Side processor that converts stereo audio from Left/Right to Mid/Side representation,
    /// applies independent gain to mid and side channels, then converts back to Left/Right.
    /// This allows processing the center channel independently from the stereo image.
    /// </summary>
    public class MidSideProcessor : EffectSampleProviderBase
    {
        private float _midGainDb = 0f;
        private float _sideGainDb = 0f;
        private float _currentMidGainLinear = 1.0f;
        private float _currentSideGainLinear = 1.0f;
        private float _targetMidGainLinear = 1.0f;
        private float _targetSideGainLinear = 1.0f;
        private float _samplesPerMs;
        private int _midSamplesUntilNextUpdate;
        private int _sideSamplesUntilNextUpdate;
        private int _midUpdateInterval;
        private int _sideUpdateInterval;

        /// <summary>
        /// Gets or sets the gain in decibels for the mid channel (center content).
        /// </summary>
        public float MidGainDb
        {
            get => _midGainDb;
            set
            {
                var newGainLinear = DbToLinear(value);
                if (Math.Abs(_targetMidGainLinear - newGainLinear) > float.Epsilon)
                {
                    _midGainDb = value;
                    _targetMidGainLinear = newGainLinear;
                    _midSamplesUntilNextUpdate = _midUpdateInterval;
                }
            }
        }

        /// <summary>
        /// Gets or sets the gain in decibels for the side channel (stereo image).
        /// </summary>
        public float SideGainDb
        {
            get => _sideGainDb;
            set
            {
                var newGainLinear = DbToLinear(value);
                if (Math.Abs(_targetSideGainLinear - newGainLinear) > float.Epsilon)
                {
                    _sideGainDb = value;
                    _targetSideGainLinear = newGainLinear;
                    _sideSamplesUntilNextUpdate = _sideUpdateInterval;
                }
            }
        }

        /// <summary>
        /// Gets or sets the smoothing time in milliseconds for mid channel gain changes.
        /// </summary>
        public float MidSmoothingMs { get; set; } = 10;

        /// <summary>
        /// Gets or sets the smoothing time in milliseconds for side channel gain changes.
        /// </summary>
        public float SideSmoothingMs { get; set; } = 10;

        /// <summary>
        /// Initializes a new instance of the MidSideProcessor.
        /// </summary>
        /// <param name="source">Source sample provider (must be stereo)</param>
        /// <exception cref="ArgumentException">Thrown if source is not stereo (2 channels)</exception>
        public MidSideProcessor(ISampleProvider source) : base(source)
        {
            if (source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("MidSideProcessor requires a stereo source (2 channels)", nameof(source));
            }

            _samplesPerMs = WaveFormat.SampleRate / 1000.0f;
            _midUpdateInterval = (int)(MidSmoothingMs * _samplesPerMs);
            _sideUpdateInterval = (int)(SideSmoothingMs * _samplesPerMs);
            if (_midUpdateInterval < 1) _midUpdateInterval = 1;
            if (_sideUpdateInterval < 1) _sideUpdateInterval = 1;
        }

        /// <summary>
        /// Processes a block of samples by converting to Mid/Side, applying independent gains,
        /// then converting back to Left/Right.
        /// </summary>
        /// <param name="buffer">The buffer containing the samples.</param>
        /// <param name="offset">The offset in the buffer where the block starts.</param>
        /// <param name="samplesRead">The number of samples read into the buffer.</param>
        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            // Ensure update intervals are up to date
            _midUpdateInterval = (int)(MidSmoothingMs * _samplesPerMs);
            _sideUpdateInterval = (int)(SideSmoothingMs * _samplesPerMs);
            if (_midUpdateInterval < 1) _midUpdateInterval = 1;
            if (_sideUpdateInterval < 1) _sideUpdateInterval = 1;

            // Process in stereo pairs (2 channels)
            for (int n = 0; n < samplesRead; n += 2)
            {
                int sampleIndex = offset + n;

                // Get left and right samples
                float left = buffer[sampleIndex];
                float right = buffer[sampleIndex + 1];

                // Convert Left/Right to Mid/Side
                float mid = (left + right) * 0.5f;
                float side = (left - right) * 0.5f;

                // Apply current gains
                float processedMid = mid * _currentMidGainLinear;
                float processedSide = side * _currentSideGainLinear;

                // Convert Mid/Side back to Left/Right
                float newLeft = processedMid + processedSide;
                float newRight = processedMid - processedSide;

                // Store back
                buffer[sampleIndex] = newLeft;
                buffer[sampleIndex + 1] = newRight;
            }

            // Update mid gain smoothly
            if (_midSamplesUntilNextUpdate > 0)
            {
                _midSamplesUntilNextUpdate -= samplesRead;
            }
            else
            {
                float midStep = (_targetMidGainLinear - _currentMidGainLinear) / _midUpdateInterval;
                _currentMidGainLinear += midStep;
                _midSamplesUntilNextUpdate = _midUpdateInterval - (samplesRead % _midUpdateInterval);
            }

            // Update side gain smoothly
            if (_sideSamplesUntilNextUpdate > 0)
            {
                _sideSamplesUntilNextUpdate -= samplesRead;
            }
            else
            {
                float sideStep = (_targetSideGainLinear - _currentSideGainLinear) / _sideUpdateInterval;
                _currentSideGainLinear += sideStep;
                _sideSamplesUntilNextUpdate = _sideUpdateInterval - (samplesRead % _sideUpdateInterval);
            }
        }
    }
}
