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

        [Fact]
        public void Constructor_WithNullSource_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DelaySampleProvider(null!));
        }

        [Fact]
        public void Constructor_WithNegativeMaxDelayMs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DelaySampleProvider(source, -1f));
        }

        [Fact]
        public void Constructor_WithNaNMaxDelayMs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DelaySampleProvider(source, float.NaN));
        }

        [Fact]
        public void Constructor_WithInfinityMaxDelayMs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DelaySampleProvider(source, float.PositiveInfinity));
        }

        [Fact]
        public void Constructor_WithTooLargeMaxDelayMs_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DelaySampleProvider(source, 1e9f)); // Very large value
        }

        [Fact]
        public void DelayMs_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.DelayMs = -1f);
        }

        [Fact]
        public void DelayMs_SetValueAboveMax_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source, maxDelayMs: 100f);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.DelayMs = 150f);
        }

        [Fact]
        public void Feedback_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Feedback = -0.1f);
        }

        [Fact]
        public void Feedback_SetValueAbovePoint95_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Feedback = 1.0f);
        }

        [Fact]
        public void Mix_SetNegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Mix = -0.1f);
        }

        [Fact]
        public void Mix_SetValueAboveOne_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Mix = 1.5f);
        }

        [Fact]
        public void Read_WithNullBuffer_ThrowsArgumentNullException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => delay.Read(null!, 0, 10));
        }

        [Fact]
        public void Read_WithNegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);
            float[] buffer = new float[10];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Read(buffer, -1, 5));
        }

        [Fact]
        public void Read_WithNegativeCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);
            float[] buffer = new float[10];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Read(buffer, 0, -5));
        }

        [Fact]
        public void Read_WithOffsetAndCountExceedingBufferLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var source = new ImpulseSampleProvider(new float[] { 0f }, new WaveFormat(44100, 1));
            var delay = new DelaySampleProvider(source);
            float[] buffer = new float[10];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => delay.Read(buffer, 5, 10)); // offset 5 + count 10 = 15 > buffer length 10
        }
    }
}
