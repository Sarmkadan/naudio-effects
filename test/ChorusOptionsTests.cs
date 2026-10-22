#nullable enable

using System;
using Xunit;

namespace NAudioEffects.Tests
{
    public class ChorusOptionsTests
    {
        [Fact]
        public void Default_ReturnsDocumentedDefaults()
        {
            var options = ChorusOptions.Default;

            Assert.Equal(0.3f, options.RateHz);
            Assert.Equal(5f, options.DepthMs);
            Assert.Equal(0.3f, options.Mix);
        }

        [Fact]
        public void Default_ReturnsNewInstanceEachTime()
        {
            var first = ChorusOptions.Default;
            var second = ChorusOptions.Default;

            Assert.NotSame(first, second);

            first.RateHz = 7f;
            Assert.Equal(0.3f, second.RateHz);
        }

        [Fact]
        public void Default_Validates()
        {
            ChorusOptions.Default.Validate();
        }

        [Theory]
        [InlineData(float.Epsilon)]
        [InlineData(0.1f)]
        [InlineData(20f)]
        public void Validate_RateHzWithinRange_DoesNotThrow(float rate)
        {
            var options = new ChorusOptions { RateHz = rate };

            options.Validate();
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-float.Epsilon)]
        [InlineData(-1f)]
        [InlineData(20.0001f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void Validate_RateHzOutsideRange_ThrowsArgumentOutOfRange(float rate)
        {
            var options = new ChorusOptions { RateHz = rate };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal(nameof(ChorusOptions.RateHz), ex.ParamName);
        }

        [Theory]
        [InlineData(float.Epsilon)]
        [InlineData(0.1f)]
        [InlineData(50f)]
        public void Validate_DepthMsWithinRange_DoesNotThrow(float depth)
        {
            var options = new ChorusOptions { DepthMs = depth };

            options.Validate();
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-float.Epsilon)]
        [InlineData(-1f)]
        [InlineData(50.0001f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void Validate_DepthMsOutsideRange_ThrowsArgumentOutOfRange(float depth)
        {
            var options = new ChorusOptions { DepthMs = depth };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal(nameof(ChorusOptions.DepthMs), ex.ParamName);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(float.Epsilon)]
        [InlineData(0.5f)]
        [InlineData(1f)]
        public void Validate_MixWithinRange_DoesNotThrow(float mix)
        {
            var options = new ChorusOptions { Mix = mix };

            options.Validate();
        }

        [Theory]
        [InlineData(-float.Epsilon)]
        [InlineData(-0.1f)]
        [InlineData(1.0001f)]
        [InlineData(1.5f)]
        [InlineData(float.NaN)]
        public void Validate_MixOutsideRange_ThrowsArgumentOutOfRange(float mix)
        {
            var options = new ChorusOptions { Mix = mix };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal(nameof(ChorusOptions.Mix), ex.ParamName);
        }

        [Fact]
        public void Validate_ExactUpperLimits_Pass()
        {
            var options = new ChorusOptions { RateHz = 20f, DepthMs = 50f, Mix = 1f };

            options.Validate();
        }

        [Fact]
        public void Validate_JustOutsideUpperLimits_Fail()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { RateHz = MathF.BitIncrement(20f) }.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { DepthMs = MathF.BitIncrement(50f) }.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { Mix = MathF.BitIncrement(1f) }.Validate());
        }

        [Fact]
        public void Validate_JustOutsideLowerLimits_Fail()
        {
            // RateHz and DepthMs are exclusive at 0, so 0 itself must fail.
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { RateHz = 0f }.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { DepthMs = 0f }.Validate());

            // Mix is inclusive at 0, so the value just below it must fail.
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ChorusOptions { Mix = MathF.BitDecrement(0f) }.Validate());
        }

        [Fact]
        public void Validate_ReportsFirstInvalidOption()
        {
            var options = new ChorusOptions { RateHz = 0f, DepthMs = 0f, Mix = 2f };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
            Assert.Equal(nameof(ChorusOptions.RateHz), ex.ParamName);
        }
    }
}
