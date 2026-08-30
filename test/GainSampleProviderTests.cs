using System;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class GainSampleProviderTests
    {
        private class ConstantSampleProvider : ISampleProvider
        {
            private readonly float _value;

            public ConstantSampleProvider(int sampleRate = 1000, int channels = 1, float value = 1.0f)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                _value = value;
            }

            public WaveFormat WaveFormat { get; }

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
        public void Read_WithUnityGain_PassesSamplesThroughUnchanged()
        {
            var gain = new GainSampleProvider(new ConstantSampleProvider(value: 0.25f));
            var buffer = new float[16];

            int samplesRead = gain.Read(buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, samplesRead);
            Assert.All(buffer, sample => Assert.Equal(0.25f, sample));
        }

        [Fact]
        public void Read_WithNegativeGain_AttenuatesToExpectedLinearFactorAfterSmoothing()
        {
            const float gainDb = -6.0f;
            var gain = new GainSampleProvider(new ConstantSampleProvider())
            {
                GainDb = gainDb,
                SmoothingMs = 10.0f
            };
            var smoothingBuffer = new float[10];
            var settledBuffer = new float[10];

            gain.Read(smoothingBuffer, 0, smoothingBuffer.Length);
            gain.Read(settledBuffer, 0, settledBuffer.Length);

            float expected = GainSampleProvider.DbToLinear(gainDb);
            Assert.All(settledBuffer, sample => Assert.InRange(sample, expected - 0.00001f, expected + 0.00001f));
        }

        [Theory]
        [InlineData(-60.0f)]
        [InlineData(-12.0f)]
        [InlineData(0.0f)]
        [InlineData(6.0f)]
        public void DbToLinear_ThenLinearToDb_RoundTrips(float decibels)
        {
            float linear = GainSampleProvider.DbToLinear(decibels);

            float result = GainSampleProvider.LinearToDb(linear);

            Assert.InRange(result, decibels - 0.00001f, decibels + 0.00001f);
        }

        [Fact]
        public void LinearToDb_WithZero_ReturnsNegativeInfinity()
        {
            Assert.Equal(float.NegativeInfinity, GainSampleProvider.LinearToDb(0.0f));
        }

        [Fact]
        public void Read_WhenBypassed_LeavesSamplesUntouched()
        {
            var gain = new GainSampleProvider(new ConstantSampleProvider(value: 0.25f))
            {
                GainDb = -24.0f,
                Bypass = true
            };
            var buffer = new float[16];

            int samplesRead = gain.Read(buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, samplesRead);
            Assert.All(buffer, sample => Assert.Equal(0.25f, sample));
        }

        [Fact]
        public void ChangingGainDb_MidStreamConvergesToNewTargetWithoutExceedingIt()
        {
            const float initialGainDb = -12.0f;
            const float newGainDb = -6.0f;
            var gain = new GainSampleProvider(new ConstantSampleProvider())
            {
                GainDb = initialGainDb,
                SmoothingMs = 10.0f
            };
            var buffer = new float[10];

            gain.Read(buffer, 0, buffer.Length);
            gain.Read(buffer, 0, buffer.Length);
            gain.GainDb = newGainDb;
            gain.Read(buffer, 0, buffer.Length);
            gain.Read(buffer, 0, buffer.Length);

            float target = GainSampleProvider.DbToLinear(newGainDb);
            Assert.All(buffer, sample => Assert.True(sample <= target + 0.00001f));
            Assert.InRange(buffer[buffer.Length - 1], target - 0.00001f, target + 0.00001f);
            Assert.InRange(gain.GainDb, newGainDb - 0.0001f, newGainDb + 0.0001f);
        }
    }
}
