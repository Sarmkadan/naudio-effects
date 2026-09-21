using System;
using NAudio.Wave;
using NAudioEffects;
using Xunit;

namespace NAudioEffects.Tests
{
    public class BitCrusherSampleProviderTests
    {
        private class ConstantSampleProvider : ISampleProvider
        {
            private readonly float _value;
            private readonly WaveFormat _waveFormat;

            public ConstantSampleProvider(float value, int channels = 1, int sampleRate = 44100)
            {
                _value = value;
                _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }

            public WaveFormat WaveFormat => _waveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                for (int n = 0; n < count; n++)
                {
                    buffer[offset + n] = _value;
                }
                return count;
            }
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new BitCrusherSampleProvider(null!));
        }

        [Fact]
        public void BitDepth_SetValueLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.BitDepth = 0);
        }

        [Fact]
        public void BitDepth_SetValueGreaterThanThirtyTwo_ThrowsArgumentOutOfRangeException()
        {
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.BitDepth = 33);
        }

        [Fact]
        public void HoldFactor_SetValueLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.HoldFactor = 0);
        }

        [Fact]
        public void Read_ThrowsArgumentNullException_WhenBufferIsNull()
        {
            // Arrange
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.Read(null!, 0, 10));
        }

        [Fact]
        public void Read_ThrowsArgumentOutOfRangeException_WhenOffsetIsNegative()
        {
            // Arrange
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            var buffer = new float[20];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(buffer, -1, 10));
        }

        [Fact]
        public void Read_ThrowsArgumentOutOfRangeException_WhenSamplesReadIsNegative()
        {
            // Arrange
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            var buffer = new float[20];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(buffer, 0, -5));
        }

        [Fact]
        public void Read_ThrowsArgumentException_WhenOffsetAndSamplesReadExceedBufferLength()
        {
            // Arrange
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            var buffer = new float[10];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.Read(buffer, 5, 10));
        }

        [Fact]
        public void Read_DoesNotThrow_WhenArgumentsAreValid()
        {
            // Arrange
            var provider = new BitCrusherSampleProvider(new ConstantSampleProvider(0.5f));
            var buffer = new float[20];

            // Act
            var read = provider.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(buffer.Length, read);
            // We can also check that the buffer has been processed (values changed)
            // But for brevity, we just ensure no exception is thrown.
        }
    }
}