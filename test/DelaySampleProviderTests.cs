#nullable enable

using System;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class DelaySampleProviderTests
    {
        private class ImpulseSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private int _index;

            public ImpulseSampleProvider(float[] data, WaveFormat waveFormat)
            {
                _data = data;
                WaveFormat = waveFormat;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                int toCopy = Math.Min(count, _data.Length - _index);
                Array.Copy(_data, _index, buffer, offset, toCopy);
                _index += toCopy;
                return toCopy;
            }
        }

        [Fact]
        public void Read_WithImpulseInput_DelayedOutputIsUnchanged()
        {
            // Arrange
            int sampleRate = 48000;
            float delayMs = 100f; // 100ms delay
            int delaySamples = (int)(delayMs * sampleRate / 1000f);
            int totalSamples = delaySamples + 10;
            
            float[] impulse = new float[totalSamples];
            impulse[0] = 1.0f;

            var source = new ImpulseSampleProvider(impulse, new WaveFormat(sampleRate, 1));
            var delay = new DelaySampleProvider(source, maxDelayMs: 200f) { DelayMs = delayMs, Feedback = 0f, Mix = 1f };
            float[] output = new float[totalSamples];

            // Act
            delay.Read(output, 0, totalSamples);

            // Assert
            // The impulse should appear exactly at the delay index
            Assert.Equal(1.0f, output[delaySamples], 5);
            Assert.Equal(0.0f, output[0], 5);
            Assert.Equal(0.0f, output[delaySamples + 1], 5);
            
            // Verify no other samples are non-zero (within floating point tolerance)
            for (int i = 0; i < totalSamples; i++)
            {
                if (i == delaySamples) continue;
                Assert.Equal(0.0f, output[i], 5);
            }
        }
    }
}
