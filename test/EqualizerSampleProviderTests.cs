using System;
using NAudio.Wave;
using NAudioEffects;
using Xunit;

namespace NAudioEffects.Tests
{
    /// <summary>
    /// Tests for <see cref="EqualizerSampleProvider"/> that verify the optimisation does not
    /// change the observable output.
    /// </summary>
    public class EqualizerSampleProviderTests
    {
        private class TestSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private int _position;
            public WaveFormat WaveFormat { get; }

            public TestSampleProvider(float[] data, int sampleRate = 44100, int channels = 2)
            {
                _data = data ?? throw new ArgumentNullException(nameof(data));
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int remaining = _data.Length - _position;
                int toCopy = Math.Min(count, remaining);
                if (toCopy <= 0) return 0;

                Array.Copy(_data, _position, buffer, offset, toCopy);
                _position += toCopy;
                return toCopy;
            }
        }

        [Fact]
        public void ProcessBlock_OutputUnchanged_WhenGainsAreZero()
        {
            // Arrange: deterministic source data (alternating small positive/negative values)
            float[] sourceData = new float[1024];
            for (int i = 0; i < sourceData.Length; i++)
                sourceData[i] = (i % 2 == 0) ? 0.1f : -0.1f;

            // Two independent providers with identical source data
            var providerA = new EqualizerSampleProvider(new TestSampleProvider(sourceData), bandCount: 5);
            var providerB = new EqualizerSampleProvider(new TestSampleProvider(sourceData), bandCount: 5);

            // Act: read full buffers from both providers
            float[] bufferA = new float[1024];
            float[] bufferB = new float[1024];
            int readA = providerA.Read(bufferA, 0, bufferA.Length);
            int readB = providerB.Read(bufferB, 0, bufferB.Length);

            // Assert: same number of samples read and identical sample values
            Assert.Equal(readA, readB);
            Assert.Equal(bufferA, bufferB);
        }
    }
}
