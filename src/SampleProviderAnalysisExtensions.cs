using System;
using System.Buffers;
using NAudio.Wave;

namespace NAudioEffects
{
    public static class SampleProviderAnalysisExtensions
    {
        public static float MeasurePeak(this ISampleProvider provider, double seconds)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (seconds <= 0) throw new ArgumentException("Seconds must be positive", nameof(seconds));

            int totalSamples = (int)(seconds * provider.WaveFormat.SampleRate * provider.WaveFormat.Channels);
            float[] buffer = ArrayPool<float>.Shared.Rent(totalSamples);
            try
            {
                int samplesRead = provider.Read(buffer, 0, totalSamples);
                float max = 0;
                for (int i = 0; i < samplesRead; i++)
                {
                    float abs = Math.Abs(buffer[i]);
                    if (abs > max) max = abs;
                }
                return max;
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }

        public static float MeasureRms(this ISampleProvider provider, double seconds)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (seconds <= 0) throw new ArgumentException("Seconds must be positive", nameof(seconds));

            int totalSamples = (int)(seconds * provider.WaveFormat.SampleRate * provider.WaveFormat.Channels);
            float[] buffer = ArrayPool<float>.Shared.Rent(totalSamples);
            try
            {
                int samplesRead = provider.Read(buffer, 0, totalSamples);
                if (samplesRead == 0) return 0;

                double sum = 0;
                for (int i = 0; i < samplesRead; i++)
                {
                    sum += buffer[i] * buffer[i];
                }
                return (float)Math.Sqrt(sum / samplesRead);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }
    }
}
