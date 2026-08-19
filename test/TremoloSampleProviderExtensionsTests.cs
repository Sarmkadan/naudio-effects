using Xunit;
using NAudioEffects;
using System;

namespace NAudioEffects.Tests
{
    public class TremoloSampleProviderExtensionsTests
    {
        [Theory]
        [InlineData(120, 4, 2.0f)]
        [InlineData(120, 8, 4.0f)]
        [InlineData(60, 4, 1.0f)]
        public void RateFromBpm_CalculatesCorrectRate(float bpm, int noteDivision, float expectedHz)
        {
            float rate = TremoloSampleProviderExtensions.RateFromBpm(bpm, noteDivision);
            Assert.Equal(expectedHz, rate, precision: 2);
        }

        [Fact]
        public void RateFromBpm_InvalidBpm_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => TremoloSampleProviderExtensions.RateFromBpm(0, 4));
        }

        [Fact]
        public void RateFromBpm_InvalidNoteDivision_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => TremoloSampleProviderExtensions.RateFromBpm(120, 0));
        }
    }
}
