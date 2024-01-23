using NAudio.Dsp;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Mid/Side stereo widening effect
    /// </summary>
    public class StereoWidenerSampleProvider : EffectSampleProviderBase
    {
            private float _width = 1.0f;

        /// <summary>
        /// Gets or sets the stereo width (0 = mono, 1 = original, 2 = maximum width)
        /// </summary>
        public float Width
        {
            get => _width;
            set => _width = Math.Clamp(value, 0.0f, 2.0f);
        }

        /// <summary>
        /// Initializes a new instance of the StereoWidenerSampleProvider
        /// </summary>
        /// <param name="source">Source sample provider (must be stereo)</param>
        /// <exception cref="ArgumentException">Thrown if source is not stereo (2 channels)</exception>
        public StereoWidenerSampleProvider(ISampleProvider source) : base(source)
        {
            if (source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("StereoWidenerSampleProvider requires a stereo source (2 channels)", nameof(source));
            }

        }

        protected override void ProcessBlock(float[] buffer, int offset, int samplesRead)
        {
            // Process in stereo pairs
            for (int n = 0; n < samplesRead; n += 2)
            {
                int sampleIndex = offset + n;

                // Get left and right samples
                float left = buffer[sampleIndex];
                float right = buffer[sampleIndex + 1];

                // Calculate mid and side
                float mid = (left + right) * 0.5f;
                float side = (left - right) * 0.5f * Width;

                // Reconstruct left and right from mid and side
                float newLeft = mid + side;
                float newRight = mid - side;

                // Store back
                buffer[sampleIndex] = newLeft;
                buffer[sampleIndex + 1] = newRight;
            }
        }
    }
}