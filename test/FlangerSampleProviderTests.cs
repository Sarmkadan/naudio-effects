using System;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    /// <summary>
    /// Regression test to ensure that providers correctly honour the offset parameter
    /// when reading samples.
    /// </summary>
    public class FlangerSampleProviderTests
    {
        private class ConstantSampleProvider : ISampleProvider
        {
            public WaveFormat WaveFormat { get; }

            private readonly float _value;

            public ConstantSampleProvider(int sampleRate = 44100, int channels = 2, float value = 0.1f)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                _value = value;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = _value;
                }
                return count;
            }
        }

        [Fact]
        public void Read_WithOffset_DoesNotOverwritePreOffsetSamples()
        {
            // Arrange
            var source = new ConstantSampleProvider();
            var flanger = new FlangerSampleProvider(source);

            // Fill buffer with a sentinel value
            float[] buffer = new float[20];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = -1f;

            // Act: read 5 samples starting at offset 7
            int samplesRead = flanger.Read(buffer, 7, 5);

            // Assert
            Assert.Equal(5, samplesRead);

            // The region before the offset must remain untouched
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(-1f, buffer[i]);
            }

            // At least one of the written samples must differ from the sentinel value,
            // confirming that processing occurred.
            bool anyChanged = false;
            for (int i = 7; i < 7 + 5; i++)
            {
                if (buffer[i] != -1f)
                {
                    anyChanged = true;
                    break;
                }
            }
            Assert.True(anyChanged, "Samples after the offset should have been processed.");
        }
    }
}
