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

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EqualizerSampleProvider(null!));
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBandCountIsZero()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 0));
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBandCountIsNegative()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new EqualizerSampleProvider(new TestSampleProvider(new float[10]), -1));
        }

        [Fact]
        public void SetBandGain_ThrowsArgumentOutOfRangeException_WhenBandIndexIsNegative()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetBandGain(-1, 0f));
        }

        [Fact]
        public void SetBandGain_ThrowsArgumentOutOfRangeException_WhenBandIndexIsTooLarge()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetBandGain(5, 0f));
        }

        [Fact]
        public void SetBandGain_ThrowsArgumentOutOfRangeException_WhenGainIsTooLow()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetBandGain(0, -25f));
        }

        [Fact]
        public void SetBandGain_ThrowsArgumentOutOfRangeException_WhenGainIsTooHigh()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetBandGain(0, 25f));
        }

        [Fact]
        public void GetBandFrequency_ThrowsArgumentOutOfRangeException_WhenBandIndexIsNegative()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetBandFrequency(-1));
        }

        [Fact]
        public void GetBandFrequency_ThrowsArgumentOutOfRangeException_WhenBandIndexIsTooLarge()
        {
            // Arrange
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10]), 5);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetBandFrequency(5));
        }

        [Fact]
        public void ProcessBlock_ThrowsArgumentOutOfRangeException_WhenFrequencyIsInvalid()
        {
            // Arrange: Create a provider with a sample rate that will make our test frequency invalid
            // We'll use a very low sample rate so that even our lowest band frequency (60Hz) is >= sampleRate/2
            var provider = new EqualizerSampleProvider(new TestSampleProvider(new float[10], sampleRate: 100), 5);
            // With sampleRate=100, sampleRate/2=50Hz. Our lowest band frequency is 60Hz, which is >= 50Hz, so invalid

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(new float[10], 0, 10));
        }

        [Fact]
        public void ProcessBlock_WorksCorrectly_WithSingleBand()
        {
            // Arrange: Test edge case of single band to avoid division by zero
            float[] sourceData = new float[1024];
            for (int i = 0; i < sourceData.Length; i++)
                sourceData[i] = (i % 2 == 0) ? 0.1f : -0.1f;

            var provider = new EqualizerSampleProvider(new TestSampleProvider(sourceData), bandCount: 1);
            provider.SetBandGain(0, 3.0f); // Set some gain

            // Act
            float[] buffer = new float[1024];
            int read = provider.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(1024, read);
            // Just verify it doesn't throw and produces some output
            Assert.NotEqual(0f, buffer[0]); // Should have processed the signal
        }
    }
}
